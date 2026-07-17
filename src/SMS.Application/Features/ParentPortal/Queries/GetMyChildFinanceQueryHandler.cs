using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.ParentPortal.Queries;

public class GetMyChildFinanceQueryHandler : IRequestHandler<GetMyChildFinanceQuery, Result<MyChildFinanceDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetMyChildFinanceQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<MyChildFinanceDto>> Handle(GetMyChildFinanceQuery request, CancellationToken cancellationToken)
    {
        if (!await ParentChildAccess.OwnsStudentAsync(_context, _currentUser.UserId, request.StudentId, cancellationToken))
        {
            return Result<MyChildFinanceDto>.Failure("Student not found");
        }

        var invoices = await _context.Invoices
            .AsNoTracking()
            .Where(i => i.StudentId == request.StudentId)
            .OrderByDescending(i => i.InvoiceDate)
            .Select(i => new
            {
                i.Id,
                i.InvoiceNumber,
                i.InvoiceDate,
                i.DueDate,
                i.TotalAmount,
                i.DiscountAmount,
                i.PaidAmount
            })
            .ToListAsync(cancellationToken);

        var invoiceDtos = invoices.Select(i =>
        {
            var balance = i.TotalAmount - i.DiscountAmount - i.PaidAmount;
            return new MyChildInvoiceDto
            {
                InvoiceId = i.Id,
                InvoiceNumber = i.InvoiceNumber,
                InvoiceDate = i.InvoiceDate,
                DueDate = i.DueDate,
                TotalAmount = i.TotalAmount,
                Balance = balance,
                Payable = balance > 0
            };
        }).ToList();

        var dto = new MyChildFinanceDto
        {
            StudentId = request.StudentId,
            TotalBilled = invoices.Sum(i => i.TotalAmount - i.DiscountAmount),
            TotalPaid = invoices.Sum(i => i.PaidAmount),
            OutstandingBalance = invoiceDtos.Sum(i => i.Balance),
            Invoices = invoiceDtos
        };

        return Result<MyChildFinanceDto>.Success(dto);
    }
}
