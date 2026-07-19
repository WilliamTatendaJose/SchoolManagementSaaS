using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Messaging;
using SMS.Application.Features.Notifications.Commands;
using SMS.Application.Interfaces;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using SMS.Infrastructure.Persistence;
using Xunit;
using AcademicTermEntity = SMS.Domain.Entities.AcademicTerm;

namespace SMS.Infrastructure.Tests;

/// <summary>
/// A fake channel that records what it was asked to send and can be told to fail for
/// specific recipients.
/// </summary>
internal sealed class FakeMessageChannel : IMessageChannel
{
    public string Channel { get; init; } = MessageChannels.Sms;
    public List<(string Recipient, string Content)> Sent { get; } = [];
    public Func<string, bool> ShouldSucceed { get; set; } = _ => true;

    public Task<MessageDeliveryResult> SendAsync(string recipient, string content, CancellationToken cancellationToken = default)
    {
        Sent.Add((recipient, content));
        return Task.FromResult(ShouldSucceed(recipient)
            ? MessageDeliveryResult.Ok()
            : MessageDeliveryResult.Fail("simulated failure"));
    }
}

public class NotificationDispatchTests : IAsyncLifetime
{
    private readonly TenantTestContext _harness = new();

    private Guid _aliceId, _bobId, _carolId, _danId, _eveId;

    private const string AlicePhone = "+263771000001";
    private const string BobPhone = "+263771000002";
    private const string DanPhone = "+263771000004";
    private const string EvePhone = "+263771000005";
    private const decimal DanBalance = 300m;

    public async Task InitializeAsync()
    {
        var tenant = new Tenant { Name = "Test School", Code = "TST", Currency = "USD" };
        await using (var db = _harness.CreateDbContext())
        {
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync();
        }
        _harness.UseTenant(tenant.Id);

        await using (var db = _harness.CreateDbContext())
        {
            var user = new User { Email = "admin@test.zw", PasswordHash = "x", FirstName = "Admin", LastName = "User" };
            db.Users.Add(user);

            var year = new AcademicYear { Name = "2026", Year = 2026, StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 12, 31) };
            db.AcademicYears.Add(year);
            await db.SaveChangesAsync();

            var term = new AcademicTermEntity { AcademicYearId = year.Id, Name = "Term 1", TermNumber = 1, StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 4, 30) };
            db.AcademicTerms.Add(term);
            await db.SaveChangesAsync();

            _aliceId = await AddStudentWithGuardian(db, "S-A", "Alice", AlicePhone);
            _bobId = await AddStudentWithGuardian(db, "S-B", "Bob", BobPhone);
            _carolId = await AddStudentWithGuardian(db, "S-C", "Carol", phone: null); // unreachable
            _danId = await AddStudentWithGuardian(db, "S-D", "Dan", DanPhone);
            _eveId = await AddStudentWithGuardian(db, "S-E", "Eve", EvePhone);

            // Dan owes 300; Eve is fully paid.
            db.Invoices.Add(NewInvoice("INV-D", _danId, term.Id, total: DanBalance, paid: 0));
            db.Invoices.Add(NewInvoice("INV-E", _eveId, term.Id, total: 200m, paid: 200m));
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
    public async Task Send_delivers_to_reachable_guardians_and_skips_students_without_a_phone()
    {
        var channel = new FakeMessageChannel();

        MessageDispatchResultDto result;
        await using (var db = _harness.CreateDbContext())
        {
            var handler = new SendMessageCommandHandler(db, [channel], _harness.CurrentUser);
            var outcome = await handler.Handle(new SendMessageCommand
            {
                Channel = MessageChannels.Sms,
                Subject = "Sports day",
                Content = "Sports day is on Friday.",
                Audience = nameof(MessageAudience.SpecificStudents),
                StudentIds = [_aliceId, _bobId, _carolId]
            }, CancellationToken.None);

            outcome.IsSuccess.Should().BeTrue();
            result = outcome.Data!;
        }

        result.TotalRecipients.Should().Be(2, "Carol's guardian has no phone");
        result.Delivered.Should().Be(2);
        result.Failed.Should().Be(0);
        channel.Sent.Select(s => s.Recipient).Should().BeEquivalentTo([AlicePhone, BobPhone]);

        await using var verify = _harness.CreateDbContext();
        var message = await verify.Messages.AsNoTracking().Include(m => m.Recipients).FirstAsync(m => m.Id == result.MessageId);
        message.Status.Should().Be(MessageStatuses.Sent);
        message.Recipients.Should().OnlyContain(r => r.Status == RecipientStatuses.Delivered);
    }

