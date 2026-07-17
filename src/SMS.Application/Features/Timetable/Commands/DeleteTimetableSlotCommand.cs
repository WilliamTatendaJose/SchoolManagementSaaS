using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Timetable.Commands;

public record DeleteTimetableSlotCommand(Guid Id) : IRequest<Result>;
