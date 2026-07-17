using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Finance.Queries;

/// <summary>
/// Summarizes completed payments over a date range, grouped by payment method — a daily
/// cashier reconciliation.
/// </summary>
public record GetCashierReconciliationQuery : IRequest<Result<ReconciliationDto>>
{
    public DateTime FromDate { get; init; }
    public DateTime ToDate { get; init; }
}

public record ReconciliationDto
{
    public DateTime FromDate { get; init; }
    public DateTime ToDate { get; init; }
    public decimal TotalCollected { get; init; }
    public int PaymentCount { get; init; }
    public List<MethodBreakdownDto> ByMethod { get; init; } = [];
}

public record MethodBreakdownDto
{
    public string PaymentMethod { get; init; } = string.Empty;
    public int Count { get; init; }
    public decimal Amount { get; init; }
}
