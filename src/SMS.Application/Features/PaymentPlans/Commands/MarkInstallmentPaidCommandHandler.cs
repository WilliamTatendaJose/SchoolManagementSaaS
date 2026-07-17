using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.PaymentPlans.Commands;

public class MarkInstallmentPaidCommandHandler : IRequestHandler<MarkInstallmentPaidCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public MarkInstallmentPaidCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(MarkInstallmentPaidCommand request, CancellationToken cancellationToken)
    {
        var installment = await _context.Installments
            .Include(i => i.PaymentPlan)
            .ThenInclude(p => p.Installments)
            .FirstOrDefaultAsync(i => i.Id == request.InstallmentId, cancellationToken);

        if (installment == null)
        {
            return Result.Failure("Installment not found");
        }

        if (installment.IsPaid)
        {
            return Result.Failure("Installment is already marked as paid");
        }

        installment.IsPaid = true;
        installment.PaidDate = (request.PaidDate ?? DateTime.UtcNow).Date;

        // Complete the plan once every installment is settled.
        if (installment.PaymentPlan.Installments.All(i => i.IsPaid))
        {
            installment.PaymentPlan.Status = "Completed";
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
