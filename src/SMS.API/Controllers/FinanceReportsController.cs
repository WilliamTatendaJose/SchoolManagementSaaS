using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.Common.Security;
using SMS.Application.Features.Finance.Queries;
using SMS.Infrastructure.Authorization;

namespace SMS.API.Controllers;

/// <summary>
/// Finance reports and documents: defaulters, cashier reconciliation, and PDF receipts.
/// </summary>
[Authorize]
[Route("api/finance/reports")]
public class FinanceReportsController : BaseApiController
{
    [HttpGet("defaulters")]
    [RequirePermission(Permissions.FinanceReport)]
    public async Task<IActionResult> GetDefaulters([FromQuery] GetDefaultersReportQuery query)
    {
        var result = await Mediator.Send(query);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
    }

    [HttpGet("reconciliation")]
    [RequirePermission(Permissions.FinanceReport)]
    public async Task<IActionResult> GetReconciliation([FromQuery] GetCashierReconciliationQuery query)
    {
        var result = await Mediator.Send(query);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
    }

    [HttpGet("receipts/{paymentId:guid}")]
    [RequirePermission(Permissions.FinanceView)]
    public async Task<IActionResult> GetReceipt(Guid paymentId)
    {
        var result = await Mediator.Send(new GenerateReceiptQuery(paymentId));
        return result.IsSuccess
            ? File(result.Data!.Content, "application/pdf", result.Data.FileName)
            : NotFound(result.Error);
    }
}
