using MediatR;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;
using SMS.Domain.Entities;

namespace SMS.Application.Features.Academic.Commands;

public class CreateSubjectCommandHandler : IRequestHandler<CreateSubjectCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public CreateSubjectCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreateSubjectCommand request, CancellationToken cancellationToken)
    {
        var subject = new Subject
        {
            Name = request.Name,
            Code = request.Code,
            Description = request.Description,
            IsCore = request.IsCore,
            IsActive = true
        };

        _context.Subjects.Add(subject);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(subject.Id);
    }
}
