using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Features.Payments.Commands;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.ParentPortal.Commands;

public class InitiateMyChildOnlinePaymentCommandHandler : IRequestHandler<InitiateMyChildOnlinePaymentCommand, Result<OnlinePaymentInitiationDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ISender _sender;

    public InitiateMyChildOnlinePaymentCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser, ISender sender)
    {
        _context = context;
        _currentUser = currentUser;
        _sender = sender;
    }

    public async Task<Result<OnlinePaymentInitiationDto>> Handle(InitiateMyChildOnlinePaymentCommand request, CancellationToken cancellationToken)
    {
        var studentId = await _context.Invoices
            .Where(i => i.Id == request.InvoiceId)
            .Select(i => (Guid?)i.StudentId)
            .FirstOrDefaultAsync(cancellationToken);

        // Same "not found" error whether the invoice is missing or just not the caller's
        // child's - never confirm the existence of another family's invoice.
        if (studentId is null || !await ParentChildAccess.OwnsStudentAsync(_context, _currentUser.UserId, studentId.Value, cancellationToken))
        {
            return Result<OnlinePaymentInitiationDto>.Failure("Invoice not found");
        }

        return await _sender.Send(new InitiateOnlinePaymentCommand
        {
            InvoiceId = request.InvoiceId,
            Amount = request.Amount,
            Email = request.Email,
            Phone = request.Phone
        }, cancellationToken);
    }
}
