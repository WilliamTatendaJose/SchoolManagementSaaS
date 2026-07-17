using MediatR;
using SMS.Application.Common.Models;
using SMS.Application.Features.Academic.Queries;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.ParentPortal.Queries;

public class GetMyChildResultsQueryHandler : IRequestHandler<GetMyChildResultsQuery, Result<StudentAcademicResultsDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ISender _sender;

    public GetMyChildResultsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser, ISender sender)
    {
        _context = context;
        _currentUser = currentUser;
        _sender = sender;
    }

    public async Task<Result<StudentAcademicResultsDto>> Handle(GetMyChildResultsQuery request, CancellationToken cancellationToken)
    {
        if (!await ParentChildAccess.OwnsStudentAsync(_context, _currentUser.UserId, request.StudentId, cancellationToken))
        {
            return Result<StudentAcademicResultsDto>.Failure("Student not found");
        }

        return await _sender.Send(new GetStudentResultsQuery
        {
            StudentId = request.StudentId,
            AcademicTermId = request.AcademicTermId
        }, cancellationToken);
    }
}
