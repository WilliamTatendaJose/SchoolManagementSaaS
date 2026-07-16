using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;
using SMS.Domain.Entities;

namespace SMS.Application.Features.Guardians.Commands;

public class LinkGuardianToStudentCommandHandler : IRequestHandler<LinkGuardianToStudentCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public LinkGuardianToStudentCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(LinkGuardianToStudentCommand request, CancellationToken cancellationToken)
    {
        var guardianExists = await _context.Guardians
            .AnyAsync(g => g.Id == request.GuardianId, cancellationToken);

        if (!guardianExists)
        {
            return Result<Guid>.Failure("Guardian not found");
        }

        var studentExists = await _context.Students
            .AnyAsync(s => s.Id == request.StudentId, cancellationToken);

        if (!studentExists)
        {
            return Result<Guid>.Failure("Student not found");
        }

        var existingLink = await _context.StudentGuardians
            .FirstOrDefaultAsync(
                sg => sg.GuardianId == request.GuardianId && sg.StudentId == request.StudentId,
                cancellationToken);

        if (existingLink != null)
        {
            return Result<Guid>.Failure("This guardian is already linked to the student");
        }

        // A student can have only one primary contact; demote any existing one.
        if (request.IsPrimaryContact)
        {
            var currentPrimaries = await _context.StudentGuardians
                .Where(sg => sg.StudentId == request.StudentId && sg.IsPrimaryContact)
                .ToListAsync(cancellationToken);

            foreach (var primary in currentPrimaries)
            {
                primary.IsPrimaryContact = false;
            }
        }

        var link = new StudentGuardian
        {
            GuardianId = request.GuardianId,
            StudentId = request.StudentId,
            Relationship = request.Relationship,
            IsPrimaryContact = request.IsPrimaryContact,
            IsEmergencyContact = request.IsEmergencyContact,
            CanPickup = request.CanPickup
        };

        _context.StudentGuardians.Add(link);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(link.Id);
    }
}
