using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Fees.Commands;

public record DeleteFeeStructureCommand(Guid Id) : IRequest<Result>;
