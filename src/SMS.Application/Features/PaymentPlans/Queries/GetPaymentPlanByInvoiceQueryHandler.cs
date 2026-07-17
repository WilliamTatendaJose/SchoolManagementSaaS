using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Features.PaymentPlans.Commands;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.PaymentPlans.Queries;

public class GetPaymentPlanByInvoiceQueryHandler : IRequestHandler<GetPaymentPlanByInvoiceQuery, Result<PaymentPlanDto>>
{
    private readonly IApplicationDbContext _context;

    public GetPaymentPlanByInvoiceQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PaymentPlanDto>> Handle(GetPaymentPlanByInvoiceQuery request, CancellationToken cancellationToken)
    {
        var plan = await _context.PaymentPlans
            .AsNoTracking()
            .Include(p => p.Installments)
            .Include(p => p.Invoice)
            .Where(p => p.InvoiceId == request.InvoiceId)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (plan == null)
        {
            return Result<PaymentPlanDto>.Failure("No payment plan found for this invoice");
        }

        var dto = new PaymentPlanDto
        {
            Id = plan.Id,
            InvoiceId = plan.InvoiceId,
            InstallmentCount = plan.InstallmentCount,
            StartDate = plan.StartDate,
            Status = plan.Status,
            Currency = plan.Invoice.Currency,
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

        return Result<PaymentPlanDto>.Success(dto);
    }
}
