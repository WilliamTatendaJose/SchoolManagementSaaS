using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Staff.Commands;

public class UpdateStaffCommandHandler : IRequestHandler<UpdateStaffCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public UpdateStaffCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(UpdateStaffCommand request, CancellationToken cancellationToken)
    {
        var staff = await _context.Staff.FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        if (staff == null)
        {
            return Result.Failure("Staff not found");
        }

        staff.Department = request.Department;
        staff.JobTitle = request.JobTitle;
        staff.DateOfJoining = request.DateOfJoining;
        staff.Qualifications = request.Qualifications;
        staff.Specialization = request.Specialization;
        staff.IsTeacher = request.IsTeacher;
        staff.IsActive = request.IsActive;

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
