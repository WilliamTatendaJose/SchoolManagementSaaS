using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;
using SMS.Domain.Entities;
using SMS.Domain.Enums;

namespace SMS.Application.Features.Students.Commands;

public class CreateStudentCommandHandler : IRequestHandler<CreateStudentCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public CreateStudentCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreateStudentCommand request, CancellationToken cancellationToken)
    {
        // Generate student number
        var studentNumber = await GenerateStudentNumberAsync(cancellationToken);

        var student = new Student
        {
            StudentNumber = studentNumber,
            FirstName = request.FirstName,
            MiddleName = request.MiddleName,
            LastName = request.LastName,
            DateOfBirth = request.DateOfBirth,
            Gender = Enum.Parse<Gender>(request.Gender),
            NationalId = request.NationalId,
            BirthCertificateNumber = request.BirthCertificateNumber,
            Address = request.Address,
            City = request.City,
            Nationality = request.Nationality ?? "Zimbabwean",
            Religion = request.Religion,
            MedicalNotes = request.MedicalNotes,
            SpecialNeeds = request.SpecialNeeds,
            PreviousSchool = request.PreviousSchool,
            CurrentClassId = request.ClassId,
            HouseId = request.HouseId,
            AdmissionDate = request.AdmissionDate,
            Status = StudentStatus.Active
        };

        _context.Students.Add(student);

        // Add guardians if provided
        if (request.Guardians?.Any() == true)
        {
            foreach (var guardianInfo in request.Guardians)
            {
                Guardian guardian;

                if (guardianInfo.GuardianId.HasValue)
                {
                    guardian = await _context.Guardians.FindAsync([guardianInfo.GuardianId.Value], cancellationToken)
                        ?? throw new KeyNotFoundException($"Guardian with ID {guardianInfo.GuardianId} not found");
                }
                else
                {
                    guardian = new Guardian
                    {
                        FirstName = guardianInfo.FirstName ?? string.Empty,
                        LastName = guardianInfo.LastName ?? string.Empty,
                        Phone = guardianInfo.Phone,
                        Email = guardianInfo.Email
                    };
                    _context.Guardians.Add(guardian);
                }

                var studentGuardian = new StudentGuardian
                {
                    Student = student,
                    Guardian = guardian,
                    Relationship = guardianInfo.Relationship,
                    IsPrimaryContact = guardianInfo.IsPrimaryContact,
                    IsEmergencyContact = guardianInfo.IsEmergencyContact
                };

                _context.StudentGuardians.Add(studentGuardian);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(student.Id);
    }

    private async Task<string> GenerateStudentNumberAsync(CancellationToken cancellationToken)
    {
        var year = DateTime.UtcNow.Year;
        var count = await _context.Students
            .CountAsync(s => s.AdmissionDate.Year == year, cancellationToken) + 1;
        
        return $"STU-{year}-{count:D5}";
    }
}
