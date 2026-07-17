using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Features.Finance.Queries;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using Xunit;
using AcademicTermEntity = SMS.Domain.Entities.AcademicTerm;

namespace SMS.Infrastructure.Tests;

public class FinanceReportsTests : IAsyncLifetime
{
    private readonly TenantTestContext _harness = new();
    private Guid _termId;
    private Guid _defaulterStudentId;

    public async Task InitializeAsync()
    {
        var tenant = new Tenant { Name = "Finance School", Code = "FIN", Currency = "USD" };
        await using (var db = _harness.CreateDbContext())
        {
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync();
        }
        _harness.UseTenant(tenant.Id);

        await using (var db = _harness.CreateDbContext())
        {
            var year = new AcademicYear { Name = "2026", Year = 2026, StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 12, 31) };
            db.AcademicYears.Add(year);
            await db.SaveChangesAsync();
            var term = new AcademicTermEntity { AcademicYearId = year.Id, Name = "Term 1", TermNumber = 1, StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 4, 30) };
            db.AcademicTerms.Add(term);

            var defaulter = NewStudent("S-DEF", "Dave");
            var paidStudent = NewStudent("S-PAID", "Paula");
            db.Students.AddRange(defaulter, paidStudent);
            var guardian = new Guardian { FirstName = "Gina", LastName = "Guardian", Gender = Gender.Other, Phone = "+263773000001" };
            db.Guardians.Add(guardian);
            await db.SaveChangesAsync();

            db.StudentGuardians.Add(new StudentGuardian { StudentId = defaulter.Id, GuardianId = guardian.Id, Relationship = "Parent", IsPrimaryContact = true });

            // Defaulter owes 400; Paula fully paid.
            var defaulterInvoice = new Invoice { InvoiceNumber = "INV-DEF", StudentId = defaulter.Id, AcademicTermId = term.Id, InvoiceDate = new DateTime(2026, 1, 15), DueDate = new DateTime(2026, 2, 15), TotalAmount = 500, PaidAmount = 100 };
            db.Invoices.Add(defaulterInvoice);
            db.Invoices.Add(new Invoice { InvoiceNumber = "INV-PAID", StudentId = paidStudent.Id, AcademicTermId = term.Id, InvoiceDate = new DateTime(2026, 1, 15), DueDate = new DateTime(2026, 2, 15), TotalAmount = 200, PaidAmount = 200 });
            await db.SaveChangesAsync();

            // Payments for the cashier reconciliation.
            db.Payments.AddRange(
                Payment("RCP-1", defaulterInvoice.Id, 60, PaymentMethod.Cash, PaymentStatus.Completed, new DateTime(2026, 3, 2)),
                Payment("RCP-2", defaulterInvoice.Id, 40, PaymentMethod.MobileMoney, PaymentStatus.Completed, new DateTime(2026, 3, 2)),
                Payment("RCP-3", defaulterInvoice.Id, 999, PaymentMethod.Cash, PaymentStatus.Completed, new DateTime(2026, 1, 1)), // out of range
                Payment("RCP-4", defaulterInvoice.Id, 500, PaymentMethod.Cash, PaymentStatus.Pending, new DateTime(2026, 3, 2))); // not completed
            await db.SaveChangesAsync();

            _termId = term.Id;
            _defaulterStudentId = defaulter.Id;
        }
    }

    public Task DisposeAsync()
    {
        _harness.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Defaulters_report_lists_only_students_with_a_balance_and_their_guardian()
    {
        await using var db = _harness.CreateDbContext();
        var result = await new GetDefaultersReportQueryHandler(db)
            .Handle(new GetDefaultersReportQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Should().ContainSingle();
        var defaulter = result.Data[0];
        defaulter.StudentId.Should().Be(_defaulterStudentId);
        defaulter.OutstandingBalance.Should().Be(400m);
        defaulter.GuardianPhone.Should().Be("+263773000001");
    }

    [Fact]
    public async Task Reconciliation_sums_completed_payments_in_range_grouped_by_method()
    {
        await using var db = _harness.CreateDbContext();
        var result = await new GetCashierReconciliationQueryHandler(db)
            .Handle(new GetCashierReconciliationQuery { FromDate = new DateTime(2026, 3, 1), ToDate = new DateTime(2026, 3, 31) }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.PaymentCount.Should().Be(2, "only the two completed in-range payments count");
        var usd = result.Data.ByCurrency.Should().ContainSingle(c => c.Currency == "USD").Subject;
        usd.TotalCollected.Should().Be(100m, "60 + 40");
        usd.ByMethod.Should().Contain(m => m.PaymentMethod == "Cash" && m.Amount == 60m);
        usd.ByMethod.Should().Contain(m => m.PaymentMethod == "MobileMoney" && m.Amount == 40m);
    }

    private static Student NewStudent(string number, string first) => new()
    {
        StudentNumber = number,
        FirstName = first,
        LastName = "Student",
        DateOfBirth = new DateTime(2012, 1, 1),
        Gender = Gender.Other,
        AdmissionDate = new DateTime(2026, 1, 1),
        Status = StudentStatus.Active
    };

    private static Payment Payment(string receipt, Guid invoiceId, decimal amount, PaymentMethod method, PaymentStatus status, DateTime date) => new()
    {
        ReceiptNumber = receipt,
        InvoiceId = invoiceId,
        Amount = amount,
        PaymentMethod = method,
        Status = status,
        PaymentDate = date
    };
}
