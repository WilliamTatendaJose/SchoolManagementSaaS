using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;
using SMS.Domain.Enums;

namespace SMS.Application.Features.Finance.Queries;

public class GetFinanceSummaryQueryHandler : IRequestHandler<GetFinanceSummaryQuery, Result<FinanceSummaryDto>>
{
    private readonly IApplicationDbContext _context;

    public GetFinanceSummaryQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<FinanceSummaryDto>> Handle(GetFinanceSummaryQuery request, CancellationToken cancellationToken)
    {
        var invoiceQuery = _context.Invoices.AsNoTracking().AsQueryable();
        var paymentQuery = _context.Payments.AsNoTracking().Include(p => p.Invoice).AsQueryable();

        // Apply term filter
        if (request.AcademicTermId.HasValue)
        {
            invoiceQuery = invoiceQuery.Where(i => i.AcademicTermId == request.AcademicTermId.Value);
            paymentQuery = paymentQuery.Where(p => p.Invoice.AcademicTermId == request.AcademicTermId.Value);
        }

        // Apply date filters
        if (request.StartDate.HasValue)
        {
            invoiceQuery = invoiceQuery.Where(i => i.InvoiceDate >= request.StartDate.Value);
            paymentQuery = paymentQuery.Where(p => p.PaymentDate >= request.StartDate.Value);
        }

        if (request.EndDate.HasValue)
        {
            invoiceQuery = invoiceQuery.Where(i => i.InvoiceDate <= request.EndDate.Value);
            paymentQuery = paymentQuery.Where(p => p.PaymentDate <= request.EndDate.Value);
        }

        var invoices = await invoiceQuery.ToListAsync(cancellationToken);
        var payments = await paymentQuery.Where(p => p.Status == PaymentStatus.Completed).ToListAsync(cancellationToken);

        var totalBilled = invoices.Sum(i => i.TotalAmount - i.DiscountAmount);
        var totalCollected = payments.Sum(p => p.Amount);
        var totalOutstanding = totalBilled - totalCollected;

        var totalInvoices = invoices.Count;
        var paidInvoices = invoices.Count(i => i.TotalAmount - i.DiscountAmount - i.PaidAmount <= 0);
        var overdueInvoices = invoices.Count(i => 
            i.DueDate < DateTime.UtcNow && 
            (i.TotalAmount - i.DiscountAmount - i.PaidAmount) > 0);

        var paymentMethodBreakdown = payments
            .GroupBy(p => p.PaymentMethod)
            .Select(g => new PaymentMethodBreakdownDto
            {
                Method = g.Key.ToString(),
                Amount = g.Sum(p => p.Amount),
                Count = g.Count()
            })
            .OrderByDescending(p => p.Amount)
            .ToList();

        var summary = new FinanceSummaryDto
        {
            TotalBilled = totalBilled,
            TotalCollected = totalCollected,
            TotalOutstanding = totalOutstanding,
            CollectionRate = totalBilled > 0 ? Math.Round(totalCollected / totalBilled * 100, 2) : 0,
            TotalInvoices = totalInvoices,
            PaidInvoices = paidInvoices,
            UnpaidInvoices = totalInvoices - paidInvoices,
            OverdueInvoices = overdueInvoices,
            PaymentMethodBreakdown = paymentMethodBreakdown
        };

        return Result<FinanceSummaryDto>.Success(summary);
    }
}