    [Fact]
    public async Task Send_records_per_recipient_failures()
    {
        var channel = new FakeMessageChannel { ShouldSucceed = phone => phone != BobPhone };

        await using var db = _harness.CreateDbContext();
        var handler = new SendMessageCommandHandler(db, [channel], _harness.CurrentUser);
        var outcome = await handler.Handle(new SendMessageCommand
        {
            Channel = MessageChannels.Sms,
            Content = "Hello",
            Audience = nameof(MessageAudience.SpecificStudents),
            StudentIds = [_aliceId, _bobId]
        }, CancellationToken.None);

        outcome.Data!.Delivered.Should().Be(1);
        outcome.Data.Failed.Should().Be(1);

        var message = await db.Messages.AsNoTracking().Include(m => m.Recipients).FirstAsync(m => m.Id == outcome.Data.MessageId);
        message.Recipients.Single(r => r.RecipientPhone == BobPhone).Status.Should().Be(RecipientStatuses.Failed);
        message.Recipients.Single(r => r.RecipientPhone == AlicePhone).Status.Should().Be(RecipientStatuses.Delivered);
    }

    [Fact]
    public async Task Send_fails_for_an_unsupported_channel()
    {
        await using var db = _harness.CreateDbContext();
        var handler = new SendMessageCommandHandler(db, [new FakeMessageChannel()], _harness.CurrentUser);

        var outcome = await handler.Handle(new SendMessageCommand
        {
            Channel = "Email",
            Content = "Hi",
            Audience = nameof(MessageAudience.SpecificStudents),
            StudentIds = [_aliceId]
        }, CancellationToken.None);

        outcome.IsSuccess.Should().BeFalse();
        outcome.Error.Should().Contain("Email");
    }

    [Fact]
    public async Task Fee_reminders_only_target_defaulters_with_a_personalised_balance()
    {
        var channel = new FakeMessageChannel();

        MessageDispatchResultDto result;
        await using (var db = _harness.CreateDbContext())
        {
            var handler = new SendFeeRemindersCommandHandler(db, [channel], _harness.CurrentUser);
            var outcome = await handler.Handle(new SendFeeRemindersCommand { Channel = MessageChannels.Sms }, CancellationToken.None);

            outcome.IsSuccess.Should().BeTrue();
            result = outcome.Data!;
        }

        result.TotalRecipients.Should().Be(1, "only Dan has an outstanding balance");
        result.Delivered.Should().Be(1);
        channel.Sent.Should().ContainSingle();
        channel.Sent[0].Recipient.Should().Be(DanPhone);
        channel.Sent[0].Content.Should().Contain("300.00").And.Contain("Dan");
    }

    private static async Task<Guid> AddStudentWithGuardian(ApplicationDbContext db, string number, string first, string? phone)
    {
        var student = new Student
        {
            StudentNumber = number,
            FirstName = first,
            LastName = "Family",
            DateOfBirth = new DateTime(2012, 1, 1),
            Gender = Gender.Other,
            AdmissionDate = new DateTime(2026, 1, 1),
            Status = StudentStatus.Active
        };
        var guardian = new Guardian { FirstName = first, LastName = "Parent", Gender = Gender.Other, Phone = phone };
        db.Students.Add(student);
        db.Guardians.Add(guardian);
        await db.SaveChangesAsync();

        db.StudentGuardians.Add(new StudentGuardian
        {
            StudentId = student.Id,
            GuardianId = guardian.Id,
            Relationship = "Parent",
            IsPrimaryContact = true
        });
        await db.SaveChangesAsync();

        return student.Id;
    }

    private static Invoice NewInvoice(string number, Guid studentId, Guid termId, decimal total, decimal paid) => new()
    {
        InvoiceNumber = number,
        StudentId = studentId,
        AcademicTermId = termId,
        InvoiceDate = new DateTime(2026, 1, 15),
        DueDate = new DateTime(2026, 2, 15),
        TotalAmount = total,
        DiscountAmount = 0,
        PaidAmount = paid
    };
}
