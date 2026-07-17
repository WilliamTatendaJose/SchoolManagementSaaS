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
            .Select(p => new { p.Currency, p.PaymentMethod, p.Amount })
            .ToListAsync(cancellationToken);

        var byCurrency = payments
            .GroupBy(p => p.Currency)
            .Select(currencyGroup => new CurrencyReconciliationDto
            {
                Currency = currencyGroup.Key,
                TotalCollected = currencyGroup.Sum(x => x.Amount),
                Count = currencyGroup.Count(),
                ByMethod = currencyGroup
                    .GroupBy(p => p.PaymentMethod)
                    .Select(methodGroup => new MethodBreakdownDto
                    {
                        PaymentMethod = methodGroup.Key.ToString(),
                        Count = methodGroup.Count(),
                        Amount = methodGroup.Sum(x => x.Amount)
                    })
                    .OrderByDescending(m => m.Amount)
                    .ToList()
            })
            .OrderBy(c => c.Currency)
            .ToList();

        var dto = new ReconciliationDto
        {
            FromDate = from,
            ToDate = request.ToDate.Date,
            PaymentCount = payments.Count,
            ByCurrency = byCurrency
        };

        return Result<ReconciliationDto>.Success(dto);
    }
}
