using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Staff.Commands;

public record DeleteStaffCommand(Guid Id) : IRequest<Result>;
