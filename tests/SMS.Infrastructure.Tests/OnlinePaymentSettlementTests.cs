using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Features.Payments.Commands;
using SMS.Application.Interfaces;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using Xunit;
using AcademicTermEntity = SMS.Domain.Entities.AcademicTerm;

namespace SMS.Infrastructure.Tests;

/// <summary>
/// A configurable fake gateway so settlement logic can be exercised without HTTP.
/// </summary>
internal sealed class FakeGatewayService : IPaymentGatewayService
{
    public PaymentStatusResult NextStatus { get; set; } = new() { IsValid = true, Status = GatewayPaymentStatus.Created };

    public Task<PaymentInitiationResult> InitiatePaymentAsync(PaymentInitiationRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult(new PaymentInitiationResult { Success = true, RedirectUrl = "https://pay", PollUrl = "https://poll" });

    public Task<PaymentStatusResult> CheckStatusAsync(string pollUrl, CancellationToken cancellationToken = default)
        => Task.FromResult(NextStatus);

    public PaymentStatusResult ParseStatusCallback(IReadOnlyList<KeyValuePair<string, string>> fields) => NextStatus;
}

public class OnlinePaymentSettlementTests : IAsyncLifetime
{
    private readonly TenantTestContext _harness = new();
    private Guid _invoiceId;
    private Guid _paymentId;

    private const decimal InvoiceTotal = 600m;
    private const decimal PaymentAmount = 600m;

    public async Task InitializeAsync()
    {
        var tenant = new Tenant { Name = "Pay School", Code = "PAY" };
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

            var student = new Student
            {
                StudentNumber = "S-PAY-1",
                FirstName = "Payer",
                LastName = "Student",
                DateOfBirth = new DateTime(2012, 1, 1),
                Gender = Gender.Other,
                AdmissionDate = new DateTime(2026, 1, 1),
                Status = StudentStatus.Active
            };
            db.Students.Add(student);
            await db.SaveChangesAsync();

            var invoice = new Invoice
            {
                InvoiceNumber = "INV-2026-000001",
                StudentId = student.Id,
                AcademicTermId = term.Id,
                InvoiceDate = new DateTime(2026, 1, 15),
                DueDate = new DateTime(2026, 2, 15),
                TotalAmount = InvoiceTotal,
                DiscountAmount = 0,
                PaidAmount = 0
            };
            db.Invoices.Add(invoice);
            await db.SaveChangesAsync();
            _invoiceId = invoice.Id;

            var payment = new Payment
            {
                ReceiptNumber = "PAY-INV-2026-000001-ref",
                InvoiceId = invoice.Id,
                Amount = PaymentAmount,
                PaymentMethod = PaymentMethod.MobileMoney,
                Status = PaymentStatus.Pending,
                PaymentDate = DateTime.UtcNow,
                TransactionReference = "PAY-INV-2026-000001-ref",
                GatewayPollUrl = "https://www.paynow.co.zw/interface/pollstatus"
            };
            db.Payments.Add(payment);
            await db.SaveChangesAsync();
            _paymentId = payment.Id;
        }
    }

    public Task DisposeAsync()
    {
        _harness.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Polling_a_paid_status_settles_the_invoice_once_and_is_idempotent()
    {
        var gateway = new FakeGatewayService
        {
            NextStatus = new PaymentStatusResult { IsValid = true, Status = GatewayPaymentStatus.Paid, Reference = "PAY-INV-2026-000001-ref", Amount = PaymentAmount }
        };

        // First poll: settles the payment and credits the invoice.
        await using (var db = _harness.CreateDbContext())
        {
            var outcome = await new CheckPaynowStatusCommandHandler(db, gateway)
                .Handle(new CheckPaynowStatusCommand(_paymentId), CancellationToken.None);

            outcome.IsSuccess.Should().BeTrue();
            outcome.Data!.Settled.Should().BeTrue();
            outcome.Data.Status.Should().Be(nameof(PaymentStatus.Completed));
            outcome.Data.InvoiceBalance.Should().Be(0m);
        }

        // Second poll: no double credit.
        await using (var db = _harness.CreateDbContext())
        {
            var outcome = await new CheckPaynowStatusCommandHandler(db, gateway)
                .Handle(new CheckPaynowStatusCommand(_paymentId), CancellationToken.None);

            outcome.Data!.Settled.Should().BeFalse();
        }

        await using (var verify = _harness.CreateDbContext())
        {
            var invoice = await verify.Invoices.AsNoTracking().FirstAsync(i => i.Id == _invoiceId);
            invoice.PaidAmount.Should().Be(PaymentAmount, "the invoice must be credited exactly once");

            var payment = await verify.Payments.AsNoTracking().FirstAsync(p => p.Id == _paymentId);
            payment.Status.Should().Be(PaymentStatus.Completed);
        }
    }

    [Fact]
    public async Task A_cancelled_status_marks_the_payment_without_crediting_the_invoice()
    {
        var gateway = new FakeGatewayService
        {
            NextStatus = new PaymentStatusResult { IsValid = true, Status = GatewayPaymentStatus.Cancelled, Reference = "PAY-INV-2026-000001-ref" }
        };

        await using (var db = _harness.CreateDbContext())
        {
            var outcome = await new CheckPaynowStatusCommandHandler(db, gateway)
                .Handle(new CheckPaynowStatusCommand(_paymentId), CancellationToken.None);

            outcome.IsSuccess.Should().BeTrue();
            outcome.Data!.Settled.Should().BeFalse();
            outcome.Data.Status.Should().Be(nameof(PaymentStatus.Cancelled));
        }

        await using (var verify = _harness.CreateDbContext())
        {
            var invoice = await verify.Invoices.AsNoTracking().FirstAsync(i => i.Id == _invoiceId);
            invoice.PaidAmount.Should().Be(0m);
        }
    }

    [Fact]
    public async Task Result_callback_settles_by_transaction_reference()
    {
        var gateway = new FakeGatewayService
        {
            NextStatus = new PaymentStatusResult { IsValid = true, Status = GatewayPaymentStatus.Paid, Reference = "PAY-INV-2026-000001-ref", Amount = PaymentAmount }
        };

        await using (var db = _harness.CreateDbContext())
        {
            var outcome = await new ProcessPaynowResultCommandHandler(db, gateway)
                .Handle(new ProcessPaynowResultCommand([]), CancellationToken.None);

            outcome.IsSuccess.Should().BeTrue();
            outcome.Data!.Settled.Should().BeTrue();
        }

        await using (var verify = _harness.CreateDbContext())
        {
            var invoice = await verify.Invoices.AsNoTracking().FirstAsync(i => i.Id == _invoiceId);
            invoice.PaidAmount.Should().Be(PaymentAmount);
        }
    }
}
