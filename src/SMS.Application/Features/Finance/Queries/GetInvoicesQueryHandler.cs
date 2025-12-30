using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Finance.Queries;

public class GetInvoicesQueryHandler : IRequestHandler<GetInvoicesQuery, Result<PaginatedList<InvoiceListDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetInvoicesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PaginatedList<InvoiceListDto>>> Handle(GetInvoicesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Invoices
            .AsNoTracking()
            .Include(i => i.Student)
            .Include(i => i.AcademicTerm)
            .AsQueryable();

        if (request.StudentId.HasValue)
        {
            query = query.Where(i => i.StudentId == request.StudentId.Value);
        }

        if (request.AcademicTermId.HasValue)
        {
            query = query.Where(i => i.AcademicTermId == request.AcademicTermId.Value);
        }

        if (request.UnpaidOnly == true)
        {
            query = query.Where(i => i.TotalAmount - i.DiscountAmount - i.PaidAmount > 0);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var invoices = await query
            .OrderByDescending(i => i.InvoiceDate)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(i => new InvoiceListDto
            {
                Id = i.Id,
                InvoiceNumber = i.InvoiceNumber,
                StudentId = i.StudentId,
                StudentName = i.Student.FullName,
                StudentNumber = i.Student.StudentNumber,
                TermName = i.AcademicTerm.Name,
                InvoiceDate = i.InvoiceDate,
                DueDate = i.DueDate,
                TotalAmount = i.TotalAmount,
                DiscountAmount = i.DiscountAmount,
                PaidAmount = i.PaidAmount,
                Balance = i.TotalAmount - i.DiscountAmount - i.PaidAmount,
                IsOverdue = i.DueDate < DateTime.UtcNow && (i.TotalAmount - i.DiscountAmount - i.PaidAmount) > 0
            })
            .ToListAsync(cancellationToken);

        var paginatedList = new PaginatedList<InvoiceListDto>(invoices, totalCount, request.PageNumber, request.PageSize);

        return Result<PaginatedList<InvoiceListDto>>.Success(paginatedList);
    }
}
