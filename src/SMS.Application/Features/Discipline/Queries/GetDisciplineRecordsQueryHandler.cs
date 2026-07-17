using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Discipline.Queries;

public class GetDisciplineRecordsQueryHandler : IRequestHandler<GetDisciplineRecordsQuery, Result<PaginatedList<DisciplineRecordDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetDisciplineRecordsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PaginatedList<DisciplineRecordDto>>> Handle(GetDisciplineRecordsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.DisciplineRecords
            .AsNoTracking()
            .Include(d => d.Student)
            .AsQueryable();

        if (request.StudentId.HasValue)
        {
            query = query.Where(d => d.StudentId == request.StudentId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.IncidentType))
        {
            query = query.Where(d => d.IncidentType == request.IncidentType);
        }

        query = query.OrderByDescending(d => d.IncidentDate);

        var totalCount = await query.CountAsync(cancellationToken);

        var records = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(d => new DisciplineRecordDto
            {
                Id = d.Id,
                StudentId = d.StudentId,
                StudentName = d.Student.FirstName + " " + d.Student.LastName,
                IncidentDate = d.IncidentDate,
                IncidentType = d.IncidentType,
                Description = d.Description,
                ActionTaken = d.ActionTaken,
                DemeritsAwarded = d.DemeritsAwarded,
                MeritsAwarded = d.MeritsAwarded,
                GuardianNotified = d.GuardianNotified,
                NotificationDate = d.NotificationDate
            })
            .ToListAsync(cancellationToken);

        var paginatedList = new PaginatedList<DisciplineRecordDto>(records, totalCount, request.PageNumber, request.PageSize);

        return Result<PaginatedList<DisciplineRecordDto>>.Success(paginatedList);
    }
}
