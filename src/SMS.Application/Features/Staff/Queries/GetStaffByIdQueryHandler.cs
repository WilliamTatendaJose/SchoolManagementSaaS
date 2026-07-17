using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Common.Staffing;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Staff.Queries;

public class GetStaffByIdQueryHandler : IRequestHandler<GetStaffByIdQuery, Result<StaffDetailDto>>
{
    private readonly IApplicationDbContext _context;

    public GetStaffByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<StaffDetailDto>> Handle(GetStaffByIdQuery request, CancellationToken cancellationToken)
    {
        var staff = await _context.Staff
            .AsNoTracking()
            .Include(s => s.User)
            .Include(s => s.Subjects)
                .ThenInclude(ts => ts.Subject)
            .Include(s => s.LeaveRequests)
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        if (staff == null)
        {
            return Result<StaffDetailDto>.Failure("Staff not found");
        }

        var dto = new StaffDetailDto
        {
            Id = staff.Id,
            UserId = staff.UserId,
            StaffNumber = staff.StaffNumber,
            FullName = staff.User.FirstName + " " + staff.User.LastName,
            Email = staff.User.Email,
            Phone = staff.User.Phone,
            Department = staff.Department,
            JobTitle = staff.JobTitle,
            DateOfJoining = staff.DateOfJoining,
            Qualifications = staff.Qualifications,
            Specialization = staff.Specialization,
            IsTeacher = staff.IsTeacher,
            IsActive = staff.IsActive,
            Subjects = staff.Subjects.Select(ts => new TeacherSubjectDto
            {
                SubjectId = ts.SubjectId,
                SubjectName = ts.Subject.Name,
                IsPrimary = ts.IsPrimary
            }).ToList(),
            PendingLeaveRequests = staff.LeaveRequests.Count(l => l.Status == LeaveStatuses.Pending)
        };

        return Result<StaffDetailDto>.Success(dto);
    }
}
