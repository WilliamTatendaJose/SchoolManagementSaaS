using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Academic.Commands;

public record DeleteAcademicYearCommand(Guid Id) : IRequest<Result>;
