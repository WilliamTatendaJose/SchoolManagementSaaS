using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Finance.Queries;

/// <summary>Generates a branded PDF for an invoice.</summary>
public record GenerateInvoiceQuery(Guid InvoiceId) : IRequest<Result<InvoiceFileDto>>;

public record InvoiceFileDto
{
    public string FileName { get; init; } = string.Empty;
    public byte[] Content { get; init; } = [];
}
