using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Users.Queries;

public record GetUserByIdQuery(Guid Id) : IRequest<Result<UserDetailDto>>;

public record UserDetailDto
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string? ProfilePicture { get; init; }
    public bool IsActive { get; init; }
    public bool EmailConfirmed { get; init; }
    public DateTime? LastLoginAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public List<RoleDto> Roles { get; init; } = [];
    public List<string> Permissions { get; init; } = [];
    
    // Linked entities
    public Guid? StaffId { get; init; }
    public string? StaffNumber { get; init; }
    public Guid? GuardianId { get; init; }
}

public record RoleDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
}
