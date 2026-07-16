using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;
using SMS.Domain.Enums;

namespace SMS.Application.Features.Guardians.Commands;

public class UpdateGuardianCommandHandler : IRequestHandler<UpdateGuardianCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public UpdateGuardianCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(UpdateGuardianCommand request, CancellationToken cancellationToken)
    {
        var guardian = await _context.Guardians
            .FirstOrDefaultAsync(g => g.Id == request.Id, cancellationToken);

        if (guardian == null)
        {
            return Result.Failure("Guardian not found");
        }

        guardian.FirstName = request.FirstName;
        guardian.LastName = request.LastName;
        guardian.Gender = Enum.Parse<Gender>(request.Gender);
        guardian.NationalId = request.NationalId;
        guardian.Phone = request.Phone;
        guardian.AlternatePhone = request.AlternatePhone;
        guardian.Email = request.Email;
        guardian.Address = request.Address;
        guardian.Occupation = request.Occupation;
        guardian.Employer = request.Employer;

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
