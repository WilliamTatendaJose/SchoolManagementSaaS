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

    /// <summary>
    /// Percentage (0-100) discounted off the term fees for each student who has at least
    /// one older sibling in the billing run (siblings share a guardian; the eldest pays
    /// full). Zero disables sibling discounting.
    /// </summary>
    public decimal SiblingDiscountPercent { get; init; }

    /// <summary>
    /// When true, each student's outstanding balance from prior invoices is added to the
    /// new invoice as an "Arrears brought forward" line item.
    /// </summary>
    public bool CarryForwardArrears { get; init; }
}

public record InvoiceGenerationResultDto
{
    public int InvoicesCreated { get; init; }
    public int StudentsSkipped { get; init; }
    public int StudentsWithoutFees { get; init; }

    /// <summary>Gross term fees billed, before discounts and excluding arrears.</summary>
    public decimal TotalBilled { get; init; }

    /// <summary>Total sibling discount applied across the run.</summary>
    public decimal TotalDiscount { get; init; }

    /// <summary>Total arrears carried forward onto the new invoices.</summary>
    public decimal TotalArrearsCarriedForward { get; init; }
}
