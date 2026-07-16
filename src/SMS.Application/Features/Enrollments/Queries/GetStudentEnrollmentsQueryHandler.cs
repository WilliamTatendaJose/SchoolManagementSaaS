using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Enrollments.Queries;

public class GetStudentEnrollmentsQueryHandler : IRequestHandler<GetStudentEnrollmentsQuery, Result<List<EnrollmentDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetStudentEnrollmentsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<EnrollmentDto>>> Handle(GetStudentEnrollmentsQuery request, CancellationToken cancellationToken)
    {
        var studentExists = await _context.Students
            .AnyAsync(s => s.Id == request.StudentId, cancellationToken);

        if (!studentExists)
        {
            return Result<List<EnrollmentDto>>.Failure("Student not found");
        }

        var enrollments = await _context.Enrollments
            .AsNoTracking()
            .Include(e => e.Student)
            .Include(e => e.Class)
            .Include(e => e.Stream)
            .Include(e => e.AcademicYear)
            .Where(e => e.StudentId == request.StudentId)
            .OrderByDescending(e => e.EnrollmentDate)
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

        return Result<List<EnrollmentDto>>.Success(enrollments);
    }
}
