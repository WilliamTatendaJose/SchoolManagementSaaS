using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Messaging;
using SMS.Application.Features.Discipline.Commands;
using SMS.Application.Features.Discipline.Queries;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using Xunit;

namespace SMS.Infrastructure.Tests;

public class DisciplineTests : IAsyncLifetime
{
    private readonly TenantTestContext _harness = new();
    private Guid _studentWithGuardianId;
    private const string GuardianPhone = "+263772000001";

    public async Task InitializeAsync()
    {
        var tenant = new Tenant { Name = "Discipline School", Code = "DSC" };
        await using (var db = _harness.CreateDbContext())
        {
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync();
        }
        _harness.UseTenant(tenant.Id);

        await using (var db = _harness.CreateDbContext())
        {
            var user = new User { Email = "head@dsc.zw", PasswordHash = "x", FirstName = "Head", LastName = "Teacher" };
            db.Users.Add(user);
            var student = new Student
            {
                StudentNumber = "S-DSC-1",
                FirstName = "Dee",
                LastName = "Student",
                DateOfBirth = new DateTime(2012, 1, 1),
                Gender = Gender.Other,
                AdmissionDate = new DateTime(2026, 1, 1),
                Status = StudentStatus.Active
            };
            var guardian = new Guardian { FirstName = "Gee", LastName = "Parent", Gender = Gender.Other, Phone = GuardianPhone };
            db.Students.Add(student);
            db.Guardians.Add(guardian);
            await db.SaveChangesAsync();

            db.StudentGuardians.Add(new StudentGuardian { StudentId = student.Id, GuardianId = guardian.Id, Relationship = "Parent", IsPrimaryContact = true });
            await db.SaveChangesAsync();

            _studentWithGuardianId = student.Id;
            _harness.CurrentUser.UserId = user.Id;
        }
    }

    public Task DisposeAsync()
    {
        _harness.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Creating_a_record_with_notify_sends_the_guardian_an_sms_and_marks_notified()
    {
        var channel = new FakeMessageChannel();

        Guid recordId;
        await using (var db = _harness.CreateDbContext())
        {
            var handler = new CreateDisciplineRecordCommandHandler(db, [channel], _harness.CurrentUser);
            var outcome = await handler.Handle(new CreateDisciplineRecordCommand
            {
                StudentId = _studentWithGuardianId,
                IncidentDate = new DateTime(2026, 3, 10),
                IncidentType = "Late",
                Description = "Arrived late three times this week.",
                DemeritsAwarded = 2,
                NotifyGuardian = true
            }, CancellationToken.None);

            outcome.IsSuccess.Should().BeTrue();
            recordId = outcome.Data;
        }

        channel.Sent.Should().ContainSingle();
        channel.Sent[0].Recipient.Should().Be(GuardianPhone);
        channel.Sent[0].Content.Should().Contain("Dee").And.Contain("Late");

        await using var verify = _harness.CreateDbContext();
        var record = await verify.DisciplineRecords.AsNoTracking().FirstAsync(d => d.Id == recordId);
        record.GuardianNotified.Should().BeTrue();
        record.NotificationDate.Should().NotBeNull();

        var message = await verify.Messages.AsNoTracking().FirstAsync();
        message.MessageType.Should().Be(MessageTypes.Discipline);
    }

    [Fact]
    public async Task Creating_a_record_with_notify_over_whatsapp_uses_the_template_path()
    {
        var channel = new RecordingTemplateChannel();

        await using var db = _harness.CreateDbContext();
        var handler = new CreateDisciplineRecordCommandHandler(db, [channel], _harness.CurrentUser);
        var outcome = await handler.Handle(new CreateDisciplineRecordCommand
        {
            StudentId = _studentWithGuardianId,
            IncidentDate = new DateTime(2026, 3, 10),
            IncidentType = "Late",
            Description = "Arrived late three times this week.",
            DemeritsAwarded = 2,
            NotifyGuardian = true,
            Channel = MessageChannels.WhatsApp
        }, CancellationToken.None);

        outcome.IsSuccess.Should().BeTrue();
        channel.FreeformSends.Should().BeEmpty("a WhatsApp-channel discipline notice must route through the template path");
        channel.TemplateSends.Should().ContainSingle();

        var send = channel.TemplateSends[0];
        send.Recipient.Should().Be(GuardianPhone);
        send.TemplateKey.Should().Be(MessageTypes.Discipline);
        send.Parameters.Should().Equal("Dee Student", "Late", "10 Mar 2026");
    }

    [Fact]
    public async Task Creating_a_record_without_notify_does_not_message()
    {
        var channel = new FakeMessageChannel();

        await using (var db = _harness.CreateDbContext())
        {
            var handler = new CreateDisciplineRecordCommandHandler(db, [channel], _harness.CurrentUser);
            await handler.Handle(new CreateDisciplineRecordCommand
            {
                StudentId = _studentWithGuardianId,
                IncidentDate = new DateTime(2026, 3, 10),
                IncidentType = "Merit",
                Description = "Helped a classmate.",
                MeritsAwarded = 1,
                NotifyGuardian = false
            }, CancellationToken.None);
        }

        channel.Sent.Should().BeEmpty();

        await using var verify = _harness.CreateDbContext();
        (await verify.Messages.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Records_can_be_listed_for_a_student()
    {
        await using (var db = _harness.CreateDbContext())
        {
            var handler = new CreateDisciplineRecordCommandHandler(db, [new FakeMessageChannel()], _harness.CurrentUser);
            await handler.Handle(new CreateDisciplineRecordCommand
            {
                StudentId = _studentWithGuardianId,
                IncidentDate = new DateTime(2026, 3, 10),
                IncidentType = "Late",
                Description = "Late."
            }, CancellationToken.None);
        }

        await using var db2 = _harness.CreateDbContext();
        var result = await new GetDisciplineRecordsQueryHandler(db2)
            .Handle(new GetDisciplineRecordsQuery { StudentId = _studentWithGuardianId }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().ContainSingle();
        result.Data.Items[0].StudentName.Should().Contain("Dee");
    }
}
