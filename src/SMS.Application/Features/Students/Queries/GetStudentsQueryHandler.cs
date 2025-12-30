using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Students.Queries;

public class GetStudentsQueryHandler : IRequestHandler<GetStudentsQuery, Result<PaginatedList<StudentDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetStudentsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PaginatedList<StudentDto>>> Handle(GetStudentsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Students
            .AsNoTracking()
            .Include(s => s.CurrentClass)
            .Include(s => s.House)
            .Include(s => s.Guardians)
                .ThenInclude(sg => sg.Guardian)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLower();
            query = query.Where(s =>
                s.FirstName.ToLower().Contains(searchTerm) ||
                s.LastName.ToLower().Contains(searchTerm) ||
                s.StudentNumber.ToLower().Contains(searchTerm));
        }

        if (request.ClassId.HasValue)
        {
            query = query.Where(s => s.CurrentClassId == request.ClassId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (Enum.TryParse<Domain.Enums.StudentStatus>(request.Status, out var status))
            {
                query = query.Where(s => s.Status == status);
            }
        }

        // Apply sorting
        query = request.SortBy?.ToLower() switch
        {
            "firstname" => request.SortDescending ? query.OrderByDescending(s => s.FirstName) : query.OrderBy(s => s.FirstName),
            "lastname" => request.SortDescending ? query.OrderByDescending(s => s.LastName) : query.OrderBy(s => s.LastName),
            "admissiondate" => request.SortDescending ? query.OrderByDescending(s => s.AdmissionDate) : query.OrderBy(s => s.AdmissionDate),
            "studentnumber" => request.SortDescending ? query.OrderByDescending(s => s.StudentNumber) : query.OrderBy(s => s.StudentNumber),
            _ => query.OrderBy(s => s.LastName).ThenBy(s => s.FirstName)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var students = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(s => new StudentDto
            {
                Id = s.Id,
                StudentNumber = s.StudentNumber,
                FirstName = s.FirstName,
                MiddleName = s.MiddleName,
                LastName = s.LastName,
                FullName = s.FullName,
                DateOfBirth = s.DateOfBirth,
                Gender = s.Gender.ToString(),
                Status = s.Status.ToString(),
                Photo = s.Photo,
                ClassName = s.CurrentClass != null ? s.CurrentClass.Name : null,
                HouseName = s.House != null ? s.House.Name : null,
                AdmissionDate = s.AdmissionDate,
                PrimaryGuardianName = s.Guardians
                    .Where(g => g.IsPrimaryContact)
                    .Select(g => g.Guardian.FirstName + " " + g.Guardian.LastName)
                    .FirstOrDefault(),
                PrimaryGuardianPhone = s.Guardians
                    .Where(g => g.IsPrimaryContact)
                    .Select(g => g.Guardian.Phone)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        var paginatedList = new PaginatedList<StudentDto>(students, totalCount, request.PageNumber, request.PageSize);

        return Result<PaginatedList<StudentDto>>.Success(paginatedList);
    }
}
