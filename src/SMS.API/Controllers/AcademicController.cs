using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application.Features.Academic.Commands;
using SMS.Application.Features.Academic.Queries;

namespace SMS.API.Controllers;

[Authorize]
public class AcademicController : BaseApiController
{
    #region Classes

    [HttpGet("classes")]
    public async Task<IActionResult> GetClasses([FromQuery] bool includeStudentCount = false)
    {
        var result = await Mediator.Send(new GetClassesQuery { IncludeStudentCount = includeStudentCount });
        
        if (!result.IsSuccess)
            return BadRequest(result.Error);
            
        return Ok(result.Data);
    }

    [HttpPost("classes")]
    public async Task<IActionResult> CreateClass([FromBody] CreateClassCommand command)
    {
        var result = await Mediator.Send(command);
        
        if (!result.IsSuccess)
            return BadRequest(result.Error);
            
        return Ok(new { Id = result.Data });
    }

    #endregion

    #region Subjects

    [HttpGet("subjects")]
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
    public async Task<IActionResult> CreateSubject([FromBody] CreateSubjectCommand command)
    {
        var result = await Mediator.Send(command);
        
        if (!result.IsSuccess)
            return BadRequest(result.Error);
            
        return Ok(new { Id = result.Data });
    }

    #endregion

    #region Academic Years

    [HttpPost("years")]
    public async Task<IActionResult> CreateAcademicYear([FromBody] CreateAcademicYearCommand command)
    {
        var result = await Mediator.Send(command);
        
        if (!result.IsSuccess)
            return BadRequest(result.Error);
            
        return Ok(new { Id = result.Data });
    }

    #endregion

    #region Assessments

    [HttpGet("assessments")]
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
    public async Task<IActionResult> CreateAssessment([FromBody] CreateAssessmentCommand command)
    {
        var result = await Mediator.Send(command);
        
        if (!result.IsSuccess)
            return BadRequest(result.Error);
            
        return Ok(new { Id = result.Data });
    }

    [HttpGet("assessments/{id:guid}/results")]
    public async Task<IActionResult> GetAssessmentResults(Guid id)
    {
        var result = await Mediator.Send(new GetAssessmentResultsQuery { AssessmentId = id });
        
        if (!result.IsSuccess)
            return NotFound(result.Error);
            
        return Ok(result.Data);
    }

    [HttpPost("assessments/{id:guid}/results")]
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

    #endregion
}
