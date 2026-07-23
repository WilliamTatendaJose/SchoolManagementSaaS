using MediatR;
using SMS.Application.Common.Models;
using SMS.Application.Features.Students.Queries;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.ParentPortal.Queries;

public class GetMyChildProfileQueryHandler : IRequestHandler<GetMyChildProfileQuery, Result<StudentDetailDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ISender _sender;

    public GetMyChildProfileQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser, ISender sender)
    {
        _context = context;
        _currentUser = currentUser;
        _sender = sender;
    }

    public async Task<Result<StudentDetailDto>> Handle(GetMyChildProfileQuery request, CancellationToken cancellationToken)
    {
        if (!await ParentChildAccess.OwnsStudentAsync(_context, _currentUser.UserId, request.StudentId, cancellationToken))
        {
            return Result<StudentDetailDto>.Failure("Student not found");
        }

        return await _sender.Send(new GetStudentByIdQuery(request.StudentId), cancellationToken);
    }
}
