using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Lms.Commands;

public record DeleteCourseMaterialCommand(Guid Id) : IRequest<Result<bool>>;
