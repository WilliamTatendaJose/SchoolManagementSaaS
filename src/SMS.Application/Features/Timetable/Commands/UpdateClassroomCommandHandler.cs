using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Timetable.Commands;

public class UpdateClassroomCommandHandler : IRequestHandler<UpdateClassroomCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public UpdateClassroomCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(UpdateClassroomCommand request, CancellationToken cancellationToken)
    {
        var classroom = await _context.Classrooms.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (classroom == null)
        {
            return Result.Failure("Classroom not found");
        }

        classroom.Name = request.Name;
        classroom.Building = request.Building;
        classroom.Capacity = request.Capacity;
        classroom.HasProjector = request.HasProjector;
        classroom.HasWhiteboard = request.HasWhiteboard;
        classroom.IsActive = request.IsActive;

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
