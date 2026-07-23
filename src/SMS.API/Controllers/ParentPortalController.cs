using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.Features.Academic.Queries;
using SMS.Application.Features.ParentPortal.Commands;
using SMS.Application.Features.ParentPortal.Queries;

namespace SMS.API.Controllers;

public class PortalSubmitAssignmentRequest
{
    public string? Comment { get; set; }
    public IFormFile? Attachment { get; set; }
}

/// <summary>
/// Read-only (plus fee payment) portal for parents/guardians, scoped to their own
/// children. Access control is by ownership (the caller's guardian record), so no
/// per-endpoint permission is required beyond authentication.
/// </summary>
[Authorize]
[Route("api/portal")]
public class ParentPortalController : BaseApiController
{
    [HttpGet("children")]
    public async Task<IActionResult> GetMyChildren()
    {
        var result = await Mediator.Send(new GetMyChildrenQuery());
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
    }

    /// <summary>
    /// Academic terms, for the results/report-card term picker. Not permission-gated -
    /// term names carry no per-student data, they're just tenant-wide reference data
    /// (already scoped to the caller's tenant by the standard query filter).
    /// </summary>
    [HttpGet("terms")]
    public async Task<IActionResult> GetTerms()
    {
        var result = await Mediator.Send(new GetAcademicTermsQuery());
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
    }

    [HttpGet("children/{studentId:guid}/finance")]
    public async Task<IActionResult> GetChildFinance(Guid studentId)
    {
        var result = await Mediator.Send(new GetMyChildFinanceQuery(studentId));
        return result.IsSuccess ? Ok(result.Data) : NotFound(result.Error);
    }

    [HttpGet("children/{studentId:guid}/results")]
    public async Task<IActionResult> GetChildResults(Guid studentId, [FromQuery] Guid? termId)
    {
        var result = await Mediator.Send(new GetMyChildResultsQuery { StudentId = studentId, AcademicTermId = termId });
        return result.IsSuccess ? Ok(result.Data) : NotFound(result.Error);
    }

    [HttpGet("children/{studentId:guid}/attendance")]
    public async Task<IActionResult> GetChildAttendance(Guid studentId, [FromQuery] Guid? termId)
    {
        var result = await Mediator.Send(new GetMyChildAttendanceQuery { StudentId = studentId, AcademicTermId = termId });
        return result.IsSuccess ? Ok(result.Data) : NotFound(result.Error);
    }

    /// <summary>Full profile (bio, class, guardians, balances) for one of the caller's children.</summary>
    [HttpGet("children/{studentId:guid}/profile")]
    public async Task<IActionResult> GetChildProfile(Guid studentId)
    {
        var result = await Mediator.Send(new GetMyChildProfileQuery(studentId));
        return result.IsSuccess ? Ok(result.Data) : NotFound(result.Error);
    }

    /// <summary>Assignments for one of the caller's children, with attachment + submission download links.</summary>
    [HttpGet("children/{studentId:guid}/assignments")]
    public async Task<IActionResult> GetChildAssignments(Guid studentId)
    {
        var result = await Mediator.Send(new GetMyChildAssignmentsQuery(studentId));
        return result.IsSuccess ? Ok(result.Data) : NotFound(result.Error);
    }

    /// <summary>Course materials (notes, worksheets, links) for one of the caller's children's class.</summary>
    [HttpGet("children/{studentId:guid}/materials")]
    public async Task<IActionResult> GetChildMaterials(Guid studentId)
    {
        var result = await Mediator.Send(new GetMyChildCourseMaterialsQuery(studentId));
        return result.IsSuccess ? Ok(result.Data) : NotFound(result.Error);
    }

    /// <summary>Submit (or replace, until graded) the caller's child's work for an assignment.</summary>
    [HttpPost("children/{studentId:guid}/assignments/{assignmentId:guid}/submit")]
    public async Task<IActionResult> SubmitChildAssignment(Guid studentId, Guid assignmentId, [FromForm] PortalSubmitAssignmentRequest request)
    {
        var (fileName, contentType, content) = await ReadAttachmentAsync(request.Attachment);

        var result = await Mediator.Send(new SubmitMyChildAssignmentCommand
        {
            StudentId = studentId,
            AssignmentId = assignmentId,
            Comment = request.Comment,
            AttachmentFileName = fileName,
            AttachmentContentType = contentType,
            AttachmentContent = content
        });

        return result.IsSuccess ? Ok(new { Id = result.Data }) : BadRequest(result.Error);
    }

    private static async Task<(string? FileName, string? ContentType, byte[]? Content)> ReadAttachmentAsync(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            return (null, null, null);
        }

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        return (file.FileName, file.ContentType, stream.ToArray());
    }

    /// <summary>Download a PDF report card for one of the caller's children.</summary>
    [HttpGet("children/{studentId:guid}/report-card")]
    public async Task<IActionResult> GetChildReportCard(Guid studentId, [FromQuery] Guid termId, [FromQuery] string? scheme)
    {
        var result = await Mediator.Send(new GetMyChildReportCardQuery
        {
            StudentId = studentId,
            AcademicTermId = termId,
            GradingScheme = scheme
        });

        if (!result.IsSuccess)
            return NotFound(result.Error);

        return File(result.Data!.Content, "application/pdf", result.Data.FileName);
    }

    /// <summary>Start an online (Paynow) payment against one of the caller's children's invoices.</summary>
    [HttpPost("payments/online/initiate")]
    public async Task<IActionResult> InitiateChildPayment([FromBody] InitiateMyChildOnlinePaymentCommand command)
    {
        var result = await Mediator.Send(command);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
    }

    /// <summary>Poll the status of a payment one of the caller's children made, settling it if paid.</summary>
    [HttpPost("payments/{paymentId:guid}/status")]
    public async Task<IActionResult> CheckChildPaymentStatus(Guid paymentId)
    {
        var result = await Mediator.Send(new CheckMyChildPaymentStatusCommand(paymentId));
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
    }
}
