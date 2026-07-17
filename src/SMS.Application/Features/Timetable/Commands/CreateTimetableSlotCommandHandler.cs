using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;
using SMS.Domain.Entities;

namespace SMS.Application.Features.Timetable.Commands;

public class CreateTimetableSlotCommandHandler : IRequestHandler<CreateTimetableSlotCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public CreateTimetableSlotCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreateTimetableSlotCommand request, CancellationToken cancellationToken)
    {
        if (!await _context.Classes.AnyAsync(c => c.Id == request.ClassId, cancellationToken))
        {
            return Result<Guid>.Failure("Class not found");
        }

        if (!await _context.Subjects.AnyAsync(s => s.Id == request.SubjectId, cancellationToken))
        {
            return Result<Guid>.Failure("Subject not found");
        }

        if (!await _context.Staff.AnyAsync(s => s.Id == request.TeacherId && s.IsTeacher, cancellationToken))
        {
            return Result<Guid>.Failure("Teacher not found");
        }

        if (!await _context.AcademicTerms.AnyAsync(t => t.Id == request.AcademicTermId, cancellationToken))
        {
            return Result<Guid>.Failure("Academic term not found");
        }

        if (request.ClassroomId.HasValue
            && !await _context.Classrooms.AnyAsync(c => c.Id == request.ClassroomId.Value, cancellationToken))
        {
            return Result<Guid>.Failure("Classroom not found");
        }

        var clash = await TimetableClashDetector.FindClashAsync(
            _context, request.AcademicTermId, request.DayOfWeek, request.StartTime, request.EndTime,
            request.ClassId, request.TeacherId, request.ClassroomId, excludeSlotId: null, cancellationToken);

        if (clash != null)
        {
            return Result<Guid>.Failure(clash);
        }

        var slot = new TimetableSlot
        {
            ClassId = request.ClassId,
            SubjectId = request.SubjectId,
            TeacherId = request.TeacherId,
            ClassroomId = request.ClassroomId,
            AcademicTermId = request.AcademicTermId,
            DayOfWeek = request.DayOfWeek,
            StartTime = request.StartTime,
            EndTime = request.EndTime
        };

        _context.TimetableSlots.Add(slot);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(slot.Id);
    }
}
