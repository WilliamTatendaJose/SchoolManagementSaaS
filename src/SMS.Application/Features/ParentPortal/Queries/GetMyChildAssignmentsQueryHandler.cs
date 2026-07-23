using MediatR;
using SMS.Application.Common.Models;
using SMS.Application.Features.Lms.Queries;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.ParentPortal.Queries;

public class GetMyChildAssignmentsQueryHandler : IRequestHandler<GetMyChildAssignmentsQuery, Result<List<StudentAssignmentDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ISender _sender;

    public GetMyChildAssignmentsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser, ISender sender)
    {
        _context = context;
        _currentUser = currentUser;
        _sender = sender;
    }

    public async Task<Result<List<StudentAssignmentDto>>> Handle(GetMyChildAssignmentsQuery request, CancellationToken cancellationToken)
    {
        if (!await ParentChildAccess.OwnsStudentAsync(_context, _currentUser.UserId, request.StudentId, cancellationToken))
        {
            return Result<List<StudentAssignmentDto>>.Failure("Student not found");
        }

        return await _sender.Send(new GetStudentAssignmentsQuery(request.StudentId), cancellationToken);
    }
}
