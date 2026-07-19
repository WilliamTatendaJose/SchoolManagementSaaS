using MediatR;
using SMS.Application.Common.Models;
using SMS.Application.Features.Academic.Queries;
using SMS.Application.Interfaces;

namespace SMS.Application.Features.ParentPortal.Queries;

public class GetMyChildReportCardQueryHandler : IRequestHandler<GetMyChildReportCardQuery, Result<ReportCardFileDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ISender _sender;

    public GetMyChildReportCardQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser, ISender sender)
    {
        _context = context;
        _currentUser = currentUser;
        _sender = sender;
    }

    public async Task<Result<ReportCardFileDto>> Handle(GetMyChildReportCardQuery request, CancellationToken cancellationToken)
    {
        if (!await ParentChildAccess.OwnsStudentAsync(_context, _currentUser.UserId, request.StudentId, cancellationToken))
        {
            return Result<ReportCardFileDto>.Failure("Student not found");
        }

        return await _sender.Send(new GenerateReportCardQuery
        {
            StudentId = request.StudentId,
            AcademicTermId = request.AcademicTermId,
            GradingScheme = request.GradingScheme
        }, cancellationToken);
    }
}
