using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;
using SMS.Domain.Enums;

namespace SMS.Application.Features.Finance.Queries;

public class GetCashierReconciliationQueryHandler : IRequestHandler<GetCashierReconciliationQuery, Result<ReconciliationDto>>
{
    private readonly IApplicationDbContext _context;

    public GetCashierReconciliationQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<ReconciliationDto>> Handle(GetCashierReconciliationQuery request, CancellationToken cancellationToken)
    {
        var from = request.FromDate.Date;
        var to = request.ToDate.Date.AddDays(1).AddTicks(-1);

        if (to < from)
        {
            return Result<ReconciliationDto>.Failure("The end date cannot be before the start date");
        }

        var payments = await _context.Payments
            .AsNoTracking()
            .Where(p => p.Status == PaymentStatus.Completed && p.PaymentDate >= from && p.PaymentDate <= to)
            .Select(p => new { p.PaymentMethod, p.Amount })
            .ToListAsync(cancellationToken);

        var byMethod = payments
            .GroupBy(p => p.PaymentMethod)
            .Select(g => new MethodBreakdownDto
            {
                PaymentMethod = g.Key.ToString(),
                Count = g.Count(),
                Amount = g.Sum(x => x.Amount)
            })
            .OrderByDescending(m => m.Amount)
            .ToList();

        var dto = new ReconciliationDto
        {
            FromDate = from,
            ToDate = request.ToDate.Date,
            TotalCollected = payments.Sum(p => p.Amount),
            PaymentCount = payments.Count,
            ByMethod = byMethod
        };

        return Result<ReconciliationDto>.Success(dto);
    }
}
