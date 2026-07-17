using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Interfaces;
using SMS.Domain.Entities;
using SMS.Domain.Enums;

namespace SMS.API.Controllers;

/// <summary>
/// API endpoints for fee and payment management
/// </summary>
[Authorize]
public class FinanceController : BaseApiController
{
    private readonly IApplicationDbContext _context;

    public FinanceController(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Get all invoices with optional filters
    /// </summary>
    [HttpGet("invoices")]
    public async Task<IActionResult> GetInvoices(
        [FromQuery] Guid? studentId,
        [FromQuery] Guid? termId,
        [FromQuery] bool? unpaidOnly,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _context.Invoices
            .AsNoTracking()
            .Include(i => i.Student)
            .Include(i => i.AcademicTerm)
            .AsQueryable();

        if (studentId.HasValue)
            query = query.Where(i => i.StudentId == studentId.Value);

        if (termId.HasValue)
            query = query.Where(i => i.AcademicTermId == termId.Value);

        if (unpaidOnly == true)
            query = query.Where(i => i.TotalAmount - i.DiscountAmount - i.PaidAmount > 0);

        var total = await query.CountAsync();
        
        var invoices = await query
            .OrderByDescending(i => i.InvoiceDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(i => new InvoiceDto
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
                Currency = i.Currency
            })
            .ToListAsync();

        return Ok(new { Data = invoices, Total = total, Page = page, PageSize = pageSize });
    }

    /// <summary>
    /// Get invoice details by ID
    /// </summary>
    [HttpGet("invoices/{id:guid}")]
    public async Task<IActionResult> GetInvoice(Guid id)
    {
        var invoice = await _context.Invoices
            .AsNoTracking()
            .Include(i => i.Student)
            .Include(i => i.AcademicTerm)
            .Include(i => i.Items)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (invoice == null)
            return NotFound();

        return Ok(invoice);
    }

    /// <summary>
    /// Create a new invoice
    /// </summary>
    [HttpPost("invoices")]
    public async Task<IActionResult> CreateInvoice([FromBody] CreateInvoiceRequest request)
    {
        var invoiceNumber = await GenerateInvoiceNumberAsync();

        var invoice = new Invoice
        {
            InvoiceNumber = invoiceNumber,
            StudentId = request.StudentId,
            AcademicTermId = request.AcademicTermId,
            InvoiceDate = DateTime.UtcNow,
            DueDate = request.DueDate,
            TotalAmount = request.Items.Sum(i => i.Amount * i.Quantity),
            DiscountAmount = request.DiscountAmount ?? 0,
            Currency = string.IsNullOrWhiteSpace(request.Currency) ? "USD" : request.Currency,
            Notes = request.Notes
        };

        foreach (var item in request.Items)
        {
            invoice.Items.Add(new InvoiceItem
            {
                FeeStructureId = item.FeeStructureId,
                Description = item.Description,
                Amount = item.Amount,
                Quantity = item.Quantity
            });
        }

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetInvoice), new { id = invoice.Id }, invoice);
    }

    /// <summary>
    /// Record a payment
    /// </summary>
    [HttpPost("payments")]
    public async Task<IActionResult> RecordPayment([FromBody] RecordPaymentRequest request)
    {
        var invoice = await _context.Invoices.FindAsync(request.InvoiceId);
        
        if (invoice == null)
            return NotFound("Invoice not found");

        var receiptNumber = await GenerateReceiptNumberAsync();

        var payment = new Payment
        {
            ReceiptNumber = receiptNumber,
            InvoiceId = request.InvoiceId,
            Amount = request.Amount,
            Currency = string.IsNullOrWhiteSpace(request.Currency) ? invoice.Currency : request.Currency,
            ExchangeRate = request.ExchangeRate ?? 1m,
            PaymentMethod = Enum.Parse<PaymentMethod>(request.PaymentMethod),
            Status = PaymentStatus.Completed,
            PaymentDate = request.PaymentDate ?? DateTime.UtcNow,
            TransactionReference = request.TransactionReference,
            MobileMoneyNumber = request.MobileMoneyNumber,
            BankName = request.BankName,
            Notes = request.Notes
        };

        _context.Payments.Add(payment);

        // Credit the invoice in its own currency.
        invoice.PaidAmount += payment.AmountInInvoiceCurrency;

        await _context.SaveChangesAsync();

        return Ok(new { ReceiptNumber = receiptNumber, PaymentId = payment.Id });
    }

    /// <summary>
    /// Get payment history
    /// </summary>
    [HttpGet("payments")]
    public async Task<IActionResult> GetPayments(
        [FromQuery] Guid? studentId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _context.Payments
            .AsNoTracking()
            .Include(p => p.Invoice)
            .ThenInclude(i => i.Student)
            .AsQueryable();

        if (studentId.HasValue)
            query = query.Where(p => p.Invoice.StudentId == studentId.Value);

        if (fromDate.HasValue)
            query = query.Where(p => p.PaymentDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(p => p.PaymentDate <= toDate.Value);

        var total = await query.CountAsync();

        var payments = await query
            .OrderByDescending(p => p.PaymentDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PaymentDto
            {
                Id = p.Id,
                ReceiptNumber = p.ReceiptNumber,
                InvoiceNumber = p.Invoice.InvoiceNumber,
                StudentName = p.Invoice.Student.FullName,
                Amount = p.Amount,
                PaymentMethod = p.PaymentMethod.ToString(),
                PaymentDate = p.PaymentDate,
                TransactionReference = p.TransactionReference
            })
            .ToListAsync();

        return Ok(new { Data = payments, Total = total, Page = page, PageSize = pageSize });
    }

    /// <summary>
    /// Get fee collection summary
    /// </summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetFinanceSummary([FromQuery] Guid? termId)
    {
        var invoiceQuery = _context.Invoices.AsNoTracking();
        var paymentQuery = _context.Payments.AsNoTracking();

        if (termId.HasValue)
        {
            invoiceQuery = invoiceQuery.Where(i => i.AcademicTermId == termId.Value);
            paymentQuery = paymentQuery.Where(p => p.Invoice.AcademicTermId == termId.Value);
        }

        var totalBilled = await invoiceQuery.SumAsync(i => i.TotalAmount - i.DiscountAmount);
        var totalCollected = await paymentQuery.Where(p => p.Status == PaymentStatus.Completed).SumAsync(p => p.Amount);
        var totalOutstanding = totalBilled - totalCollected;

        var invoiceCount = await invoiceQuery.CountAsync();
        var paidInvoices = await invoiceQuery.CountAsync(i => i.TotalAmount - i.DiscountAmount - i.PaidAmount <= 0);

        return Ok(new
        {
            TotalBilled = totalBilled,
            TotalCollected = totalCollected,
            TotalOutstanding = totalOutstanding,
            CollectionRate = totalBilled > 0 ? Math.Round(totalCollected / totalBilled * 100, 2) : 0,
            TotalInvoices = invoiceCount,
            PaidInvoices = paidInvoices,
            UnpaidInvoices = invoiceCount - paidInvoices
        });
    }

    private async Task<string> GenerateInvoiceNumberAsync()
    {
        var year = DateTime.UtcNow.Year;
        var count = await _context.Invoices.CountAsync(i => i.InvoiceDate.Year == year) + 1;
        return $"INV-{year}-{count:D6}";
    }

    private async Task<string> GenerateReceiptNumberAsync()
    {
        var year = DateTime.UtcNow.Year;
        var count = await _context.Payments.CountAsync(p => p.PaymentDate.Year == year) + 1;
        return $"RCP-{year}-{count:D6}";
    }
}

