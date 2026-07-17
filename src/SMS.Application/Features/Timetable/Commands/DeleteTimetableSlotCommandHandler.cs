using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Timetable.Commands;

public class DeleteTimetableSlotCommandHandler : IRequestHandler<DeleteTimetableSlotCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public DeleteTimetableSlotCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(DeleteTimetableSlotCommand request, CancellationToken cancellationToken)
    {
        var slot = await _context.TimetableSlots.FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);
        if (slot == null)
        {
            return Result.Failure("Timetable slot not found");
        }

        _context.TimetableSlots.Remove(slot);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
