using MediatR;
using SMS.Application.Common.Models;
using SMS.Application.Features.Lms.Queries;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.ParentPortal.Queries;

public class GetMyChildCourseMaterialsQueryHandler : IRequestHandler<GetMyChildCourseMaterialsQuery, Result<List<StudentCourseMaterialDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ISender _sender;

    public GetMyChildCourseMaterialsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser, ISender sender)
    {
        _context = context;
        _currentUser = currentUser;
        _sender = sender;
    }

    public async Task<Result<List<StudentCourseMaterialDto>>> Handle(GetMyChildCourseMaterialsQuery request, CancellationToken cancellationToken)
    {
        if (!await ParentChildAccess.OwnsStudentAsync(_context, _currentUser.UserId, request.StudentId, cancellationToken))
        {
            return Result<List<StudentCourseMaterialDto>>.Failure("Student not found");
        }

        return await _sender.Send(new GetStudentCourseMaterialsQuery(request.StudentId), cancellationToken);
    }
}
