using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Behaviors;
using SMS.Application.Common.Models;
using SMS.Domain.Entities;
using Xunit;

namespace SMS.Infrastructure.Tests;

public class AuditBehaviorTests : IAsyncLifetime
{
    private readonly TenantTestContext _harness = new();

    // Request shapes named to exercise the Command convention and redaction.
    internal sealed record CreateWidgetCommand
    {
        public string Name { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
    }

    internal sealed record GetWidgetsQuery
    {
        public string Filter { get; init; } = string.Empty;
    }

    public async Task InitializeAsync()
    {
        var tenant = new Tenant { Name = "Audit School", Code = "AUD" };
        await using (var db = _harness.CreateDbContext())
        {
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync();
        }
        _harness.UseTenant(tenant.Id);

        await using (var db = _harness.CreateDbContext())
        {
            var user = new User { Email = "admin@audit.zw", PasswordHash = "x", FirstName = "Admin", LastName = "User" };
            db.Users.Add(user);
            await db.SaveChangesAsync();
            _harness.CurrentUser.UserId = user.Id;
        }
    }

    public Task DisposeAsync()
    {
        _harness.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Successful_command_writes_a_redacted_audit_log()
    {
        await using var db = _harness.CreateDbContext();
        var behavior = new AuditBehavior<CreateWidgetCommand, Result<Guid>>(db, _harness.CurrentUser);

        await behavior.Handle(
            new CreateWidgetCommand { Name = "Chalk", Password = "super-secret" },
            _ => Task.FromResult(Result<Guid>.Success(Guid.NewGuid())),
            CancellationToken.None);

        var log = await db.AuditLogs.AsNoTracking().SingleAsync();
        log.Action.Should().Be("Create");
        log.EntityType.Should().Be("Widget");
        log.UserId.Should().Be(_harness.CurrentUser.UserId);
        log.NewValues.Should().Contain("Chalk");
        log.NewValues.Should().Contain("***").And.NotContain("super-secret", "password fields must be redacted");
    }

    [Fact]
    public async Task Failed_command_is_not_audited()
    {
        await using var db = _harness.CreateDbContext();
        var behavior = new AuditBehavior<CreateWidgetCommand, Result<Guid>>(db, _harness.CurrentUser);

        await behavior.Handle(
            new CreateWidgetCommand { Name = "X" },
            _ => Task.FromResult(Result<Guid>.Failure("rejected")),
            CancellationToken.None);

        (await db.AuditLogs.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Query_requests_are_not_audited()
    {
        await using var db = _harness.CreateDbContext();
        var behavior = new AuditBehavior<GetWidgetsQuery, Result<Guid>>(db, _harness.CurrentUser);

        await behavior.Handle(
            new GetWidgetsQuery { Filter = "all" },
            _ => Task.FromResult(Result<Guid>.Success(Guid.NewGuid())),
            CancellationToken.None);

        (await db.AuditLogs.CountAsync()).Should().Be(0);
    }
}
