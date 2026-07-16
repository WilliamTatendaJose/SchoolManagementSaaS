using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Guardians.Commands;

public class DeleteGuardianCommandHandler : IRequestHandler<DeleteGuardianCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public DeleteGuardianCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(DeleteGuardianCommand request, CancellationToken cancellationToken)
    {
        var guardian = await _context.Guardians
            .FirstOrDefaultAsync(g => g.Id == request.Id, cancellationToken);

        if (guardian == null)
        {
            return Result.Failure("Guardian not found");
        }

        // Remove student links first so no dangling associations remain
        var links = await _context.StudentGuardians
            .Where(sg => sg.GuardianId == request.Id)
            .ToListAsync(cancellationToken);

        if (links.Count > 0)
        {
            _context.StudentGuardians.RemoveRange(links);
        }

        _context.Guardians.Remove(guardian);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
