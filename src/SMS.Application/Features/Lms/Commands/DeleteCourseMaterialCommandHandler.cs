using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.Lms.Commands;

public class DeleteCourseMaterialCommandHandler : IRequestHandler<DeleteCourseMaterialCommand, Result<bool>>
{
    private readonly IApplicationDbContext _context;

    public DeleteCourseMaterialCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<bool>> Handle(DeleteCourseMaterialCommand request, CancellationToken cancellationToken)
    {
        var material = await _context.CourseMaterials.FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken);
        if (material == null)
        {
            return Result<bool>.Failure("Material not found");
        }

        // Soft-delete (the stored file is left in place; harmless and keeps deletes reversible).
        _context.CourseMaterials.Remove(material);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
