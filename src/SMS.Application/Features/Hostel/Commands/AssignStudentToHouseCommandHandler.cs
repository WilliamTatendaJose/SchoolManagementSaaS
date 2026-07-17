using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Hostel.Commands;

public class AssignStudentToHouseCommandHandler : IRequestHandler<AssignStudentToHouseCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public AssignStudentToHouseCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(AssignStudentToHouseCommand request, CancellationToken cancellationToken)
    {
        var student = await _context.Students.FirstOrDefaultAsync(s => s.Id == request.StudentId, cancellationToken);
        if (student == null)
        {
            return Result.Failure("Student not found");
        }

        if (request.HouseId is not null
            && !await _context.Houses.AnyAsync(h => h.Id == request.HouseId.Value, cancellationToken))
        {
            return Result.Failure("House not found");
        }

        student.HouseId = request.HouseId;
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
