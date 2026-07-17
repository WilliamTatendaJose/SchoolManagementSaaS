using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;
using StaffEntity = SMS.Domain.Entities.Staff;

namespace SMS.Application.Features.Staff.Commands;

public class CreateStaffCommandHandler : IRequestHandler<CreateStaffCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public CreateStaffCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreateStaffCommand request, CancellationToken cancellationToken)
    {
        if (!await _context.Users.AnyAsync(u => u.Id == request.UserId, cancellationToken))
        {
            return Result<Guid>.Failure("User not found");
        }

        if (await _context.Staff.AnyAsync(s => s.UserId == request.UserId, cancellationToken))
        {
            return Result<Guid>.Failure("This user already has a staff profile");
        }

        var staff = new StaffEntity
        {
            UserId = request.UserId,
            StaffNumber = await GenerateStaffNumberAsync(cancellationToken),
            Department = request.Department,
            JobTitle = request.JobTitle,
            DateOfJoining = request.DateOfJoining,
            Qualifications = request.Qualifications,
            Specialization = request.Specialization,
            IsTeacher = request.IsTeacher,
            IsActive = true
        };

        _context.Staff.Add(staff);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(staff.Id);
    }

    private async Task<string> GenerateStaffNumberAsync(CancellationToken cancellationToken)
    {
        var year = DateTime.UtcNow.Year;
        var count = await _context.Staff.CountAsync(s => s.StaffNumber.StartsWith($"STAFF-{year}-"), cancellationToken) + 1;
        return $"STAFF-{year}-{count:D5}";
    }
}
