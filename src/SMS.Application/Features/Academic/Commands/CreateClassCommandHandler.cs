using MediatR;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;
using SMS.Domain.Entities;

namespace SMS.Application.Features.Academic.Commands;

public class CreateClassCommandHandler : IRequestHandler<CreateClassCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public CreateClassCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreateClassCommand request, CancellationToken cancellationToken)
    {
        var classEntity = new Class
        {
            Name = request.Name,
            Code = request.Code,
            Level = request.Level,
            Capacity = request.Capacity,
            ClassTeacherId = request.ClassTeacherId,
            ClassroomId = request.ClassroomId
        };

        _context.Classes.Add(classEntity);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(classEntity.Id);
    }
}
