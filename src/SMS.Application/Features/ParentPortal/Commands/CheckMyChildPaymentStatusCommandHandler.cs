using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Features.Payments.Commands;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.ParentPortal.Commands;

public class CheckMyChildPaymentStatusCommandHandler : IRequestHandler<CheckMyChildPaymentStatusCommand, Result<PaymentSettlementDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ISender _sender;

    public CheckMyChildPaymentStatusCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser, ISender sender)
    {
        _context = context;
        _currentUser = currentUser;
        _sender = sender;
    }

    public async Task<Result<PaymentSettlementDto>> Handle(CheckMyChildPaymentStatusCommand request, CancellationToken cancellationToken)
    {
        var studentId = await (
            from payment in _context.Payments
            join invoice in _context.Invoices on payment.InvoiceId equals invoice.Id
            where payment.Id == request.PaymentId
            select (Guid?)invoice.StudentId
        ).FirstOrDefaultAsync(cancellationToken);

        if (studentId is null || !await ParentChildAccess.OwnsStudentAsync(_context, _currentUser.UserId, studentId.Value, cancellationToken))
        {
            return Result<PaymentSettlementDto>.Failure("Payment not found");
        }

        return await _sender.Send(new CheckPaynowStatusCommand(request.PaymentId), cancellationToken);
    }
}
