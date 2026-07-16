using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Guardians.Queries;

public class GetGuardianByIdQueryHandler : IRequestHandler<GetGuardianByIdQuery, Result<GuardianDetailDto>>
{
    private readonly IApplicationDbContext _context;

    public GetGuardianByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<GuardianDetailDto>> Handle(GetGuardianByIdQuery request, CancellationToken cancellationToken)
    {
        var guardian = await _context.Guardians
            .AsNoTracking()
            .Include(g => g.Students)
                .ThenInclude(sg => sg.Student)
                    .ThenInclude(s => s.CurrentClass)
            .FirstOrDefaultAsync(g => g.Id == request.Id, cancellationToken);

        if (guardian == null)
        {
            return Result<GuardianDetailDto>.Failure("Guardian not found");
        }

        var dto = new GuardianDetailDto
        {
            Id = guardian.Id,
            UserId = guardian.UserId,
            FirstName = guardian.FirstName,
            LastName = guardian.LastName,
            FullName = guardian.FullName,
            Gender = guardian.Gender.ToString(),
            NationalId = guardian.NationalId,
            Phone = guardian.Phone,
            AlternatePhone = guardian.AlternatePhone,
            Email = guardian.Email,
            Address = guardian.Address,
            Occupation = guardian.Occupation,
            Employer = guardian.Employer,
            Students = guardian.Students.Select(sg => new LinkedStudentDto
            {
                StudentId = sg.StudentId,
                StudentNumber = sg.Student.StudentNumber,
                FullName = sg.Student.FullName,
                ClassName = sg.Student.CurrentClass != null ? sg.Student.CurrentClass.Name : null,
                Relationship = sg.Relationship,
                IsPrimaryContact = sg.IsPrimaryContact,
                IsEmergencyContact = sg.IsEmergencyContact,
                CanPickup = sg.CanPickup
            }).ToList()
        };

        return Result<GuardianDetailDto>.Success(dto);
    }
}
