using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Payments.Commands;

/// <summary>
/// Processes an asynchronous Paynow result callback. Fields must be in the order the
/// gateway posted them so the hash verifies.
/// </summary>
public record ProcessPaynowResultCommand(IReadOnlyList<KeyValuePair<string, string>> Fields)
    : IRequest<Result<PaymentSettlementDto>>;
