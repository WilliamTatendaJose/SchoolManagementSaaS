using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Messaging;
using SMS.Application.Features.Notifications.Commands;
using SMS.Application.Interfaces;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using Xunit;

namespace SMS.Infrastructure.Tests;

/// <summary>
/// A WhatsApp-like channel: it confirms delivery asynchronously (receipts) and returns a
/// provider message id (wamid) for every accepted send.
/// </summary>
internal sealed class FakeWhatsAppChannel : IMessageChannel
{
    private int _counter;
    public string Channel => MessageChannels.WhatsApp;
    public bool SupportsDeliveryReceipts => true;
    public List<(string Recipient, string Wamid)> Sent { get; } = [];

    public Task<MessageDeliveryResult> SendAsync(string recipient, string content, CancellationToken cancellationToken = default)
    {
        var wamid = $"wamid.TEST{++_counter:D4}";
        Sent.Add((recipient, wamid));
        return Task.FromResult(MessageDeliveryResult.Ok(wamid));
    }
}

/// <summary>
/// Verifies the delivery-status lifecycle: a receipt-capable channel marks sends as "Sent"
/// (not "Delivered"), and the webhook-processing command advances recipients through
/// Delivered/Read/Failed, guards against out-of-order regressions, and rolls up the
/// parent message's counts - matched cross-tenant by the wamid.
/// </summary>
public class WhatsAppDeliveryTrackingTests : IAsyncLifetime
{
    private readonly TenantTestContext _harness = new();
    private Guid _tenantId;
    private Guid _userId;
    private Guid _studentId;

    private const string GuardianPhone = "+263771000111";

    public async Task InitializeAsync()
    {
        var tenant = new Tenant { Name = "WA School", Code = "WAS" };
        await using (var db = _harness.CreateDbContext())
        {
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync();
        }
        _tenantId = tenant.Id;
        _harness.UseTenant(tenant.Id);

        await using (var db = _harness.CreateDbContext())
        {
            var user = new User { Email = "admin@was.zw", PasswordHash = "x", FirstName = "Ad", LastName = "Min" };
            var student = new Student
            {
                StudentNumber = "S-WA-1", FirstName = "Wanda", LastName = "Learner",
                DateOfBirth = new DateTime(2012, 1, 1), Gender = Gender.Female,
                AdmissionDate = new DateTime(2026, 1, 1), Status = StudentStatus.Active
            };
            var guardian = new Guardian { FirstName = "Gigi", LastName = "Guardian", Gender = Gender.Female, Phone = GuardianPhone };
            db.Users.Add(user);
            db.Students.Add(student);
            db.Guardians.Add(guardian);
            await db.SaveChangesAsync();

            db.StudentGuardians.Add(new StudentGuardian { StudentId = student.Id, GuardianId = guardian.Id, Relationship = "Parent", IsPrimaryContact = true });
            await db.SaveChangesAsync();

            _userId = user.Id;
            _studentId = student.Id;
            _harness.CurrentUser.UserId = user.Id;
        }
    }

    public Task DisposeAsync()
    {
        _harness.Dispose();
        return Task.CompletedTask;
    }

    private async Task<(Guid MessageId, string Wamid)> SeedSentMessageAsync()
    {
        const string wamid = "wamid.HBgTEST123";
        await using var db = _harness.CreateDbContext();
        var message = new Message
        {
            Subject = "Test", Content = "Hello", MessageType = MessageTypes.Announcement,
            Channel = MessageChannels.WhatsApp, CreatedByUserId = _userId,
            Status = MessageStatuses.Sent, TotalRecipients = 1
        };
        message.Recipients.Add(new MessageRecipient
        {
            RecipientPhone = GuardianPhone, RecipientName = "Gigi Guardian",
            StudentId = _studentId, Status = RecipientStatuses.Sent, ProviderMessageId = wamid
        });
        db.Messages.Add(message);
        await db.SaveChangesAsync();
        return (message.Id, wamid);
    }

