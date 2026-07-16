using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Fees.Commands;

/// <summary>
/// Command to bulk-generate term invoices for actively-enrolled students from the
/// fee structures defined for their class. Idempotent: students who already have an
/// invoice for the term are skipped.
/// </summary>
public record GenerateInvoicesCommand : IRequest<Result<InvoiceGenerationResultDto>>
{
    public Guid AcademicTermId { get; init; }
    public Guid? ClassId { get; init; }
    public DateTime DueDate { get; init; }
    public bool IncludeOptionalFees { get; init; }
}

public record InvoiceGenerationResultDto
{
    public int InvoicesCreated { get; init; }
    public int StudentsSkipped { get; init; }
    public int StudentsWithoutFees { get; init; }
    public decimal TotalBilled { get; init; }
}
