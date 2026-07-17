using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Features.PaymentPlans.Commands;
using SMS.Application.Features.PaymentPlans.Queries;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using Xunit;
using AcademicTermEntity = SMS.Domain.Entities.AcademicTerm;

namespace SMS.Infrastructure.Tests;

public class PaymentPlanTests : IAsyncLifetime
{
    private readonly TenantTestContext _harness = new();
    private Guid _invoiceId;

    public async Task InitializeAsync()
    {
        var tenant = new Tenant { Name = "Plan School", Code = "PLN", Currency = "USD" };
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
            var student = new Student { StudentNumber = "S-PLN", FirstName = "Plan", LastName = "Ner", DateOfBirth = new DateTime(2012, 1, 1), Gender = Gender.Other, AdmissionDate = new DateTime(2026, 1, 1), Status = StudentStatus.Active };
            db.Students.Add(student);
            await db.SaveChangesAsync();

            // A USD invoice for 100 with a 5 partial payment already applied -> 95 balance.
            var invoice = new Invoice { InvoiceNumber = "INV-PLN", StudentId = student.Id, AcademicTermId = term.Id, InvoiceDate = new DateTime(2026, 1, 15), DueDate = new DateTime(2026, 2, 15), TotalAmount = 100, PaidAmount = 5, Currency = "USD" };
            db.Invoices.Add(invoice);
            await db.SaveChangesAsync();
            _invoiceId = invoice.Id;
        }
    }

    public Task DisposeAsync()
    {
        _harness.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Creating_a_plan_splits_the_balance_into_installments_that_sum_exactly()
    {
        await using var db = _harness.CreateDbContext();
        var result = await new CreatePaymentPlanCommandHandler(db).Handle(new CreatePaymentPlanCommand
        {
            InvoiceId = _invoiceId,
            InstallmentCount = 3,
            StartDate = new DateTime(2026, 2, 1),
            Frequency = "Monthly"
        }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var plan = result.Data!;
        plan.Installments.Should().HaveCount(3);

        // 95 / 3 -> 31.67, 31.67, 31.66 ; must sum to the 95 balance exactly.
        plan.Installments.Sum(i => i.Amount).Should().Be(95m);
        plan.Installments[0].Amount.Should().Be(31.67m);
        plan.Installments[2].Amount.Should().Be(31.66m);

        // Monthly spacing from the start date.
        plan.Installments[0].DueDate.Should().Be(new DateTime(2026, 2, 1));
        plan.Installments[1].DueDate.Should().Be(new DateTime(2026, 3, 1));
        plan.Installments[2].DueDate.Should().Be(new DateTime(2026, 4, 1));

        plan.Currency.Should().Be("USD");
    }

    [Fact]
    public async Task Weekly_frequency_spaces_installments_seven_days_apart()
    {
        await using var db = _harness.CreateDbContext();
        var result = await new CreatePaymentPlanCommandHandler(db).Handle(new CreatePaymentPlanCommand
        {
            InvoiceId = _invoiceId,
            InstallmentCount = 2,
            StartDate = new DateTime(2026, 2, 1),
            Frequency = "Weekly"
        }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Installments[1].DueDate.Should().Be(new DateTime(2026, 2, 8));
    }

    [Fact]
    public async Task A_second_active_plan_for_the_same_invoice_is_rejected()
    {
        await using (var db = _harness.CreateDbContext())
        {
            await new CreatePaymentPlanCommandHandler(db).Handle(new CreatePaymentPlanCommand
            {
                InvoiceId = _invoiceId,
                InstallmentCount = 2
            }, CancellationToken.None);
        }

        await using var db2 = _harness.CreateDbContext();
        var second = await new CreatePaymentPlanCommandHandler(db2).Handle(new CreatePaymentPlanCommand
        {
            InvoiceId = _invoiceId,
            InstallmentCount = 4
        }, CancellationToken.None);

        second.IsSuccess.Should().BeFalse();
        second.Error.Should().Contain("already exists");
    }

    [Fact]
    public async Task Paying_the_final_installment_completes_the_plan()
    {
        Guid planId;
        await using (var db = _harness.CreateDbContext())
        {
            var created = await new CreatePaymentPlanCommandHandler(db).Handle(new CreatePaymentPlanCommand
            {
                InvoiceId = _invoiceId,
                InstallmentCount = 2
            }, CancellationToken.None);
            planId = created.Data!.Id;
        }

        // Pull the plan's installment ids back out.
        List<Guid> installmentIds;
        await using (var db = _harness.CreateDbContext())
        {
            installmentIds = await db.Installments
                .Where(i => i.PaymentPlanId == planId)
                .OrderBy(i => i.SequenceNumber)
                .Select(i => i.Id)
                .ToListAsync();
        }

        // Pay the first: plan stays active.
        await using (var db = _harness.CreateDbContext())
        {
            var r = await new MarkInstallmentPaidCommandHandler(db).Handle(new MarkInstallmentPaidCommand { InstallmentId = installmentIds[0] }, CancellationToken.None);
            r.IsSuccess.Should().BeTrue();
        }
        await using (var db = _harness.CreateDbContext())
        {
            (await db.PaymentPlans.AsNoTracking().FirstAsync(p => p.Id == planId)).Status.Should().Be("Active");
        }

        // Pay the second: plan completes.
        await using (var db = _harness.CreateDbContext())
        {
            await new MarkInstallmentPaidCommandHandler(db).Handle(new MarkInstallmentPaidCommand { InstallmentId = installmentIds[1] }, CancellationToken.None);
        }
        await using (var db = _harness.CreateDbContext())
        {
            (await db.PaymentPlans.AsNoTracking().FirstAsync(p => p.Id == planId)).Status.Should().Be("Completed");
        }
    }

    [Fact]
    public async Task Marking_an_already_paid_installment_is_rejected()
    {
        Guid installmentId;
        await using (var db = _harness.CreateDbContext())
        {
            var created = await new CreatePaymentPlanCommandHandler(db).Handle(new CreatePaymentPlanCommand
            {
                InvoiceId = _invoiceId,
                InstallmentCount = 2
            }, CancellationToken.None);
            installmentId = created.Data!.Installments[0].Id;
        }

        await using (var db = _harness.CreateDbContext())
        {
            await new MarkInstallmentPaidCommandHandler(db).Handle(new MarkInstallmentPaidCommand { InstallmentId = installmentId }, CancellationToken.None);
        }

        await using var db2 = _harness.CreateDbContext();
        var again = await new MarkInstallmentPaidCommandHandler(db2).Handle(new MarkInstallmentPaidCommand { InstallmentId = installmentId }, CancellationToken.None);
        again.IsSuccess.Should().BeFalse();
        again.Error.Should().Contain("already");
    }

    [Fact]
    public async Task Get_by_invoice_returns_the_plan_with_its_installments()
    {
        await using (var db = _harness.CreateDbContext())
        {
            await new CreatePaymentPlanCommandHandler(db).Handle(new CreatePaymentPlanCommand
            {
                InvoiceId = _invoiceId,
                InstallmentCount = 4
            }, CancellationToken.None);
        }

        await using var db2 = _harness.CreateDbContext();
        var result = await new GetPaymentPlanByInvoiceQueryHandler(db2)
            .Handle(new GetPaymentPlanByInvoiceQuery(_invoiceId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Installments.Should().HaveCount(4);
        result.Data.TotalScheduled.Should().Be(95m);
        result.Data.Currency.Should().Be("USD");
    }

    [Fact]
    public async Task A_plan_cannot_be_created_for_a_fully_paid_invoice()
    {
        // Settle the invoice first.
        await using (var db = _harness.CreateDbContext())
        {
            var invoice = await db.Invoices.FirstAsync(i => i.Id == _invoiceId);
            invoice.PaidAmount = invoice.TotalAmount;
            await db.SaveChangesAsync();
        }

        await using var db2 = _harness.CreateDbContext();
        var result = await new CreatePaymentPlanCommandHandler(db2).Handle(new CreatePaymentPlanCommand
        {
            InvoiceId = _invoiceId,
            InstallmentCount = 3
        }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("no outstanding balance");
    }
}
