using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.PaymentPlans.Commands;

/// <summary>
/// Splits an invoice's outstanding balance into a schedule of equal installments.
/// </summary>
public record CreatePaymentPlanCommand : IRequest<Result<PaymentPlanDto>>
{
    public Guid InvoiceId { get; init; }
    public int InstallmentCount { get; init; }

    /// <summary>Due date of the first installment; defaults to today.</summary>
    public DateTime? StartDate { get; init; }

    /// <summary>Spacing between installments: Monthly (default) or Weekly.</summary>
    public string Frequency { get; init; } = "Monthly";
}

public record PaymentPlanDto
{
    public Guid Id { get; init; }
    public Guid InvoiceId { get; init; }
    public int InstallmentCount { get; init; }
    public DateTime StartDate { get; init; }
    public string Status { get; init; } = string.Empty;
    public string Currency { get; init; } = string.Empty;
    public decimal TotalScheduled { get; init; }
    public IReadOnlyList<InstallmentDto> Installments { get; init; } = [];
}

public record InstallmentDto
{
    public Guid Id { get; init; }
    public int SequenceNumber { get; init; }
    public DateTime DueDate { get; init; }
    public decimal Amount { get; init; }
    public bool IsPaid { get; init; }
    public DateTime? PaidDate { get; init; }
}
