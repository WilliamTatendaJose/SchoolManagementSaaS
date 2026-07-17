using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Boarding.Commands;

/// <summary>
/// Records a boarding student's request to leave campus for a weekend. Starts in Pending.
/// </summary>
public record RequestWeekendLeaveCommand : IRequest<Result<Guid>>
{
    public Guid StudentId { get; init; }
    public DateTime DepartureDate { get; init; }
    public DateTime ExpectedReturnDate { get; init; }
    public string Destination { get; init; } = string.Empty;
    public string? Reason { get; init; }
}
