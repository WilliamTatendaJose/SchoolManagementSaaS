using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Enrollments.Queries;

public class GetEnrollmentsQueryHandler : IRequestHandler<GetEnrollmentsQuery, Result<PaginatedList<EnrollmentDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetEnrollmentsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PaginatedList<EnrollmentDto>>> Handle(GetEnrollmentsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Enrollments
            .AsNoTracking()
            .Include(e => e.Student)
            .Include(e => e.Class)
            .Include(e => e.Stream)
            .Include(e => e.AcademicYear)
            .AsQueryable();

        if (request.AcademicYearId.HasValue)
        {
            query = query.Where(e => e.AcademicYearId == request.AcademicYearId.Value);
        }

        if (request.ClassId.HasValue)
        {
            query = query.Where(e => e.ClassId == request.ClassId.Value);
        }

        if (request.StreamId.HasValue)
        {
            query = query.Where(e => e.StreamId == request.StreamId.Value);
        }

        if (request.StudentId.HasValue)
        {
            query = query.Where(e => e.StudentId == request.StudentId.Value);
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(e => e.IsActive == request.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLower();
            query = query.Where(e =>
                e.Student.FirstName.ToLower().Contains(searchTerm) ||
                e.Student.LastName.ToLower().Contains(searchTerm) ||
                e.Student.StudentNumber.ToLower().Contains(searchTerm));
        }

        query = query
            .OrderByDescending(e => e.IsActive)
            .ThenByDescending(e => e.EnrollmentDate);

        var totalCount = await query.CountAsync(cancellationToken);

        var enrollments = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(e => new EnrollmentDto
            {
                Id = e.Id,
                StudentId = e.StudentId,
                StudentNumber = e.Student.StudentNumber,
                StudentName = e.Student.FullName,
                ClassId = e.ClassId,
                ClassName = e.Class.Name,
                StreamId = e.StreamId,
                StreamName = e.Stream != null ? e.Stream.Name : null,
                AcademicYearId = e.AcademicYearId,
                AcademicYearName = e.AcademicYear.Name,
                EnrollmentDate = e.EnrollmentDate,
                IsActive = e.IsActive
            })
            .ToListAsync(cancellationToken);

        var paginatedList = new PaginatedList<EnrollmentDto>(enrollments, totalCount, request.PageNumber, request.PageSize);

        return Result<PaginatedList<EnrollmentDto>>.Success(paginatedList);
    }
}
