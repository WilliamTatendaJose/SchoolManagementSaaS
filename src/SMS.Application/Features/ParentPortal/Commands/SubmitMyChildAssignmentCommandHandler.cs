using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Features.Lms.Commands;
using SMS.Application.Features.ParentPortal.Queries;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.ParentPortal.Commands;

public class SubmitMyChildAssignmentCommandHandler : IRequestHandler<SubmitMyChildAssignmentCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ISender _sender;

    public SubmitMyChildAssignmentCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser, ISender sender)
    {
        _context = context;
        _currentUser = currentUser;
        _sender = sender;
    }

    public async Task<Result<Guid>> Handle(SubmitMyChildAssignmentCommand request, CancellationToken cancellationToken)
    {
        if (!await ParentChildAccess.OwnsStudentAsync(_context, _currentUser.UserId, request.StudentId, cancellationToken))
        {
            return Result<Guid>.Failure("Student not found");
        }

        // The assignment must be one set for the child's own class - otherwise a guardian
        // could submit their child against any assignment id in the tenant.
        var student = await _context.Students
            .Where(s => s.Id == request.StudentId)
            .Select(s => new { s.CurrentClassId })
            .FirstOrDefaultAsync(cancellationToken);

        var assignmentClassId = await _context.Assignments
            .Where(a => a.Id == request.AssignmentId)
            .Select(a => (Guid?)a.ClassId)
            .FirstOrDefaultAsync(cancellationToken);

        if (assignmentClassId is null || student?.CurrentClassId != assignmentClassId)
        {
            return Result<Guid>.Failure("This assignment is not for your child's class");
        }

        return await _sender.Send(new RecordAssignmentSubmissionCommand
        {
            AssignmentId = request.AssignmentId,
            StudentId = request.StudentId,
            Comment = request.Comment,
            AttachmentFileName = request.AttachmentFileName,
            AttachmentContentType = request.AttachmentContentType,
            AttachmentContent = request.AttachmentContent
        }, cancellationToken);
    }
}
