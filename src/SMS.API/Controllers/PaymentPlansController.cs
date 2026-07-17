using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.Common.Security;
using SMS.Application.Features.PaymentPlans.Commands;
using SMS.Application.Features.PaymentPlans.Queries;
using SMS.Infrastructure.Authorization;

namespace SMS.API.Controllers;

/// <summary>
/// API endpoints for invoice payment plans (installment schedules).
/// </summary>
[Authorize]
public class PaymentPlansController : BaseApiController
{
    /// <summary>
    /// Get the payment plan for an invoice, if one exists.
    /// </summary>
    [HttpGet("invoices/{invoiceId:guid}")]
    [RequirePermission(Permissions.FinanceView)]
    public async Task<IActionResult> GetByInvoice(Guid invoiceId)
    {
        var result = await Mediator.Send(new GetPaymentPlanByInvoiceQuery(invoiceId));

        if (!result.IsSuccess)
            return NotFound(result.Error);

        return Ok(result.Data);
    }

    /// <summary>
    /// Create an installment schedule for an invoice's outstanding balance.
    /// </summary>
    [HttpPost]
    [RequirePermission(Permissions.InvoicesEdit)]
    public async Task<IActionResult> Create([FromBody] CreatePaymentPlanCommand command)
    {
        var result = await Mediator.Send(command);

        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return Ok(result.Data);
    }

    /// <summary>
    /// Mark an installment as paid.
    /// </summary>
    [HttpPost("installments/{installmentId:guid}/pay")]
    [RequirePermission(Permissions.PaymentsRecord)]
    public async Task<IActionResult> MarkInstallmentPaid(Guid installmentId, [FromBody] MarkInstallmentPaidCommand? command)
    {
        var result = await Mediator.Send(new MarkInstallmentPaidCommand
        {
            InstallmentId = installmentId,
            PaidDate = command?.PaidDate
        });

        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return NoContent();
    }
}
