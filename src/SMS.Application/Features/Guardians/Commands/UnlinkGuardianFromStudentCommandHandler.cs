using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Guardians.Commands;

public class UnlinkGuardianFromStudentCommandHandler : IRequestHandler<UnlinkGuardianFromStudentCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public UnlinkGuardianFromStudentCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(UnlinkGuardianFromStudentCommand request, CancellationToken cancellationToken)
    {
        var link = await _context.StudentGuardians
            .FirstOrDefaultAsync(
                sg => sg.GuardianId == request.GuardianId && sg.StudentId == request.StudentId,
                cancellationToken);

        if (link == null)
        {
            return Result.Failure("Guardian is not linked to this student");
        }

        _context.StudentGuardians.Remove(link);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
