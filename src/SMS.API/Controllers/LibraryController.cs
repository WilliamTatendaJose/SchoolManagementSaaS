using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.Common.Security;
using SMS.Application.Features.Library.Commands;
using SMS.Application.Features.Library.Queries;
using SMS.Infrastructure.Authorization;

namespace SMS.API.Controllers;

/// <summary>
/// School library: book catalog and borrow/return of copies.
/// </summary>
[Authorize]
public class LibraryController : BaseApiController
{
    [HttpGet("books")]
    [RequirePermission(Permissions.LibraryView)]
    public async Task<IActionResult> GetBooks([FromQuery] string? search)
    {
        var result = await Mediator.Send(new GetBooksQuery { Search = search });
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
    }

    [HttpPost("books")]
    [RequirePermission(Permissions.LibraryManage)]
    public async Task<IActionResult> CreateBook([FromBody] CreateBookCommand command)
    {
        var result = await Mediator.Send(command);
        return result.IsSuccess ? Ok(new { Id = result.Data }) : BadRequest(result.Error);
    }

    [HttpGet("loans/overdue")]
    [RequirePermission(Permissions.LibraryView)]
    public async Task<IActionResult> GetOverdueLoans()
    {
        var result = await Mediator.Send(new GetOverdueLoansQuery());
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
    }

    [HttpGet("students/{studentId:guid}/loans")]
    [RequirePermission(Permissions.LibraryView)]
    public async Task<IActionResult> GetStudentLoans(Guid studentId)
    {
        var result = await Mediator.Send(new GetStudentLoansQuery(studentId));
        return result.IsSuccess ? Ok(result.Data) : NotFound(result.Error);
    }

    [HttpPost("loans/borrow")]
    [RequirePermission(Permissions.LibraryManage)]
    public async Task<IActionResult> BorrowBook([FromBody] BorrowBookCommand command)
    {
        var result = await Mediator.Send(command);
        return result.IsSuccess ? Ok(new { Id = result.Data }) : BadRequest(result.Error);
    }

    [HttpPost("loans/{loanId:guid}/return")]
    [RequirePermission(Permissions.LibraryManage)]
    public async Task<IActionResult> ReturnBook(Guid loanId, [FromBody] ReturnBookCommand command)
    {
        if (loanId != command.LoanId)
            return BadRequest("ID mismatch");

        var result = await Mediator.Send(command);
        return result.IsSuccess ? NoContent() : BadRequest(result.Error);
    }
}
