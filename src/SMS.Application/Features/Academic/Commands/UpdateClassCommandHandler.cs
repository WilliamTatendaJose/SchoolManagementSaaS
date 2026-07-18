using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Academic.Commands;

public class UpdateClassCommandHandler : IRequestHandler<UpdateClassCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public UpdateClassCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(UpdateClassCommand request, CancellationToken cancellationToken)
    {
        var classEntity = await _context.Classes
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (classEntity == null)
        {
            return Result.Failure("Class not found");
        }

        classEntity.Name = request.Name;
        classEntity.Code = request.Code;
        classEntity.Level = request.Level;
        classEntity.Capacity = request.Capacity;
        classEntity.ClassTeacherId = request.ClassTeacherId;
        classEntity.ClassroomId = request.ClassroomId;

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
