using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Messaging;
using SMS.Application.Features.Notifications.Commands;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using Xunit;

namespace SMS.Infrastructure.Tests;

public class MessageOutboxTests : IAsyncLifetime
{
    private readonly TenantTestContext _harness = new();
    private Guid _studentId;
    private const string GuardianPhone = "+263774000001";

    public async Task InitializeAsync()
    {
        var tenant = new Tenant { Name = "Outbox School", Code = "OBX" };
        await using (var db = _harness.CreateDbContext())
        {
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync();
        }
        _harness.UseTenant(tenant.Id);

        await using (var db = _harness.CreateDbContext())
        {
            var user = new User { Email = "admin@obx.zw", PasswordHash = "x", FirstName = "Admin", LastName = "User" };
            db.Users.Add(user);
            var student = new Student { StudentNumber = "S-OBX", FirstName = "Ob", LastName = "Box", DateOfBirth = new DateTime(2012, 1, 1), Gender = Gender.Other, AdmissionDate = new DateTime(2026, 1, 1), Status = StudentStatus.Active };
            var guardian = new Guardian { FirstName = "G", LastName = "One", Gender = Gender.Other, Phone = GuardianPhone };
            db.Students.Add(student);
            db.Guardians.Add(guardian);
            await db.SaveChangesAsync();
            db.StudentGuardians.Add(new StudentGuardian { StudentId = student.Id, GuardianId = guardian.Id, Relationship = "Parent", IsPrimaryContact = true });
            await db.SaveChangesAsync();

            _studentId = student.Id;
            _harness.CurrentUser.UserId = user.Id;
        }
    }

    public Task DisposeAsync()
    {
        _harness.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task A_future_scheduled_message_is_queued_not_sent_then_dispatched_when_due()
    {
        var channel = new FakeMessageChannel();

        // Schedule for the future -> queued, nothing sent yet.
        Guid messageId;
        await using (var db = _harness.CreateDbContext())
        {
            var outcome = await new SendMessageCommandHandler(db, [channel], _harness.CurrentUser).Handle(new SendMessageCommand
            {
                Channel = MessageChannels.Sms,
                Content = "Reminder: sports day tomorrow.",
                Audience = MessageAudience.SpecificStudents,
                StudentIds = [_studentId],
                ScheduledAt = DateTime.UtcNow.AddHours(2)
            }, CancellationToken.None);
            messageId = outcome.Data!.MessageId;
        }

        channel.Sent.Should().BeEmpty("a future-scheduled message must not send immediately");
        await using (var verify = _harness.CreateDbContext())
        {
            var msg = await verify.Messages.AsNoTracking().Include(m => m.Recipients).FirstAsync(m => m.Id == messageId);
            msg.Status.Should().Be(MessageStatuses.Queued);
            msg.Recipients.Should().OnlyContain(r => r.Status == RecipientStatuses.Pending);
        }

        // Outbox pass while still not due -> nothing dispatched.
        await using (var db = _harness.CreateDbContext())
        {
            var early = await new ProcessMessageOutboxCommandHandler(db, [channel]).Handle(new ProcessMessageOutboxCommand(), CancellationToken.None);
            early.Data.Should().Be(0);
        }
        channel.Sent.Should().BeEmpty();

        // Make it due, then process.
        await using (var db = _harness.CreateDbContext())
        {
            var msg = await db.Messages.FirstAsync(m => m.Id == messageId);
            msg.ScheduledAt = DateTime.UtcNow.AddMinutes(-1);
            await db.SaveChangesAsync();
        }

        await using (var db = _harness.CreateDbContext())
        {
            var processed = await new ProcessMessageOutboxCommandHandler(db, [channel]).Handle(new ProcessMessageOutboxCommand(), CancellationToken.None);
            processed.Data.Should().Be(1);
        }

        channel.Sent.Should().ContainSingle(s => s.Recipient == GuardianPhone);
        await using (var verify = _harness.CreateDbContext())
        {
            var msg = await verify.Messages.AsNoTracking().Include(m => m.Recipients).FirstAsync(m => m.Id == messageId);
            msg.Status.Should().Be(MessageStatuses.Sent);
            msg.DeliveredCount.Should().Be(1);
            msg.Recipients.Should().OnlyContain(r => r.Status == RecipientStatuses.Delivered);
        }
    }

    [Fact]
    public async Task An_immediate_message_still_sends_synchronously()
    {
        var channel = new FakeMessageChannel();

        await using var db = _harness.CreateDbContext();
        var outcome = await new SendMessageCommandHandler(db, [channel], _harness.CurrentUser).Handle(new SendMessageCommand
        {
            Channel = MessageChannels.Sms,
            Content = "Immediate notice.",
            Audience = MessageAudience.SpecificStudents,
            StudentIds = [_studentId]
        }, CancellationToken.None);

        outcome.Data!.Delivered.Should().Be(1);
        channel.Sent.Should().ContainSingle();
    }
}
