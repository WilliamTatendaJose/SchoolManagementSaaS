using MediatR;
using SMS.Application.Common.Models;
using SMS.Application.Features.Payments.Commands;

namespace SMS.Application.Features.ParentPortal.Commands;

/// <summary>
/// Polls the gateway for the status of a payment one of the signed-in parent's children
/// made, and settles the invoice if paid. Used on the portal's "return from Paynow" screen.
/// </summary>
public record CheckMyChildPaymentStatusCommand(Guid PaymentId) : IRequest<Result<PaymentSettlementDto>>;
