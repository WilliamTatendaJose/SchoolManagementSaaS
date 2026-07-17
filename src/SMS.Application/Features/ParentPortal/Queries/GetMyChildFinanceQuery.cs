using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.ParentPortal.Queries;

/// <summary>Invoices and balance for one of the signed-in parent's children.</summary>
public record GetMyChildFinanceQuery(Guid StudentId) : IRequest<Result<MyChildFinanceDto>>;

public record MyChildFinanceDto
{
    public Guid StudentId { get; init; }
    public decimal TotalBilled { get; init; }
    public decimal TotalPaid { get; init; }
    public decimal OutstandingBalance { get; init; }
    public List<MyChildInvoiceDto> Invoices { get; init; } = [];
}

public record MyChildInvoiceDto
{
    public Guid InvoiceId { get; init; }
    public string InvoiceNumber { get; init; } = string.Empty;
    public DateTime InvoiceDate { get; init; }
    public DateTime DueDate { get; init; }
    public decimal TotalAmount { get; init; }
    public decimal Balance { get; init; }

    /// <summary>True when this invoice still has a balance the parent can pay online.</summary>
    public bool Payable { get; init; }
}
