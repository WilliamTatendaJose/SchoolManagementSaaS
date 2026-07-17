using MediatR;
using SMS.Application.Common.Models;
using SMS.Application.Features.PaymentPlans.Commands;

namespace SMS.Application.Features.PaymentPlans.Queries;

/// <summary>
/// Returns the most recent payment plan (with its installments) for an invoice, if one exists.
/// </summary>
public record GetPaymentPlanByInvoiceQuery(Guid InvoiceId) : IRequest<Result<PaymentPlanDto>>;
