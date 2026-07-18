using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.Common.Security;
using SMS.Application.Features.Academic.Commands;
using SMS.Application.Features.Academic.Queries;
using SMS.Infrastructure.Authorization;

namespace SMS.API.Controllers;

[Authorize]
public class AcademicController : BaseApiController
{
    #region Classes

    [HttpGet("classes")]
    [RequirePermission(Permissions.ClassesView)]
    public async Task<IActionResult> GetClasses([FromQuery] bool includeStudentCount = false)
    {
        var result = await Mediator.Send(new GetClassesQuery { IncludeStudentCount = includeStudentCount });
        
        if (!result.IsSuccess)
            return BadRequest(result.Error);
            
        return Ok(result.Data);
    }

    [HttpPost("classes")]
    [RequirePermission(Permissions.ClassesManage)]
    public async Task<IActionResult> CreateClass([FromBody] CreateClassCommand command)
    {
        var result = await Mediator.Send(command);

        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return Ok(new { Id = result.Data });
    }

    [HttpPut("classes/{id:guid}")]
    [RequirePermission(Permissions.ClassesManage)]
    public async Task<IActionResult> UpdateClass(Guid id, [FromBody] UpdateClassCommand command)
    {
        if (id != command.Id)
            return BadRequest("ID mismatch");

        var result = await Mediator.Send(command);

        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return NoContent();
    }

    [HttpDelete("classes/{id:guid}")]
    [RequirePermission(Permissions.ClassesManage)]
    public async Task<IActionResult> DeleteClass(Guid id)
    {
        var result = await Mediator.Send(new DeleteClassCommand(id));

        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return NoContent();
    }

    #endregion

    #region Subjects

    [HttpGet("subjects")]
    [RequirePermission(Permissions.SubjectsView)]
    public async Task<IActionResult> GetSubjects([FromQuery] bool? activeOnly, [FromQuery] bool? coreOnly)
    {
        var result = await Mediator.Send(new GetSubjectsQuery 
        { 
            ActiveOnly = activeOnly,
            CoreOnly = coreOnly 
        });
        
        if (!result.IsSuccess)
            return BadRequest(result.Error);
            
        return Ok(result.Data);
    }

    [HttpPost("subjects")]
    [RequirePermission(Permissions.SubjectsManage)]
    public async Task<IActionResult> CreateSubject([FromBody] CreateSubjectCommand command)
    {
        var result = await Mediator.Send(command);

        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return Ok(new { Id = result.Data });
    }

    [HttpPut("subjects/{id:guid}")]
    [RequirePermission(Permissions.SubjectsManage)]
    public async Task<IActionResult> UpdateSubject(Guid id, [FromBody] UpdateSubjectCommand command)
    {
        if (id != command.Id)
            return BadRequest("ID mismatch");

        var result = await Mediator.Send(command);

        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return NoContent();
    }

    [HttpDelete("subjects/{id:guid}")]
    [RequirePermission(Permissions.SubjectsManage)]
    public async Task<IActionResult> DeleteSubject(Guid id)
    {
        var result = await Mediator.Send(new DeleteSubjectCommand(id));

        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return NoContent();
    }

    #endregion

    #region Academic Years

    [HttpGet("years")]
    [RequirePermission(Permissions.ClassesView)]
    public async Task<IActionResult> GetAcademicYears()
    {
        var result = await Mediator.Send(new GetAcademicYearsQuery());

        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return Ok(result.Data);
    }

    [HttpPost("years")]
    [RequirePermission(Permissions.ClassesManage)]
    public async Task<IActionResult> CreateAcademicYear([FromBody] CreateAcademicYearCommand command)
    {
        var result = await Mediator.Send(command);

        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return Ok(new { Id = result.Data });
    }

    [HttpPut("years/{id:guid}")]
    [RequirePermission(Permissions.ClassesManage)]
    public async Task<IActionResult> UpdateAcademicYear(Guid id, [FromBody] UpdateAcademicYearCommand command)
    {
        if (id != command.Id)
            return BadRequest("ID mismatch");

        var result = await Mediator.Send(command);

        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return NoContent();
    }

    [HttpDelete("years/{id:guid}")]
    [RequirePermission(Permissions.ClassesManage)]
    public async Task<IActionResult> DeleteAcademicYear(Guid id)
    {
        var result = await Mediator.Send(new DeleteAcademicYearCommand(id));

        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return NoContent();
    }

    #endregion

    #region Academic Terms

    [HttpGet("terms")]
    [RequirePermission(Permissions.ClassesView)]
    public async Task<IActionResult> GetAcademicTerms([FromQuery] Guid? academicYearId)
    {
        var result = await Mediator.Send(new GetAcademicTermsQuery { AcademicYearId = academicYearId });

        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return Ok(result.Data);
    }

    #endregion

    #region Assessments