public record InvoiceDto
{
    public Guid Id { get; init; }
    public string InvoiceNumber { get; init; } = string.Empty;
    public Guid StudentId { get; init; }
    public string StudentName { get; init; } = string.Empty;
    public string StudentNumber { get; init; } = string.Empty;
    public string TermName { get; init; } = string.Empty;
    public DateTime InvoiceDate { get; init; }
    public DateTime DueDate { get; init; }
    public decimal TotalAmount { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal PaidAmount { get; init; }
    public decimal Balance { get; init; }
    public string Currency { get; init; } = "USD";
}

public record CreateInvoiceRequest
{
    public Guid StudentId { get; init; }
    public Guid AcademicTermId { get; init; }
    public DateTime DueDate { get; init; }
    public decimal? DiscountAmount { get; init; }
    public string? Currency { get; init; }
    public string? Notes { get; init; }
    public List<InvoiceItemRequest> Items { get; init; } = [];
}

public record InvoiceItemRequest
{
    public Guid? FeeStructureId { get; init; }
    public string Description { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public int Quantity { get; init; } = 1;
}

public record RecordPaymentRequest
{
    public Guid InvoiceId { get; init; }
    public decimal Amount { get; init; }
    public string? Currency { get; init; }
    public decimal? ExchangeRate { get; init; }
    public string PaymentMethod { get; init; } = string.Empty;
    public DateTime? PaymentDate { get; init; }
    public string? TransactionReference { get; init; }
    public string? MobileMoneyNumber { get; init; }
    public string? BankName { get; init; }
    public string? Notes { get; init; }
}

public record PaymentDto
{
    public Guid Id { get; init; }
    public string ReceiptNumber { get; init; } = string.Empty;
    public string InvoiceNumber { get; init; } = string.Empty;
    public string StudentName { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string PaymentMethod { get; init; } = string.Empty;
    public DateTime PaymentDate { get; init; }
    public string? TransactionReference { get; init; }
}
