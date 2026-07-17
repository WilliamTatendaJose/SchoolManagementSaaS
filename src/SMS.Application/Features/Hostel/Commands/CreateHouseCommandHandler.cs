using MediatR;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;
using SMS.Domain.Entities;

namespace SMS.Application.Features.Hostel.Commands;

public class CreateHouseCommandHandler : IRequestHandler<CreateHouseCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public CreateHouseCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreateHouseCommand request, CancellationToken cancellationToken)
    {
        var house = new House
        {
            Name = request.Name,
            Color = request.Color,
            HouseMasterId = request.HouseMasterId
        };

        _context.Houses.Add(house);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(house.Id);
    }
}