    [HttpGet("assessments")]
    [RequirePermission(Permissions.AssessmentsView)]
    public async Task<IActionResult> GetAssessments(
        [FromQuery] Guid? classId,
        [FromQuery] Guid? subjectId,
        [FromQuery] Guid? termId,
        [FromQuery] string? assessmentType)
    {
        var result = await Mediator.Send(new GetAssessmentsQuery
        {
            ClassId = classId,
            SubjectId = subjectId,
            AcademicTermId = termId,
            AssessmentType = assessmentType
        });
        
        if (!result.IsSuccess)
            return BadRequest(result.Error);
            
        return Ok(result.Data);
    }

    [HttpPost("assessments")]
    [RequirePermission(Permissions.AssessmentsCreate)]
    public async Task<IActionResult> CreateAssessment([FromBody] CreateAssessmentCommand command)
    {
        var result = await Mediator.Send(command);
        
        if (!result.IsSuccess)
            return BadRequest(result.Error);
            
        return Ok(new { Id = result.Data });
    }

    [HttpGet("assessments/{id:guid}/results")]
    [RequirePermission(Permissions.ResultsView)]
    public async Task<IActionResult> GetAssessmentResults(Guid id)
    {
        var result = await Mediator.Send(new GetAssessmentResultsQuery { AssessmentId = id });
        
        if (!result.IsSuccess)
            return NotFound(result.Error);
            
        return Ok(result.Data);
    }

    [HttpPost("assessments/{id:guid}/results")]
    [RequirePermission(Permissions.ResultsRecord)]
    public async Task<IActionResult> RecordResults(Guid id, [FromBody] List<StudentResultDto> results)
    {
        var result = await Mediator.Send(new RecordResultsCommand
        {
            AssessmentId = id,
            Results = results
        });
        
        if (!result.IsSuccess)
            return BadRequest(result.Error);
            
        return Ok(new { RecordedCount = result.Data });
    }

    [HttpPost("assessments/{id:guid}/publish")]
    [RequirePermission(Permissions.ResultsPublish)]
    public async Task<IActionResult> PublishResults(Guid id)
    {
        var result = await Mediator.Send(new PublishResultsCommand { AssessmentId = id });
        
        if (!result.IsSuccess)
            return BadRequest(result.Error);
            
        return Ok(new { Message = "Results published successfully" });
    }

    #endregion

    #region Student Results

    [HttpGet("students/{studentId:guid}/results")]
    [RequirePermission(Permissions.ResultsView)]
    public async Task<IActionResult> GetStudentResults(
        Guid studentId,
        [FromQuery] Guid? termId,
        [FromQuery] Guid? subjectId)
    {
        var result = await Mediator.Send(new GetStudentResultsQuery
        {
            StudentId = studentId,
            AcademicTermId = termId,
            SubjectId = subjectId
        });
        
        if (!result.IsSuccess)
            return NotFound(result.Error);

        return Ok(result.Data);
    }

    /// <summary>
    /// Download a PDF report card for a student and term.
    /// </summary>
    [HttpGet("students/{studentId:guid}/report-card")]
    [RequirePermission(Permissions.ResultsView)]
    public async Task<IActionResult> GetReportCard(
        Guid studentId,
        [FromQuery] Guid termId,
        [FromQuery] string? scheme,
        [FromQuery] string? classTeacherComment,
        [FromQuery] string? headComment)
    {
        var result = await Mediator.Send(new GenerateReportCardQuery
        {
            StudentId = studentId,
            AcademicTermId = termId,
            GradingScheme = scheme,
            ClassTeacherComment = classTeacherComment,
            HeadComment = headComment
        });

        if (!result.IsSuccess)
            return NotFound(result.Error);

        return File(result.Data!.Content, "application/pdf", result.Data.FileName);
    }

    /// <summary>
    /// Get the persisted class-teacher/head remarks for a student's report card in a term.
    /// </summary>
    [HttpGet("students/{studentId:guid}/report-card/comment")]
    [RequirePermission(Permissions.ResultsView)]
    public async Task<IActionResult> GetReportCardComment(Guid studentId, [FromQuery] Guid termId)
    {
        var result = await Mediator.Send(new GetReportCardCommentQuery(studentId, termId));

        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return Ok(result.Data);
    }

    /// <summary>
    /// Save the class-teacher and/or head remarks for a student's report card in a term.
    /// </summary>
    [HttpPut("students/{studentId:guid}/report-card/comment")]
    [RequirePermission(Permissions.ResultsRecord)]
    public async Task<IActionResult> SaveReportCardComment(Guid studentId, [FromBody] SaveReportCardCommentCommand command)
    {
        if (studentId != command.StudentId)
            return BadRequest("ID mismatch");

        var result = await Mediator.Send(command);

        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return NoContent();
    }

    #endregion
}
