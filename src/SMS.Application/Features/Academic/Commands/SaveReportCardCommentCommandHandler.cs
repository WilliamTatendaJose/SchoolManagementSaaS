using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Interfaces;
using DomainEntities = SMS.Domain.Entities;
using Result = SMS.Application.Common.Models.Result;

namespace SMS.Application.Features.Academic.Commands;

public class SaveReportCardCommentCommandHandler : IRequestHandler<SaveReportCardCommentCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public SaveReportCardCommentCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(SaveReportCardCommentCommand request, CancellationToken cancellationToken)
    {
        var studentExists = await _context.Students.AnyAsync(s => s.Id == request.StudentId, cancellationToken);
        if (!studentExists)
        {
            return Result.Failure("Student not found");
        }

        var comment = await _context.ReportCardComments
            .FirstOrDefaultAsync(c => c.StudentId == request.StudentId && c.AcademicTermId == request.AcademicTermId, cancellationToken);

        if (comment == null)
        {
            comment = new DomainEntities.ReportCardComment
            {
                StudentId = request.StudentId,
                AcademicTermId = request.AcademicTermId
            };
            _context.ReportCardComments.Add(comment);
        }

        if (request.ClassTeacherComment != null)
        {
            comment.ClassTeacherComment = request.ClassTeacherComment;
        }

        if (request.HeadComment != null)
        {
            comment.HeadComment = request.HeadComment;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
