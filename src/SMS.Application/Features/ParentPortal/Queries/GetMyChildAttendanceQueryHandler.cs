using MediatR;
using SMS.Application.Common.Models;
using SMS.Application.Features.Attendance.Queries;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.ParentPortal.Queries;

public class GetMyChildAttendanceQueryHandler : IRequestHandler<GetMyChildAttendanceQuery, Result<AttendanceSummaryDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ISender _sender;

    public GetMyChildAttendanceQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser, ISender sender)
    {
        _context = context;
        _currentUser = currentUser;
        _sender = sender;
    }

    public async Task<Result<AttendanceSummaryDto>> Handle(GetMyChildAttendanceQuery request, CancellationToken cancellationToken)
    {
        if (!await ParentChildAccess.OwnsStudentAsync(_context, _currentUser.UserId, request.StudentId, cancellationToken))
        {
            return Result<AttendanceSummaryDto>.Failure("Student not found");
        }

        return await _sender.Send(new GetStudentAttendanceSummaryQuery
        {
            StudentId = request.StudentId,
            AcademicTermId = request.AcademicTermId
        }, cancellationToken);
    }
}
