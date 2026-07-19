using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Messaging;
using SMS.Application.Features.Lms.Commands;
using SMS.Application.Features.Notifications.Commands;
using SMS.Application.Interfaces;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using Xunit;
using AcademicTermEntity = SMS.Domain.Entities.AcademicTerm;

namespace SMS.Infrastructure.Tests;

/// <summary>
/// A channel that records SendAsync and SendTemplateAsync calls separately, so a test can
/// assert which path a dispatch actually took (a bug that routes through SendAsync when it
/// should use SendTemplateAsync would otherwise pass silently, since both return success).
/// </summary>
internal sealed class RecordingTemplateChannel : IMessageChannel
{
    public string Channel => MessageChannels.WhatsApp;
    public bool SupportsDeliveryReceipts => true;

    public List<(string Recipient, string Content)> FreeformSends { get; } = [];
    public List<(string Recipient, string TemplateKey, IReadOnlyList<string> Parameters, string Fallback)> TemplateSends { get; } = [];

    public Task<MessageDeliveryResult> SendAsync(string recipient, string content, CancellationToken cancellationToken = default)
    {
        FreeformSends.Add((recipient, content));
        return Task.FromResult(MessageDeliveryResult.Ok("wamid.FREEFORM"));
    }

    public Task<MessageDeliveryResult> SendTemplateAsync(
        string recipient, string templateKey, IReadOnlyList<string> parameters, string fallbackContent, CancellationToken cancellationToken = default)
    {
        TemplateSends.Add((recipient, templateKey, parameters, fallbackContent));
        return Task.FromResult(MessageDeliveryResult.Ok("wamid.TEMPLATE"));
    }
}

/// <summary>
/// Verifies the business-initiated notification handlers (fee reminders, assignment
/// reminders) route WhatsApp sends through SendTemplateAsync - not the freeform path -
/// with the message type as the template key and the documented ordered parameters.
/// </summary>
public class WhatsAppTemplateDispatchTests : IAsyncLifetime
{
    private readonly TenantTestContext _harness = new();

    public Task DisposeAsync()
    {
        _harness.Dispose();
        return Task.CompletedTask;
    }

    public async Task InitializeAsync()
    {
        var tenant = new Tenant { Name = "Template School", Code = "TPL", Currency = "USD" };
        await using var db = _harness.CreateDbContext();
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();
        _harness.UseTenant(tenant.Id);
    }

    [Fact]
    public async Task Fee_reminders_over_whatsapp_use_the_template_path_with_name_and_balance()
    {
        var channel = new RecordingTemplateChannel();
        Guid studentId, guardianUserId;

        await using (var db = _harness.CreateDbContext())
        {
            var user = new User { Email = "admin@tpl.zw", PasswordHash = "x", FirstName = "Ad", LastName = "Min" };
            var year = new AcademicYear { Name = "2026", Year = 2026, StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 12, 31) };
            db.Users.Add(user);
            db.AcademicYears.Add(year);
            await db.SaveChangesAsync();

            var term = new AcademicTermEntity { AcademicYearId = year.Id, Name = "Term 1", TermNumber = 1, StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 4, 30) };
            db.AcademicTerms.Add(term);

            var student = new Student
            {
                StudentNumber = "S-TPL-1", FirstName = "Tino", LastName = "Moyo",
                DateOfBirth = new DateTime(2012, 1, 1), Gender = Gender.Male,
                AdmissionDate = new DateTime(2026, 1, 1), Status = StudentStatus.Active
            };
            var guardian = new Guardian { FirstName = "Grace", LastName = "Moyo", Gender = Gender.Female, Phone = "+263772000111" };
            db.Students.Add(student);
            db.Guardians.Add(guardian);
            await db.SaveChangesAsync();

            db.StudentGuardians.Add(new StudentGuardian { StudentId = student.Id, GuardianId = guardian.Id, Relationship = "Mother", IsPrimaryContact = true });
            db.Invoices.Add(new Invoice
            {
                InvoiceNumber = "INV-TPL-1", StudentId = student.Id, AcademicTermId = term.Id,
                InvoiceDate = new DateTime(2026, 1, 15), DueDate = new DateTime(2026, 2, 15),
                TotalAmount = 500m, DiscountAmount = 0, PaidAmount = 350m
            });
            await db.SaveChangesAsync();

