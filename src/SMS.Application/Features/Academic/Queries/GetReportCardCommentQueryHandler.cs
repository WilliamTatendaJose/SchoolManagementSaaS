using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Academic.Queries;

public class GetReportCardCommentQueryHandler : IRequestHandler<GetReportCardCommentQuery, Result<ReportCardCommentDto>>
{
    private readonly IApplicationDbContext _context;

    public GetReportCardCommentQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<ReportCardCommentDto>> Handle(GetReportCardCommentQuery request, CancellationToken cancellationToken)
    {
        var comment = await _context.ReportCardComments
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.StudentId == request.StudentId && c.AcademicTermId == request.AcademicTermId, cancellationToken);

        return Result<ReportCardCommentDto>.Success(new ReportCardCommentDto
        {
            ClassTeacherComment = comment?.ClassTeacherComment,
            HeadComment = comment?.HeadComment
        });
    }
}