    private async Task<int> ProcessAsync(params WhatsAppStatusUpdate[] updates)
    {
        await using var db = _harness.CreateDbContext();
        var result = await new ProcessWhatsAppStatusUpdatesCommandHandler(db)
            .Handle(new ProcessWhatsAppStatusUpdatesCommand(updates), CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
        return result.Data;
    }

    [Fact]
    public async Task Dispatch_over_a_receipt_channel_marks_recipients_Sent_not_Delivered()
    {
        var channel = new FakeWhatsAppChannel();

        MessageDispatchResultDto dto;
        await using (var db = _harness.CreateDbContext())
        {
            var handler = new SendMessageCommandHandler(db, [channel], _harness.CurrentUser);
            var outcome = await handler.Handle(new SendMessageCommand
            {
                Channel = MessageChannels.WhatsApp,
                Subject = "Notice",
                Content = "Term starts Monday",
                Audience = nameof(MessageAudience.SpecificStudents),
                StudentIds = [_studentId]
            }, CancellationToken.None);

            outcome.IsSuccess.Should().BeTrue();
            dto = outcome.Data!;
        }

        // The message was accepted by WhatsApp but not yet confirmed delivered.
        dto.TotalRecipients.Should().Be(1);
        dto.Delivered.Should().Be(0);

        channel.Sent.Should().ContainSingle();

        await using var verify = _harness.CreateDbContext();
        var saved = await verify.Messages.Include(m => m.Recipients).AsNoTracking().FirstAsync(m => m.Id == dto.MessageId);
        var recipient = saved.Recipients.Single();
        recipient.Status.Should().Be(RecipientStatuses.Sent);
        recipient.ProviderMessageId.Should().StartWith("wamid.");
        recipient.DeliveredAt.Should().BeNull();
        saved.DeliveredCount.Should().Be(0);
    }

    [Fact]
    public async Task Webhook_advances_a_recipient_from_Sent_to_Delivered_to_Read_and_rolls_up_counts()
    {
        var (messageId, wamid) = await SeedSentMessageAsync();

        (await ProcessAsync(new WhatsAppStatusUpdate { ProviderMessageId = wamid, Status = "delivered", Timestamp = new DateTime(2026, 3, 1, 8, 0, 0) }))
            .Should().Be(1);

        await using (var db = _harness.CreateDbContext())
        {
            var msg = await db.Messages.Include(m => m.Recipients).AsNoTracking().FirstAsync(m => m.Id == messageId);
            var r = msg.Recipients.Single();
            r.Status.Should().Be(RecipientStatuses.Delivered);
            r.DeliveredAt.Should().NotBeNull();
            msg.DeliveredCount.Should().Be(1);
        }

        (await ProcessAsync(new WhatsAppStatusUpdate { ProviderMessageId = wamid, Status = "read" })).Should().Be(1);

        await using var verify = _harness.CreateDbContext();
        var read = await verify.Messages.Include(m => m.Recipients).AsNoTracking().FirstAsync(m => m.Id == messageId);
        read.Recipients.Single().Status.Should().Be(RecipientStatuses.Read);
        read.Recipients.Single().ReadAt.Should().NotBeNull();
        read.DeliveredCount.Should().Be(1, "a read message is still counted as delivered");
    }

    [Fact]
    public async Task Webhook_never_regresses_a_recipient_on_out_of_order_events()
    {
        var (messageId, wamid) = await SeedSentMessageAsync();

        await ProcessAsync(new WhatsAppStatusUpdate { ProviderMessageId = wamid, Status = "read" });
        // A late "delivered" arriving after "read" must not downgrade.
        var applied = await ProcessAsync(new WhatsAppStatusUpdate { ProviderMessageId = wamid, Status = "delivered" });
        applied.Should().Be(0);

        await using var verify = _harness.CreateDbContext();
        var msg = await verify.Messages.Include(m => m.Recipients).AsNoTracking().FirstAsync(m => m.Id == messageId);
        msg.Recipients.Single().Status.Should().Be(RecipientStatuses.Read);
    }

    [Fact]
    public async Task Webhook_marks_a_recipient_Failed_and_records_the_error()
    {
        var (messageId, wamid) = await SeedSentMessageAsync();

        (await ProcessAsync(new WhatsAppStatusUpdate { ProviderMessageId = wamid, Status = "failed", ErrorTitle = "Message undeliverable" }))
            .Should().Be(1);

        await using var verify = _harness.CreateDbContext();
        var msg = await verify.Messages.Include(m => m.Recipients).AsNoTracking().FirstAsync(m => m.Id == messageId);
        var r = msg.Recipients.Single();
        r.Status.Should().Be(RecipientStatuses.Failed);
        r.FailureReason.Should().Be("Message undeliverable");
        msg.FailedCount.Should().Be(1);
        msg.DeliveredCount.Should().Be(0);
    }

    [Fact]
    public async Task Webhook_ignores_an_unknown_wamid()
    {
        await SeedSentMessageAsync();
        (await ProcessAsync(new WhatsAppStatusUpdate { ProviderMessageId = "wamid.UNKNOWN", Status = "delivered" }))
            .Should().Be(0);
    }

    [Fact]
    public async Task Webhook_matches_the_recipient_across_tenants_by_wamid()
    {
        var (messageId, wamid) = await SeedSentMessageAsync();

        // Simulate the unauthenticated webhook: a different tenant is active, so the normal
        // tenant query filter would hide the recipient. The handler must still find it by
        // the globally-unique wamid.
        _harness.UseTenant(Guid.NewGuid());
        var applied = await ProcessAsync(new WhatsAppStatusUpdate { ProviderMessageId = wamid, Status = "delivered" });
        applied.Should().Be(1);

        _harness.UseTenant(_tenantId);
        await using var verify = _harness.CreateDbContext();
        var msg = await verify.Messages.Include(m => m.Recipients).AsNoTracking().FirstAsync(m => m.Id == messageId);
        msg.Recipients.Single().Status.Should().Be(RecipientStatuses.Delivered);
    }
}
