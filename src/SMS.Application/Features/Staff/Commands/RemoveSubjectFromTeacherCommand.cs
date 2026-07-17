using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Staff.Commands;

public record RemoveSubjectFromTeacherCommand(Guid StaffId, Guid SubjectId) : IRequest<Result>;
