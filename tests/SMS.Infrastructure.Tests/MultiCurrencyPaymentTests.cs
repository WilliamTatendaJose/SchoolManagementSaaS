using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Features.Finance.Commands;
using SMS.Application.Features.Finance.Queries;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using Xunit;
using AcademicTermEntity = SMS.Domain.Entities.AcademicTerm;

namespace SMS.Infrastructure.Tests;

public class MultiCurrencyPaymentTests : IAsyncLifetime
{
    private readonly TenantTestContext _harness = new();
    private Guid _invoiceId;

    public async Task InitializeAsync()
    {
        var tenant = new Tenant { Name = "Currency School", Code = "CUR", Currency = "USD" };
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
            var student = new Student { StudentNumber = "S-CUR", FirstName = "Cur", LastName = "Rency", DateOfBirth = new DateTime(2012, 1, 1), Gender = Gender.Other, AdmissionDate = new DateTime(2026, 1, 1), Status = StudentStatus.Active };
            db.Students.Add(student);
            await db.SaveChangesAsync();

            // A USD invoice for 100.
            var invoice = new Invoice { InvoiceNumber = "INV-USD", StudentId = student.Id, AcademicTermId = term.Id, InvoiceDate = new DateTime(2026, 1, 15), DueDate = new DateTime(2026, 2, 15), TotalAmount = 100, Currency = "USD" };
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
    public async Task A_ZWG_payment_credits_the_USD_invoice_by_the_converted_amount()
    {
        await using var db = _harness.CreateDbContext();
        var result = await new RecordPaymentCommandHandler(db, _harness.CurrentUser).Handle(new RecordPaymentCommand
        {
            InvoiceId = _invoiceId,
            Amount = 3600m,           // ZWG tendered
            Currency = "ZWG",
            ExchangeRate = 0.025m,    // 3600 ZWG -> 90 USD
            PaymentMethod = "MobileMoney"
        }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.RemainingBalance.Should().Be(10m, "100 USD invoice minus the 90 USD equivalent");

        var invoice = await db.Invoices.AsNoTracking().FirstAsync(i => i.Id == _invoiceId);
        invoice.PaidAmount.Should().Be(90m);

        var payment = await db.Payments.AsNoTracking().FirstAsync();
        payment.Currency.Should().Be("ZWG");
        payment.Amount.Should().Be(3600m);
        payment.AmountInInvoiceCurrency.Should().Be(90m);
    }

    [Fact]
    public async Task Reconciliation_keeps_currencies_separate()
    {
        await using (var db = _harness.CreateDbContext())
        {
            var handler = new RecordPaymentCommandHandler(db, _harness.CurrentUser);
            await handler.Handle(new RecordPaymentCommand { InvoiceId = _invoiceId, Amount = 50m, Currency = "USD", PaymentMethod = "Cash", PaymentDate = new DateTime(2026, 3, 2) }, CancellationToken.None);
            await handler.Handle(new RecordPaymentCommand { InvoiceId = _invoiceId, Amount = 800m, Currency = "ZWG", ExchangeRate = 0.025m, PaymentMethod = "MobileMoney", PaymentDate = new DateTime(2026, 3, 2) }, CancellationToken.None);
        }

        await using var db2 = _harness.CreateDbContext();
        var recon = await new GetCashierReconciliationQueryHandler(db2)
            .Handle(new GetCashierReconciliationQuery { FromDate = new DateTime(2026, 3, 1), ToDate = new DateTime(2026, 3, 31) }, CancellationToken.None);

        recon.Data!.ByCurrency.Should().HaveCount(2);
        recon.Data.ByCurrency.Single(c => c.Currency == "USD").TotalCollected.Should().Be(50m);
        recon.Data.ByCurrency.Single(c => c.Currency == "ZWG").TotalCollected.Should().Be(800m, "reconciliation reports tendered amounts, not converted");
    }
}
