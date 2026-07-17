using MediatR;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;
using SMS.Domain.Entities;

namespace SMS.Application.Features.Hostel.Commands;

public class CreateDormitoryCommandHandler : IRequestHandler<CreateDormitoryCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public CreateDormitoryCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreateDormitoryCommand request, CancellationToken cancellationToken)
    {
        var dormitory = new Dormitory
        {
            Name = request.Name,
            Capacity = request.Capacity,
            Gender = request.Gender,
            WardenId = request.WardenId,
            IsActive = true
        };

        _context.Dormitories.Add(dormitory);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(dormitory.Id);
    }
}