            studentId = student.Id;
            guardianUserId = user.Id;
            _harness.CurrentUser.UserId = user.Id;
        }

        await using var handlerDb = _harness.CreateDbContext();
        var outcome = await new SendFeeRemindersCommandHandler(handlerDb, [channel], _harness.CurrentUser)
            .Handle(new SendFeeRemindersCommand { Channel = MessageChannels.WhatsApp }, CancellationToken.None);

        outcome.IsSuccess.Should().BeTrue();
        channel.FreeformSends.Should().BeEmpty("a fee reminder must never fall back to freeform text silently");
        channel.TemplateSends.Should().ContainSingle();

        var send = channel.TemplateSends[0];
        send.TemplateKey.Should().Be(MessageTypes.FeeReminder);
        send.Parameters.Should().HaveCount(2);
        send.Parameters[0].Should().Be("Tino Moyo");
        send.Parameters[1].Should().Be("USD 150.00");
        _ = (studentId, guardianUserId);
    }

    [Fact]
    public async Task Assignment_reminders_over_whatsapp_use_the_template_path_with_subject_title_and_due_date()
    {
        var channel = new RecordingTemplateChannel();

        Guid assignmentId;
        await using (var db = _harness.CreateDbContext())
        {
            var user = new User { Email = "teacher@tpl.zw", PasswordHash = "x", FirstName = "Tea", LastName = "Cher" };
            var cls = new Class { Name = "Form 2", Level = 2, Capacity = 40 };
            var subject = new Subject { Name = "English", Code = "ENG" };
            var year = new AcademicYear { Name = "2026", Year = 2026, StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 12, 31) };
            db.Users.Add(user);
            db.Classes.Add(cls);
            db.Subjects.Add(subject);
            db.AcademicYears.Add(year);
            await db.SaveChangesAsync();

            var term = new AcademicTermEntity { AcademicYearId = year.Id, Name = "Term 1", TermNumber = 1, StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 4, 30) };
            db.AcademicTerms.Add(term);

            var student = new Student
            {
                StudentNumber = "S-TPL-2", FirstName = "Nina", LastName = "Ncube",
                DateOfBirth = new DateTime(2012, 1, 1), Gender = Gender.Female,
                AdmissionDate = new DateTime(2026, 1, 1), Status = StudentStatus.Active, CurrentClassId = cls.Id
            };
            var guardian = new Guardian { FirstName = "Nomsa", LastName = "Ncube", Gender = Gender.Female, Phone = "+263772000222" };
            db.Students.Add(student);
            db.Guardians.Add(guardian);
            await db.SaveChangesAsync();

            db.StudentGuardians.Add(new StudentGuardian { StudentId = student.Id, GuardianId = guardian.Id, Relationship = "Mother", IsPrimaryContact = true });

            var assignment = new Assignment
            {
                ClassId = cls.Id, SubjectId = subject.Id, AcademicTermId = term.Id,
                Title = "Essay: My Holiday", DueDate = new DateTime(2026, 3, 1)
            };
            db.Assignments.Add(assignment);
            await db.SaveChangesAsync();

            assignmentId = assignment.Id;
            _harness.CurrentUser.UserId = user.Id;
        }

        await using var handlerDb = _harness.CreateDbContext();
        var outcome = await new RemindNonSubmittersCommandHandler(handlerDb, [channel], _harness.CurrentUser)
            .Handle(new RemindNonSubmittersCommand { AssignmentId = assignmentId, Channel = MessageChannels.WhatsApp }, CancellationToken.None);

        outcome.IsSuccess.Should().BeTrue();
        channel.FreeformSends.Should().BeEmpty();
        channel.TemplateSends.Should().ContainSingle();

        var send = channel.TemplateSends[0];
        send.TemplateKey.Should().Be(MessageTypes.AssignmentReminder);
        send.Parameters.Should().Equal("Nina Ncube", "English", "Essay: My Holiday", "01 Mar 2026");
    }
}
