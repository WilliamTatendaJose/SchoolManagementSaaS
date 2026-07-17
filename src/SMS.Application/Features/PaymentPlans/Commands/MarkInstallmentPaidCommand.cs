using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.PaymentPlans.Commands;

/// <summary>
/// Marks a single installment as settled. When the final installment is paid the plan is completed.
/// </summary>
public record MarkInstallmentPaidCommand : IRequest<Result>
{
    public Guid InstallmentId { get; init; }
    public DateTime? PaidDate { get; init; }
}
