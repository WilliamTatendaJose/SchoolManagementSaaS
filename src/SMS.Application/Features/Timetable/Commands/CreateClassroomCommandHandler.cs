using MediatR;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;
using SMS.Domain.Entities;

namespace SMS.Application.Features.Timetable.Commands;

public class CreateClassroomCommandHandler : IRequestHandler<CreateClassroomCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public CreateClassroomCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreateClassroomCommand request, CancellationToken cancellationToken)
    {
        var classroom = new Classroom
        {
            Name = request.Name,
            Building = request.Building,
            Capacity = request.Capacity,
            HasProjector = request.HasProjector,
            HasWhiteboard = request.HasWhiteboard,
            IsActive = true
        };

        _context.Classrooms.Add(classroom);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(classroom.Id);
    }
}
