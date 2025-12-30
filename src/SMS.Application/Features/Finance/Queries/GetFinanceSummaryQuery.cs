using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Finance.Queries;

public record GetFinanceSummaryQuery : IRequest<Result<FinanceSummaryDto>>
{
    public Guid? AcademicTermId { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
}

public record FinanceSummaryDto
{
    public decimal TotalBilled { get; init; }
    public decimal TotalCollected { get; init; }
    public decimal TotalOutstanding { get; init; }
    public decimal CollectionRate { get; init; }
    public int TotalInvoices { get; init; }
    public int PaidInvoices { get; init; }
    public int UnpaidInvoices { get; init; }
    public int OverdueInvoices { get; init; }
    public List<PaymentMethodBreakdownDto> PaymentMethodBreakdown { get; init; } = [];
}

public record PaymentMethodBreakdownDto
{
    public string Method { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public int Count { get; init; }
}
