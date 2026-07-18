using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;
using StaffEntity = SMS.Domain.Entities.Staff;

namespace SMS.Application.Features.Staff.Commands;

public class CreateStaffCommandHandler : IRequestHandler<CreateStaffCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CreateStaffCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
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
        var tenantId = _currentUserService.TenantId;

        // IgnoreQueryFilters: a soft-deleted staff member's number must still count as "used"
        // so a later create in the same year can't regenerate and collide with it.
        var count = await _context.Staff
            .IgnoreQueryFilters()
            .CountAsync(s => s.TenantId == tenantId && s.StaffNumber.StartsWith($"STAFF-{year}-"), cancellationToken) + 1;
        return $"STAFF-{year}-{count:D5}";
    }
}
