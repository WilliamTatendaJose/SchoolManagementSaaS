using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Timetable.Commands;

public class UpdateTimetableSlotCommandHandler : IRequestHandler<UpdateTimetableSlotCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public UpdateTimetableSlotCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(UpdateTimetableSlotCommand request, CancellationToken cancellationToken)
    {
        var slot = await _context.TimetableSlots.FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);
        if (slot == null)
        {
            return Result.Failure("Timetable slot not found");
        }

        if (!await _context.Staff.AnyAsync(s => s.Id == request.TeacherId && s.IsTeacher, cancellationToken))
        {
            return Result.Failure("Teacher not found");
        }

        if (request.ClassroomId.HasValue
            && !await _context.Classrooms.AnyAsync(c => c.Id == request.ClassroomId.Value, cancellationToken))
        {
            return Result.Failure("Classroom not found");
        }

        var clash = await TimetableClashDetector.FindClashAsync(
            _context, request.AcademicTermId, request.DayOfWeek, request.StartTime, request.EndTime,
            request.ClassId, request.TeacherId, request.ClassroomId, excludeSlotId: slot.Id, cancellationToken);

        if (clash != null)
        {
            return Result.Failure(clash);
        }

        slot.ClassId = request.ClassId;
        slot.SubjectId = request.SubjectId;
        slot.TeacherId = request.TeacherId;
        slot.ClassroomId = request.ClassroomId;
        slot.AcademicTermId = request.AcademicTermId;
        slot.DayOfWeek = request.DayOfWeek;
        slot.StartTime = request.StartTime;
        slot.EndTime = request.EndTime;

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
