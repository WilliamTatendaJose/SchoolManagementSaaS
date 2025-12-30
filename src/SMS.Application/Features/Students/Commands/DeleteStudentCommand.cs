using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Students.Commands;

public record DeleteStudentCommand(Guid Id) : IRequest<Result>;
