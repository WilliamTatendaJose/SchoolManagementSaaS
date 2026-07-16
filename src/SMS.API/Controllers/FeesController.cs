using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.Common.Security;
using SMS.Application.Features.Fees.Commands;
using SMS.Application.Features.Fees.Queries;
using SMS.Infrastructure.Authorization;

namespace SMS.API.Controllers;

/// <summary>
/// API endpoints for fee structures and bulk invoice generation
/// </summary>
[Authorize]
public class FeesController : BaseApiController
{
    /// <summary>
    /// Get fee structures, optionally filtered by class and/or academic year
    /// </summary>
    [HttpGet("structures")]
    [RequirePermission(Permissions.FeeStructuresView)]
    public async Task<IActionResult> GetFeeStructures([FromQuery] GetFeeStructuresQuery query)
    {
        var result = await Mediator.Send(query);

        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return Ok(result.Data);
    }

    /// <summary>
    /// Create a fee structure
    /// </summary>
    [HttpPost("structures")]
    [RequirePermission(Permissions.FeeStructuresManage)]
    public async Task<IActionResult> CreateFeeStructure([FromBody] CreateFeeStructureCommand command)
    {
        var result = await Mediator.Send(command);

        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return Ok(new { Id = result.Data });
    }

    /// <summary>
    /// Update a fee structure
    /// </summary>
    [HttpPut("structures/{id:guid}")]
    [RequirePermission(Permissions.FeeStructuresManage)]
    public async Task<IActionResult> UpdateFeeStructure(Guid id, [FromBody] UpdateFeeStructureCommand command)
    {
        if (id != command.Id)
            return BadRequest("ID mismatch");

        var result = await Mediator.Send(command);

        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return NoContent();
    }

    /// <summary>
    /// Delete a fee structure
    /// </summary>
    [HttpDelete("structures/{id:guid}")]
    [RequirePermission(Permissions.FeeStructuresManage)]
    public async Task<IActionResult> DeleteFeeStructure(Guid id)
    {
        var result = await Mediator.Send(new DeleteFeeStructureCommand(id));

        if (!result.IsSuccess)
            return NotFound(result.Error);

        return NoContent();
    }

    /// <summary>
    /// Bulk-generate term invoices for actively-enrolled students from their class fee structures
    /// </summary>
    [HttpPost("invoices/generate")]
    [RequirePermission(Permissions.InvoicesCreate)]
    public async Task<IActionResult> GenerateInvoices([FromBody] GenerateInvoicesCommand command)
    {
        var result = await Mediator.Send(command);

        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return Ok(result.Data);
    }
}
