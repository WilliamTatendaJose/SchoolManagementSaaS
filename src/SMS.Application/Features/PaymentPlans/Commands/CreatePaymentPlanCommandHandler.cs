using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;
using SMS.Domain.Entities;

namespace SMS.Application.Features.PaymentPlans.Commands;

public class CreatePaymentPlanCommandHandler : IRequestHandler<CreatePaymentPlanCommand, Result<PaymentPlanDto>>
{
    private readonly IApplicationDbContext _context;

    public CreatePaymentPlanCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PaymentPlanDto>> Handle(CreatePaymentPlanCommand request, CancellationToken cancellationToken)
    {
        var invoice = await _context.Invoices.FindAsync([request.InvoiceId], cancellationToken);

        if (invoice == null)
        {
            return Result<PaymentPlanDto>.Failure("Invoice not found");
        }

        var existing = await _context.PaymentPlans
            .AnyAsync(p => p.InvoiceId == request.InvoiceId && p.Status == "Active", cancellationToken);

        if (existing)
        {
            return Result<PaymentPlanDto>.Failure("An active payment plan already exists for this invoice");
        }

        var balance = invoice.Balance;

        if (balance <= 0)
        {
            return Result<PaymentPlanDto>.Failure("Invoice has no outstanding balance to schedule");
        }

        var startDate = (request.StartDate ?? DateTime.UtcNow).Date;

        var plan = new PaymentPlan
        {
            InvoiceId = invoice.Id,
            InstallmentCount = request.InstallmentCount,
            StartDate = startDate,
            Status = "Active"
        };

        // Split the balance evenly, rounding each installment to cents and carrying the
        // rounding remainder onto the final installment so the schedule sums to the balance exactly.
        var perInstallment = Math.Round(balance / request.InstallmentCount, 2, MidpointRounding.AwayFromZero);

        for (var i = 0; i < request.InstallmentCount; i++)
        {
            var isLast = i == request.InstallmentCount - 1;
            var amount = isLast ? balance - perInstallment * (request.InstallmentCount - 1) : perInstallment;

            plan.Installments.Add(new Installment
            {
                SequenceNumber = i + 1,
                DueDate = NextDueDate(startDate, i, request.Frequency),
                Amount = amount,
                IsPaid = false
            });
        }

        _context.PaymentPlans.Add(plan);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<PaymentPlanDto>.Success(ToDto(plan, invoice.Currency));
    }

    private static DateTime NextDueDate(DateTime start, int index, string frequency) =>
        frequency.Equals("Weekly", StringComparison.OrdinalIgnoreCase)
            ? start.AddDays(7 * index)
            : start.AddMonths(index);

    private static PaymentPlanDto ToDto(PaymentPlan plan, string currency) => new()
    {
        Id = plan.Id,
        InvoiceId = plan.InvoiceId,
        InstallmentCount = plan.InstallmentCount,
        StartDate = plan.StartDate,
        Status = plan.Status,
        Currency = currency,
        TotalScheduled = plan.Installments.Sum(i => i.Amount),
        Installments = plan.Installments
            .OrderBy(i => i.SequenceNumber)
            .Select(i => new InstallmentDto
            {
                Id = i.Id,
                SequenceNumber = i.SequenceNumber,
                DueDate = i.DueDate,
                Amount = i.Amount,
                IsPaid = i.IsPaid,
                PaidDate = i.PaidDate
            })
            .ToList()
    };
}
