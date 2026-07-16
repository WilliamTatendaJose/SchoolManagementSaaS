using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Guardians.Commands;

public record DeleteGuardianCommand(Guid Id) : IRequest<Result>;
