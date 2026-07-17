using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.Common.Security;
using SMS.Application.Features.Notifications.Commands;
using SMS.Application.Features.Notifications.Queries;
using SMS.Infrastructure.Authorization;

namespace SMS.API.Controllers;

/// <summary>
/// Parent communication: send announcements and fee reminders, and review delivery.
/// </summary>
[Authorize]
public class MessagesController : BaseApiController
{
    /// <summary>
    /// Get a paginated list of messages with delivery counts.
    /// </summary>
    [HttpGet]
    [RequirePermission(Permissions.MessagesView)]
    public async Task<IActionResult> GetMessages([FromQuery] GetMessagesQuery query)
    {
        var result = await Mediator.Send(query);

        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return Ok(result.Data);
    }

    /// <summary>
    /// Get a message with its per-recipient delivery breakdown.
    /// </summary>
    [HttpGet("{id:guid}")]
    [RequirePermission(Permissions.MessagesView)]
    public async Task<IActionResult> GetMessage(Guid id)
    {
        var result = await Mediator.Send(new GetMessageByIdQuery(id));

        if (!result.IsSuccess)
            return NotFound(result.Error);

        return Ok(result.Data);
    }

    /// <summary>
    /// Send a message to the guardians of a chosen audience.
    /// </summary>
    [HttpPost("send")]
    [RequirePermission(Permissions.MessagesSend)]
    public async Task<IActionResult> Send([FromBody] SendMessageCommand command)
    {
        var result = await Mediator.Send(command);

        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return Ok(result.Data);
    }

    /// <summary>
    /// Send personalised fee reminders to guardians of students with outstanding balances.
    /// </summary>
    [HttpPost("fee-reminders")]
    [RequirePermission(Permissions.MessagesBulk)]
    public async Task<IActionResult> SendFeeReminders([FromBody] SendFeeRemindersCommand command)
    {
        var result = await Mediator.Send(command);

        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return Ok(result.Data);
    }
}
