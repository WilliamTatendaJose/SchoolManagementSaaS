using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;
using SMS.Domain.Enums;

namespace SMS.Application.Features.Students.Queries;

public class GetStudentByIdQueryHandler : IRequestHandler<GetStudentByIdQuery, Result<StudentDetailDto>>
{
    private readonly IApplicationDbContext _context;

    public GetStudentByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<StudentDetailDto>> Handle(GetStudentByIdQuery request, CancellationToken cancellationToken)
    {
        var student = await _context.Students
            .AsNoTracking()
            .Include(s => s.CurrentClass)
            .Include(s => s.House)
            .Include(s => s.Dormitory)
            .Include(s => s.Guardians)
                .ThenInclude(sg => sg.Guardian)
            .Include(s => s.Invoices)
            .Include(s => s.Attendances)
            .Include(s => s.DisciplineRecords)
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        if (student == null)
        {
            return Result<StudentDetailDto>.Failure("Student not found");
        }

        // Calculate attendance percentage for current term
        var attendanceRecords = student.Attendances
            .Where(a => a.Date >= DateTime.UtcNow.AddMonths(-3))
            .ToList();

        var attendancePercentage = attendanceRecords.Count > 0
            ? (int)Math.Round((double)attendanceRecords.Count(a => a.Status == AttendanceStatus.Present || a.Status == AttendanceStatus.Late) / attendanceRecords.Count * 100)
            : 0;

        // Calculate outstanding balance
        var outstandingBalance = student.Invoices
            .Sum(i => i.TotalAmount - i.DiscountAmount - i.PaidAmount);

        // Calculate merits and demerits
        var totalDemerits = student.DisciplineRecords.Sum(d => d.DemeritsAwarded ?? 0);
        var totalMerits = student.DisciplineRecords.Sum(d => d.MeritsAwarded ?? 0);

        var dto = new StudentDetailDto
        {
            Id = student.Id,
            StudentNumber = student.StudentNumber,
            FirstName = student.FirstName,
            MiddleName = student.MiddleName,
            LastName = student.LastName,
            FullName = student.FullName,
            DateOfBirth = student.DateOfBirth,
            Gender = student.Gender.ToString(),
            NationalId = student.NationalId,
            BirthCertificateNumber = student.BirthCertificateNumber,
            Photo = student.Photo,
            Address = student.Address,
            City = student.City,
            Nationality = student.Nationality,
            Religion = student.Religion,
            Status = student.Status.ToString(),
            AdmissionDate = student.AdmissionDate,
            GraduationDate = student.GraduationDate,
            MedicalNotes = student.MedicalNotes,
            SpecialNeeds = student.SpecialNeeds,
            PreviousSchool = student.PreviousSchool,
            CurrentClassId = student.CurrentClassId,
            CurrentClassName = student.CurrentClass?.Name,
            HouseId = student.HouseId,
            HouseName = student.House?.Name,
            DormitoryId = student.DormitoryId,
            DormitoryName = student.Dormitory?.Name,
            Guardians = student.Guardians.Select(sg => new GuardianDto
            {
                Id = sg.Guardian.Id,
                FirstName = sg.Guardian.FirstName,
                LastName = sg.Guardian.LastName,
                FullName = sg.Guardian.FullName,
                Phone = sg.Guardian.Phone,
                Email = sg.Guardian.Email,
                Relationship = sg.Relationship,
                IsPrimaryContact = sg.IsPrimaryContact,
                IsEmergencyContact = sg.IsEmergencyContact
            }).ToList(),
            OutstandingBalance = outstandingBalance,
            AttendancePercentage = attendancePercentage,
            TotalDemerits = totalDemerits,
            TotalMerits = totalMerits
        };

        return Result<StudentDetailDto>.Success(dto);
    }
}
