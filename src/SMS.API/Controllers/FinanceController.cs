using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Finance;
using SMS.Application.Common.Security;
using SMS.Application.Interfaces;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using SMS.Infrastructure.Authorization;

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
    [RequirePermission(Permissions.FinanceView)]
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
    [RequirePermission(Permissions.FinanceView)]
    public async Task<IActionResult> GetInvoice(Guid id)
    {
        // Project to a DTO rather than returning the tracked entity graph directly:
        // Invoice -> Student/AcademicTerm carry navigation properties that cycle back
        // (e.g. AcademicTerm.AcademicYear.Terms), which crashes System.Text.Json with
        // an object-cycle exception (surfaces to the client as an opaque 500).
        var invoice = await _context.Invoices
            .AsNoTracking()
            .Where(i => i.Id == id)
            .Select(i => new InvoiceDetailDto
            {
                Id = i.Id,
                InvoiceNumber = i.InvoiceNumber,
                StudentId = i.StudentId,
                AcademicTermId = i.AcademicTermId,
                InvoiceDate = i.InvoiceDate,
                DueDate = i.DueDate,
                TotalAmount = i.TotalAmount,
                DiscountAmount = i.DiscountAmount,
                PaidAmount = i.PaidAmount,
                Balance = i.TotalAmount - i.DiscountAmount - i.PaidAmount,
                Currency = i.Currency,
                Notes = i.Notes,
                IsPaid = i.TotalAmount - i.DiscountAmount - i.PaidAmount <= 0,
                Student = new InvoiceStudentDto
                {
                    Id = i.Student.Id,
                    FullName = i.Student.FullName,
                    StudentNumber = i.Student.StudentNumber
                },
                AcademicTerm = new InvoiceTermDto
                {
                    Id = i.AcademicTerm.Id,
                    Name = i.AcademicTerm.Name
                },
                Items = i.Items.Select(item => new InvoiceLineItemDto
                {
                    Id = item.Id,
                    FeeStructureId = item.FeeStructureId,
                    Description = item.Description,
                    Amount = item.Amount,
                    Quantity = item.Quantity
                }).ToList(),
                Payments = i.Payments.Select(p => new InvoicePaymentSummaryDto
                {
                    Id = p.Id,
                    ReceiptNumber = p.ReceiptNumber,
                    Amount = p.Amount,
                    PaymentMethod = p.PaymentMethod.ToString(),
                    PaymentDate = p.PaymentDate,
                    TransactionReference = p.TransactionReference
                }).ToList()
            })
            .FirstOrDefaultAsync();

        if (invoice == null)
            return NotFound();

        return Ok(invoice);
    }

    /// <summary>
    /// Create a new invoice
    /// </summary>
    [HttpPost("invoices")]
    [RequirePermission(Permissions.InvoicesCreate)]
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

        // Return just the id, not the tracked entity: Invoice.Items/Payments navigate
        // back to Invoice (EF relationship fixup sets InvoiceItem.Invoice on Add), which
        // crashes System.Text.Json with an object-cycle exception if serialized directly.
        return CreatedAtAction(nameof(GetInvoice), new { id = invoice.Id }, new { Id = invoice.Id });
    }

    /// <summary>
    /// Record a payment. Supports paying from a student's prepaid account balance
    /// (PaymentMethod "AccountCredit"), and — for Cash — reconciling a tendered amount
    /// against what's owed, either handing back change or crediting the excess to the
    /// student's account for next time.
    /// </summary>
    [HttpPost("payments")]
    [RequirePermission(Permissions.PaymentsRecord)]
    public async Task<IActionResult> RecordPayment([FromBody] RecordPaymentRequest request)
    {
        var invoice = await _context.Invoices.FindAsync(request.InvoiceId);

        if (invoice == null)
            return NotFound("Invoice not found");

        if (!Enum.TryParse<PaymentMethod>(request.PaymentMethod, out var paymentMethod))
            return BadRequest($"Invalid payment method: {request.PaymentMethod}");

        if (invoice.Balance <= 0)
            return BadRequest("This invoice is already fully paid");

        if (request.Amount <= 0)
            return BadRequest("Amount must be greater than zero");

        decimal amountToApply;
        decimal changeDue = 0;
        decimal creditedAmount = 0;

        if (paymentMethod == PaymentMethod.AccountCredit)
        {
            var balance = await GetAccountBalanceAsync(invoice.StudentId);
            if (request.Amount > balance)
                return BadRequest($"Insufficient account balance. Available: {balance:F2}");
            if (request.Amount > invoice.Balance)
                return BadRequest("Amount exceeds the invoice balance");

            amountToApply = request.Amount;
        }
        else
        {
            // Reconcile any excess over what's actually owed - whether it comes from cash
            // physically tendered above the entered amount, or from the entered amount
            // itself exceeding the invoice balance. An invoice can never be pushed into
            // overpayment (negative balance) without the surplus being tracked as either
            // change handed back or account credit.
            var tendered = request.AmountTendered ?? request.Amount;
            if (tendered < request.Amount)
                return BadRequest("Amount tendered cannot be less than the amount");

            amountToApply = Math.Min(request.Amount, invoice.Balance);
            var excess = tendered - amountToApply;

            if (excess > 0)
            {
                // Only cash can be physically handed back as change. Every other method
                // (card, bank transfer, mobile money, cheque) has no such mechanism, so
                // any surplus there always becomes account credit regardless of
                // ExcessHandling.
                var wantsCredit = string.Equals(request.ExcessHandling, "Credit", StringComparison.OrdinalIgnoreCase);
                if (paymentMethod == PaymentMethod.Cash && !wantsCredit)
                    changeDue = excess;
                else
                    creditedAmount = excess;
            }
        }

        var receiptNumber = await GenerateReceiptNumberAsync();

        var payment = new Payment
        {
            ReceiptNumber = receiptNumber,
            InvoiceId = request.InvoiceId,
            Amount = amountToApply,
            Currency = string.IsNullOrWhiteSpace(request.Currency) ? invoice.Currency : request.Currency,
            ExchangeRate = request.ExchangeRate ?? 1m,
            PaymentMethod = paymentMethod,
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

        if (paymentMethod == PaymentMethod.AccountCredit)
        {
            _context.StudentAccountTransactions.Add(new StudentAccountTransaction
            {
                StudentId = invoice.StudentId,
                Amount = -amountToApply,
                Type = StudentAccountTransactionTypes.AppliedToInvoice,
                InvoiceId = invoice.Id,
                PaymentId = payment.Id,
                Notes = $"Applied to invoice {invoice.InvoiceNumber}"
            });
        }
        else if (creditedAmount > 0)
        {
            _context.StudentAccountTransactions.Add(new StudentAccountTransaction
            {
                StudentId = invoice.StudentId,
                Amount = creditedAmount,
                Type = StudentAccountTransactionTypes.OverpaymentCredit,
                PaymentId = payment.Id,
                Notes = $"Overpayment on receipt {receiptNumber} credited to account"
            });
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            ReceiptNumber = receiptNumber,
            PaymentId = payment.Id,
            AmountApplied = amountToApply,
            ChangeDue = changeDue,
            CreditedToAccount = creditedAmount
        });
    }

    /// <summary>
    /// Get a student's prepaid account balance (credit from overpayments, available to
    /// apply toward future invoices).
    /// </summary>
    [HttpGet("students/{studentId:guid}/account-balance")]
    [RequirePermission(Permissions.FinanceView)]
    public async Task<IActionResult> GetAccountBalance(Guid studentId)
    {
        if (!await _context.Students.AnyAsync(s => s.Id == studentId))
            return NotFound("Student not found");

        var balance = await GetAccountBalanceAsync(studentId);
        return Ok(new { StudentId = studentId, Balance = balance });
    }

    /// <summary>
    /// Get a student's account transaction history (deposits/credits and debits).
    /// </summary>
    [HttpGet("students/{studentId:guid}/account-transactions")]
    [RequirePermission(Permissions.FinanceView)]
    public async Task<IActionResult> GetAccountTransactions(Guid studentId)
    {
        var transactions = await _context.StudentAccountTransactions
            .AsNoTracking()
            .Where(t => t.StudentId == studentId)
            .OrderByDescending(t => t.TransactionDate)
            .Select(t => new StudentAccountTransactionDto
            {
                Id = t.Id,
                Amount = t.Amount,
                Type = t.Type,
                InvoiceId = t.InvoiceId,
                Notes = t.Notes,
                TransactionDate = t.TransactionDate
            })
            .ToListAsync();

        return Ok(transactions);
    }

    private async Task<decimal> GetAccountBalanceAsync(Guid studentId)
    {
        return await _context.StudentAccountTransactions
            .Where(t => t.StudentId == studentId)
            .SumAsync(t => (decimal?)t.Amount) ?? 0m;
    }

    /// <summary>
    /// Get payment history
    /// </summary>
    [HttpGet("payments")]
    [RequirePermission(Permissions.FinanceView)]
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
                InvoiceId = p.InvoiceId,
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
    [RequirePermission(Permissions.FinanceView)]
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

    /// <summary>Cash only: what the payer physically handed over, if more than Amount.</summary>
    public decimal? AmountTendered { get; init; }

    /// <summary>"Change" (default) or "Credit" - how to handle any excess over AmountTendered.</summary>
    public string? ExcessHandling { get; init; }
}

