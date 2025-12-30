using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;
using SMS.Domain.Enums;

namespace SMS.Application.Features.Students.Commands;

public class UpdateStudentCommandHandler : IRequestHandler<UpdateStudentCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public UpdateStudentCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(UpdateStudentCommand request, CancellationToken cancellationToken)
    {
        var student = await _context.Students
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        if (student == null)
        {
            return Result.Failure("Student not found");
        }

        student.FirstName = request.FirstName;
        student.MiddleName = request.MiddleName;
        student.LastName = request.LastName;
        student.DateOfBirth = request.DateOfBirth;
        student.Gender = Enum.Parse<Gender>(request.Gender);
        student.NationalId = request.NationalId;
        student.BirthCertificateNumber = request.BirthCertificateNumber;
        student.Address = request.Address;
        student.City = request.City;
        student.Nationality = request.Nationality;
        student.Religion = request.Religion;
        student.MedicalNotes = request.MedicalNotes;
        student.SpecialNeeds = request.SpecialNeeds;
        student.CurrentClassId = request.ClassId;
        student.HouseId = request.HouseId;
        student.DormitoryId = request.DormitoryId;

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
