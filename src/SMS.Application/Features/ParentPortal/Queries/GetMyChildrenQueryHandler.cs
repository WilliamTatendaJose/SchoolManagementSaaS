using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.ParentPortal.Queries;

public class GetMyChildrenQueryHandler : IRequestHandler<GetMyChildrenQuery, Result<List<MyChildDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetMyChildrenQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<List<MyChildDto>>> Handle(GetMyChildrenQuery request, CancellationToken cancellationToken)
    {
        var childIds = await ParentChildAccess.GetChildStudentIdsAsync(_context, _currentUser.UserId, cancellationToken);
        if (childIds.Count == 0)
        {
            return Result<List<MyChildDto>>.Success([]);
        }

        // Invoice balances (summed in memory to avoid provider-specific decimal SQL).
        var invoiceRows = await _context.Invoices
            .Where(i => childIds.Contains(i.StudentId))
            .Select(i => new { i.StudentId, i.TotalAmount, i.DiscountAmount, i.PaidAmount })
            .ToListAsync(cancellationToken);

        var balanceByStudent = invoiceRows
            .GroupBy(i => i.StudentId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.TotalAmount - x.DiscountAmount - x.PaidAmount));

        var children = await _context.Students
            .AsNoTracking()
            .Include(s => s.CurrentClass)
            .Where(s => childIds.Contains(s.Id))
            .Select(s => new
            {
                s.Id,
                s.StudentNumber,
                s.FirstName,
                s.LastName,
                ClassName = s.CurrentClass != null ? s.CurrentClass.Name : null
            })
            .ToListAsync(cancellationToken);

        var result = children
            .Select(s => new MyChildDto
            {
                StudentId = s.Id,
                StudentNumber = s.StudentNumber,
                FullName = s.FirstName + " " + s.LastName,
                ClassName = s.ClassName,
                OutstandingBalance = balanceByStudent.GetValueOrDefault(s.Id, 0m)
            })
            .OrderBy(c => c.FullName)
            .ToList();

        return Result<List<MyChildDto>>.Success(result);
    }
}