public record PaymentDto
{
    public Guid Id { get; init; }
    public string ReceiptNumber { get; init; } = string.Empty;
    public Guid InvoiceId { get; init; }
    public string InvoiceNumber { get; init; } = string.Empty;
    public string StudentName { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string PaymentMethod { get; init; } = string.Empty;
    public DateTime PaymentDate { get; init; }
    public string? TransactionReference { get; init; }
}

public record InvoiceDetailDto
{
    public Guid Id { get; init; }
    public string InvoiceNumber { get; init; } = string.Empty;
    public Guid StudentId { get; init; }
    public Guid AcademicTermId { get; init; }
    public DateTime InvoiceDate { get; init; }
    public DateTime DueDate { get; init; }
    public decimal TotalAmount { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal PaidAmount { get; init; }
    public decimal Balance { get; init; }
    public string Currency { get; init; } = "USD";
    public string? Notes { get; init; }
    public bool IsPaid { get; init; }
    public InvoiceStudentDto Student { get; init; } = null!;
    public InvoiceTermDto AcademicTerm { get; init; } = null!;
    public List<InvoiceLineItemDto> Items { get; init; } = [];
    public List<InvoicePaymentSummaryDto> Payments { get; init; } = [];
}

public record InvoiceStudentDto
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string StudentNumber { get; init; } = string.Empty;
}

public record InvoiceTermDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
}

public record InvoiceLineItemDto
{
    public Guid Id { get; init; }
    public Guid? FeeStructureId { get; init; }
    public string Description { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public int Quantity { get; init; }
}

public record InvoicePaymentSummaryDto
{
    public Guid Id { get; init; }
    public string ReceiptNumber { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string PaymentMethod { get; init; } = string.Empty;
    public DateTime PaymentDate { get; init; }
    public string? TransactionReference { get; init; }
}

public record StudentAccountTransactionDto
{
    public Guid Id { get; init; }
    public decimal Amount { get; init; }
    public string Type { get; init; } = string.Empty;
    public Guid? InvoiceId { get; init; }
    public string? Notes { get; init; }
    public DateTime TransactionDate { get; init; }
}
