using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Branding;
using SMS.Application.Common.Grading;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Academic.Queries;

public class GenerateReportCardQueryHandler : IRequestHandler<GenerateReportCardQuery, Result<ReportCardFileDto>>
{
    private readonly ISender _sender;
    private readonly IApplicationDbContext _context;
    private readonly IReportCardGenerator _generator;
    private readonly ICurrentUserService _currentUser;

    public GenerateReportCardQueryHandler(
        ISender sender,
        IApplicationDbContext context,
        IReportCardGenerator generator,
        ICurrentUserService currentUser)
    {
        _sender = sender;
        _context = context;
        _generator = generator;
        _currentUser = currentUser;
    }

    public async Task<Result<ReportCardFileDto>> Handle(GenerateReportCardQuery request, CancellationToken cancellationToken)
    {
        var resultsOutcome = await _sender.Send(new GetStudentResultsQuery
        {
            StudentId = request.StudentId,
            AcademicTermId = request.AcademicTermId
        }, cancellationToken);

        if (!resultsOutcome.IsSuccess)
        {
            return Result<ReportCardFileDto>.Failure(resultsOutcome.Error!);
        }

        var results = resultsOutcome.Data!;
        var scale = GradeScales.Resolve(request.GradingScheme);

        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.Id == _currentUser.TenantId, cancellationToken);

        var classTeacherComment = request.ClassTeacherComment;
        var headComment = request.HeadComment;

        if (classTeacherComment == null || headComment == null)
        {
            var saved = await _context.ReportCardComments
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.StudentId == request.StudentId && c.AcademicTermId == request.AcademicTermId, cancellationToken);

            classTeacherComment ??= saved?.ClassTeacherComment;
            headComment ??= saved?.HeadComment;
        }

        var model = new ReportCardModel
        {
            Branding = BrandingBuilder.From(tenant),
            StudentName = results.StudentName,
            StudentNumber = results.StudentNumber,
            ClassName = results.ClassName,
            TermName = results.TermName ?? string.Empty,
            GradingScheme = scale.Name,
            OverallAverage = results.OverallAverage,
            OverallGrade = scale.GetGrade(results.OverallAverage),
            ClassRank = results.ClassRank,
            TotalInClass = results.TotalInClass,
            ClassTeacherComment = classTeacherComment,
            HeadComment = headComment,
            Subjects = results.SubjectResults
                .Select(s => new ReportCardSubjectLine
                {
                    SubjectName = s.SubjectName,
                    Percentage = s.AveragePercentage,
                    Grade = scale.GetGrade(s.AveragePercentage)
                })
                .ToList()
        };

        var pdf = _generator.Generate(model);
        var fileName = BuildFileName(results.StudentNumber, model.TermName);

        return Result<ReportCardFileDto>.Success(new ReportCardFileDto
        {
            FileName = fileName,
            Content = pdf
        });
    }

    private static string BuildFileName(string studentNumber, string termName)
    {
        var raw = $"ReportCard_{studentNumber}_{termName}".Trim();
        var safe = new string(raw.Select(c => char.IsLetterOrDigit(c) || c is '-' or '_' ? c : '_').ToArray());
        return $"{safe}.pdf";
    }
}
