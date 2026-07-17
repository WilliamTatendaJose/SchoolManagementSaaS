using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Finance.Queries;

/// <summary>Generates a PDF receipt for a completed payment.</summary>
public record GenerateReceiptQuery(Guid PaymentId) : IRequest<Result<ReceiptFileDto>>;

public record ReceiptFileDto
{
    public string FileName { get; init; } = string.Empty;
    public byte[] Content { get; init; } = [];
}
