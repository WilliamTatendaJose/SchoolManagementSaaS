using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;
using SMS.Domain.Entities;
using SMS.Domain.Enums;

namespace SMS.Application.Features.Guardians.Commands;

public class CreateGuardianCommandHandler : IRequestHandler<CreateGuardianCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public CreateGuardianCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreateGuardianCommand request, CancellationToken cancellationToken)
    {
        var guardian = new Guardian
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Gender = Enum.Parse<Gender>(request.Gender),
            NationalId = request.NationalId,
            Phone = request.Phone,
            AlternatePhone = request.AlternatePhone,
            Email = request.Email,
            Address = request.Address,
            Occupation = request.Occupation,
            Employer = request.Employer
        };

        _context.Guardians.Add(guardian);

        if (request.LinkToStudent is not null)
        {
            var studentExists = await _context.Students
                .AnyAsync(s => s.Id == request.LinkToStudent.StudentId, cancellationToken);

            if (!studentExists)
            {
                return Result<Guid>.Failure("Student to link not found");
            }

            _context.StudentGuardians.Add(new StudentGuardian
            {
                Guardian = guardian,
                StudentId = request.LinkToStudent.StudentId,
                Relationship = request.LinkToStudent.Relationship,
                IsPrimaryContact = request.LinkToStudent.IsPrimaryContact,
                IsEmergencyContact = request.LinkToStudent.IsEmergencyContact,
                CanPickup = request.LinkToStudent.CanPickup
            });
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(guardian.Id);
    }
}
