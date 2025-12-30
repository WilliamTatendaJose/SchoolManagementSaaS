using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Finance.Queries;

public record GetInvoicesQuery : IRequest<Result<PaginatedList<InvoiceListDto>>>
{
    public Guid? StudentId { get; init; }
    public Guid? AcademicTermId { get; init; }
    public bool? UnpaidOnly { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public record InvoiceListDto
{
    public Guid Id { get; init; }
    public string InvoiceNumber { get; init; } = string.Empty;
    public Guid StudentId { get; init; }
    public string StudentName { get; init; } = string.Empty;
    public string StudentNumber { get; init; } = string.Empty;
    public string TermName { get; init; } = string.Empty;
    public DateTime InvoiceDate { get; init; }
    public DateTime DueDate { get; init; }
    public decimal TotalAmount { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal PaidAmount { get; init; }
    public decimal Balance { get; init; }
    public bool IsOverdue { get; init; }
}
