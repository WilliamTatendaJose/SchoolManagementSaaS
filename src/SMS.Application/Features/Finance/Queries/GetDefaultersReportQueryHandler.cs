using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Finance.Queries;

public class GetDefaultersReportQueryHandler : IRequestHandler<GetDefaultersReportQuery, Result<List<DefaulterDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetDefaultersReportQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<DefaulterDto>>> Handle(GetDefaultersReportQuery request, CancellationToken cancellationToken)
    {
        var studentScope = _context.Students.AsQueryable();
        if (request.ClassId is { } classId)
        {
            studentScope = studentScope.Where(s => s.CurrentClassId == classId);
        }

        var students = await studentScope
            .AsNoTracking()
            .Select(s => new
            {
                s.Id,
                s.StudentNumber,
                s.FirstName,
                s.LastName,
                ClassName = s.CurrentClass != null ? s.CurrentClass.Name : null
            })
            .ToListAsync(cancellationToken);

        var scopeIds = students.Select(s => s.Id).ToHashSet();

        var invoiceRows = await _context.Invoices
            .Where(i => scopeIds.Contains(i.StudentId))
            .Select(i => new { i.StudentId, i.TotalAmount, i.DiscountAmount, i.PaidAmount })
            .ToListAsync(cancellationToken);

        var balanceByStudent = invoiceRows
            .GroupBy(i => i.StudentId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.TotalAmount - x.DiscountAmount - x.PaidAmount));

        var defaulterIds = balanceByStudent.Where(kv => kv.Value > 0).Select(kv => kv.Key).ToHashSet();
        if (defaulterIds.Count == 0)
        {
            return Result<List<DefaulterDto>>.Success([]);
        }

        // Primary guardian contact per defaulter (fall back to any guardian with a phone).
        var links = await _context.StudentGuardians
            .Where(sg => defaulterIds.Contains(sg.StudentId))
            .Select(sg => new
            {
                sg.StudentId,
                sg.IsPrimaryContact,
                Name = sg.Guardian.FirstName + " " + sg.Guardian.LastName,
                sg.Guardian.Phone
            })
            .ToListAsync(cancellationToken);

        var contactByStudent = links
            .GroupBy(l => l.StudentId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(x => x.IsPrimaryContact).First());

        var report = students
            .Where(s => defaulterIds.Contains(s.Id))
            .Select(s =>
            {
                contactByStudent.TryGetValue(s.Id, out var contact);
                return new DefaulterDto
                {
                    StudentId = s.Id,
                    StudentNumber = s.StudentNumber,
                    StudentName = s.FirstName + " " + s.LastName,
                    ClassName = s.ClassName,
                    GuardianName = contact?.Name,
                    GuardianPhone = contact?.Phone,
                    OutstandingBalance = balanceByStudent[s.Id]
                };
            })
            .OrderByDescending(d => d.OutstandingBalance)
            .ToList();

        return Result<List<DefaulterDto>>.Success(report);
    }
}
