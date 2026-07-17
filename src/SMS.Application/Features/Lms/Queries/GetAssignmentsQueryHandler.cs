using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Lms.Queries;

public class GetAssignmentsQueryHandler : IRequestHandler<GetAssignmentsQuery, Result<List<AssignmentDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetAssignmentsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<AssignmentDto>>> Handle(GetAssignmentsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Assignments
            .AsNoTracking()
            .Include(a => a.Class)
            .Include(a => a.Subject)
            .AsQueryable();

        if (request.ClassId.HasValue)
        {
            query = query.Where(a => a.ClassId == request.ClassId.Value);
        }

        if (request.SubjectId.HasValue)
        {
            query = query.Where(a => a.SubjectId == request.SubjectId.Value);
        }

        if (request.AcademicTermId.HasValue)
        {
            query = query.Where(a => a.AcademicTermId == request.AcademicTermId.Value);
        }

        var assignments = await query
            .OrderByDescending(a => a.DueDate)
            .Select(a => new AssignmentDto
            {
                Id = a.Id,
                Title = a.Title,
                Description = a.Description,
                ClassName = a.Class.Name,
                SubjectName = a.Subject.Name,
                DueDate = a.DueDate,
                AttachmentFileName = a.AttachmentFileName,
                IsPublished = a.IsPublished,
                SubmissionCount = a.Submissions.Count
            })
            .ToListAsync(cancellationToken);

        return Result<List<AssignmentDto>>.Success(assignments);
    }
}
