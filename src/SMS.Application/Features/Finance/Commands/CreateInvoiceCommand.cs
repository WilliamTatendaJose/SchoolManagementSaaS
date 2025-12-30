using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Finance.Commands;

public record CreateInvoiceCommand : IRequest<Result<Guid>>
{
    public Guid StudentId { get; init; }
    public Guid AcademicTermId { get; init; }
    public DateTime DueDate { get; init; }
    public decimal? DiscountAmount { get; init; }
    public string? Notes { get; init; }
    public List<InvoiceItemDto> Items { get; init; } = [];
}

public record InvoiceItemDto
{
    public Guid? FeeStructureId { get; init; }
    public string Description { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public int Quantity { get; init; } = 1;
}
