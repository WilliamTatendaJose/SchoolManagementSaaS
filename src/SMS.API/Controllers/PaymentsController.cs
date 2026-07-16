using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.Common.Security;
using SMS.Application.Features.Payments.Commands;
using SMS.Application.Interfaces;
using SMS.Infrastructure.Authorization;

namespace SMS.API.Controllers;

/// <summary>
/// Online payment (Paynow) endpoints: initiate a payment, poll its status, and receive
/// the asynchronous result callback.
/// </summary>
[Authorize]
public class PaymentsController : BaseApiController
{
    private readonly ITenantService _tenantService;

    public PaymentsController(ITenantService tenantService)
    {
        _tenantService = tenantService;
    }

    /// <summary>
    /// Start an online payment against an invoice; returns the redirect/poll URLs.
    /// </summary>
    [HttpPost("online/initiate")]
    [RequirePermission(Permissions.PaymentsRecord)]
    public async Task<IActionResult> Initiate([FromBody] InitiateOnlinePaymentCommand command)
    {
        var result = await Mediator.Send(command);

        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return Ok(result.Data);
    }

    /// <summary>
    /// Poll the gateway for the current status of a payment and settle if paid.
    /// </summary>
    [HttpPost("{id:guid}/status")]
    [RequirePermission(Permissions.PaymentsRecord)]
    public async Task<IActionResult> CheckStatus(Guid id)
    {
        var result = await Mediator.Send(new CheckPaynowStatusCommand(id));

        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return Ok(result.Data);
    }

    /// <summary>
    /// Asynchronous Paynow result callback. Unauthenticated; the tenant is taken from the
    /// route so the payment can be resolved within the correct tenant scope.
    /// </summary>
    [HttpPost("paynow/result/{tenantId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> PaynowResult(Guid tenantId)
    {
        _tenantService.SetCurrentTenant(tenantId);

        var fields = Request.Form
            .Select(f => new KeyValuePair<string, string>(f.Key, f.Value.ToString()))
            .ToList();

        var result = await Mediator.Send(new ProcessPaynowResultCommand(fields));

        // Paynow retries on non-200, so acknowledge once the callback has been processed.
        return result.IsSuccess ? Ok() : BadRequest(result.Error);
    }
}
