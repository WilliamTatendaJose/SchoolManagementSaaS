using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SMS.Application.Features.Lms;
using SMS.Application.Features.Lms.Commands;
using SMS.Application.Features.Lms.Queries;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using Xunit;
using AcademicTermEntity = SMS.Domain.Entities.AcademicTerm;

namespace SMS.Infrastructure.Tests;

/// <summary>
/// Covers the assignment roster view (submitters + non-submitters + progress), the
/// mark-sheet bulk-grade workflow, and the reminder-to-non-submitters dispatch.
/// </summary>
public class AssignmentRosterTests : IAsyncLifetime
{
    private readonly TenantTestContext _harness = new();

    private Guid _assignmentId;
    private Guid _classId;
    private Guid _subjectId;
    private Guid _termId;
    private Guid _submitterId;      // has a submission
    private Guid _nonSubmitterId;   // in the class, no submission, has a guardian
    private Guid _paperStudentId;   // in the class, no submission, no guardian
    private Guid _otherClassId;

    private const string GuardianPhone = "+263772000900";

    private static IServiceProvider NoFileStorage() =>
        new ServiceCollection().BuildServiceProvider();

    public async Task InitializeAsync()
    {
        var tenant = new Tenant { Name = "Roster School", Code = "RST" };
        await using (var db = _harness.CreateDbContext())
        {
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync();
        }
        _harness.UseTenant(tenant.Id);

        await using (var db = _harness.CreateDbContext())
        {
            var user = new User { Email = "teacher@rst.zw", PasswordHash = "x", FirstName = "Tea", LastName = "Cher" };
            db.Users.Add(user);

            var cls = new Class { Name = "Form 2", Level = 2, Capacity = 40 };
            var otherCls = new Class { Name = "Form 3", Level = 3, Capacity = 40 };
            var subject = new Subject { Name = "Mathematics", Code = "MATH" };
            var year = new AcademicYear { Name = "2026", Year = 2026, StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 12, 31) };
            db.Classes.AddRange(cls, otherCls);
            db.Subjects.Add(subject);
            db.AcademicYears.Add(year);
            await db.SaveChangesAsync();

            var term = new AcademicTermEntity { AcademicYearId = year.Id, Name = "Term 1", TermNumber = 1, StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 4, 30) };
            db.AcademicTerms.Add(term);

            var submitter = NewStudent("S-SUB", "Sam", "Submitter", cls.Id);
            var nonSubmitter = NewStudent("S-NON", "Nina", "NonSubmitter", cls.Id);
            var paperStudent = NewStudent("S-PAP", "Pete", "Paper", cls.Id);
            var otherClassStudent = NewStudent("S-OTH", "Otto", "Other", otherCls.Id);
            db.Students.AddRange(submitter, nonSubmitter, paperStudent, otherClassStudent);

            var guardian = new Guardian { FirstName = "Gina", LastName = "Guardian", Gender = Gender.Female, Phone = GuardianPhone };
            db.Guardians.Add(guardian);
            await db.SaveChangesAsync();

            // Only the non-submitter has a contactable guardian.
            db.StudentGuardians.Add(new StudentGuardian { StudentId = nonSubmitter.Id, GuardianId = guardian.Id, Relationship = "Parent", IsPrimaryContact = true });

            var assignment = new Assignment
            {
                ClassId = cls.Id,
                SubjectId = subject.Id,
                AcademicTermId = term.Id,
                Title = "Algebra worksheet",
                DueDate = new DateTime(2026, 2, 20)
            };
            db.Assignments.Add(assignment);
            await db.SaveChangesAsync();

            db.AssignmentSubmissions.Add(new AssignmentSubmission
            {
                AssignmentId = assignment.Id,
                StudentId = submitter.Id,
                SubmittedAt = new DateTime(2026, 2, 18),
                Status = SubmissionStatuses.Submitted
            });
            await db.SaveChangesAsync();

            _classId = cls.Id;
            _otherClassId = otherCls.Id;
            _subjectId = subject.Id;
            _termId = term.Id;
            _assignmentId = assignment.Id;
            _submitterId = submitter.Id;
            _nonSubmitterId = nonSubmitter.Id;
            _paperStudentId = paperStudent.Id;
            _harness.CurrentUser.UserId = user.Id;
        }
    }

    private static Student NewStudent(string number, string first, string last, Guid classId) => new()
    {
        StudentNumber = number,
        FirstName = first,
        LastName = last,
        DateOfBirth = new DateTime(2012, 1, 1),
        Gender = Gender.Other,
        AdmissionDate = new DateTime(2026, 1, 1),
        Status = StudentStatus.Active,
        CurrentClassId = classId
    };

    public Task DisposeAsync()
    {
        _harness.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Roster_lists_every_class_member_including_non_submitters_with_a_progress_summary()
    {
        await using var db = _harness.CreateDbContext();
        var result = await new GetAssignmentRosterQueryHandler(db, NoFileStorage())
            .Handle(new GetAssignmentRosterQuery(_assignmentId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var roster = result.Data!;

        roster.RosterCount.Should().Be(3, "three active students are in the class");
        roster.SubmittedCount.Should().Be(1);
        roster.MissingCount.Should().Be(2);
        roster.GradedCount.Should().Be(0);

        roster.Rows.Should().HaveCount(3);
        roster.Rows.Should().Contain(r => r.StudentId == _nonSubmitterId && r.Status == "NotSubmitted" && r.SubmissionId == null);
        roster.Rows.Should().Contain(r => r.StudentId == _submitterId && r.Status == SubmissionStatuses.Submitted && r.SubmissionId != null);
        roster.Rows.Should().NotContain(r => r.StudentName.Contains("Other"), "a student in a different class is not on this roster");
    }

    [Fact]
    public async Task Bulk_grade_grades_an_existing_submission_and_creates_a_graded_record_for_a_paper_student()
    {
        await using (var db = _harness.CreateDbContext())
        {
            var result = await new BulkGradeAssignmentSubmissionsCommandHandler(db).Handle(new BulkGradeAssignmentSubmissionsCommand
            {
                AssignmentId = _assignmentId,
                Grades =
                [
                    new AssignmentGradeEntry { StudentId = _submitterId, Grade = 88, Feedback = "Great work" },
                    new AssignmentGradeEntry { StudentId = _paperStudentId, Grade = 55, Feedback = "Handed in on paper" },
                    new AssignmentGradeEntry { StudentId = _nonSubmitterId, Grade = null }, // skipped
                ]
            }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
        }

        await using var verify = _harness.CreateDbContext();
        var submitter = await verify.AssignmentSubmissions.AsNoTracking().FirstAsync(s => s.StudentId == _submitterId);
        submitter.Grade.Should().Be(88);
        submitter.Status.Should().Be(SubmissionStatuses.Graded);

        var paper = await verify.AssignmentSubmissions.AsNoTracking().FirstAsync(s => s.StudentId == _paperStudentId);
        paper.Grade.Should().Be(55);
        paper.Status.Should().Be(SubmissionStatuses.Graded);

        (await verify.AssignmentSubmissions.AsNoTracking().AnyAsync(s => s.StudentId == _nonSubmitterId))
            .Should().BeFalse("a null grade is skipped, so no record is created");
    }

    [Fact]
    public async Task Bulk_grade_ignores_students_who_are_not_in_the_class()
    {
        // A student from another class must not receive a grade for this assignment.
        var otherClassStudentId = await OtherClassStudentIdAsync();

        await using var db = _harness.CreateDbContext();
        var result = await new BulkGradeAssignmentSubmissionsCommandHandler(db).Handle(new BulkGradeAssignmentSubmissionsCommand
        {
            AssignmentId = _assignmentId,
            Grades = [new AssignmentGradeEntry { StudentId = otherClassStudentId, Grade = 99 }]
        }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        (await db.AssignmentSubmissions.AnyAsync(s => s.StudentId == otherClassStudentId)).Should().BeFalse();
    }

    private async Task<Guid> OtherClassStudentIdAsync()
    {
        await using var db = _harness.CreateDbContext();
        return await db.Students.Where(s => s.CurrentClassId == _otherClassId).Select(s => s.Id).FirstAsync();
    }

    [Fact]
    public async Task Roster_reflects_grades_after_a_bulk_grade()
    {
        await using (var db = _harness.CreateDbContext())
        {
            await new BulkGradeAssignmentSubmissionsCommandHandler(db).Handle(new BulkGradeAssignmentSubmissionsCommand
            {
                AssignmentId = _assignmentId,
                Grades = [new AssignmentGradeEntry { StudentId = _submitterId, Grade = 70 }]
            }, CancellationToken.None);
        }

        await using var verify = _harness.CreateDbContext();
        var roster = (await new GetAssignmentRosterQueryHandler(verify, NoFileStorage())
            .Handle(new GetAssignmentRosterQuery(_assignmentId), CancellationToken.None)).Data!;

        roster.GradedCount.Should().Be(1);
        roster.Rows.First(r => r.StudentId == _submitterId).Grade.Should().Be(70);
    }

    [Fact]
    public async Task Remind_messages_only_guardians_of_non_submitters()
    {
        var channel = new FakeMessageChannel();

        await using var db = _harness.CreateDbContext();
        var result = await new RemindNonSubmittersCommandHandler(db, [channel], _harness.CurrentUser)
            .Handle(new RemindNonSubmittersCommand { AssignmentId = _assignmentId }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.TotalRecipients.Should().Be(1, "only the non-submitter has a contactable guardian");
        result.Data.Delivered.Should().Be(1);

        channel.Sent.Should().ContainSingle();
        channel.Sent[0].Recipient.Should().Be(GuardianPhone);
        channel.Sent[0].Content.Should().Contain("Nina").And.Contain("Algebra worksheet").And.Contain("Mathematics");
    }

    [Fact]
    public async Task Remind_fails_when_everyone_has_submitted()
    {
        // Submit for the remaining class members so nobody is missing.
        await using (var db = _harness.CreateDbContext())
        {
            db.AssignmentSubmissions.AddRange(
                new AssignmentSubmission { AssignmentId = _assignmentId, StudentId = _nonSubmitterId, SubmittedAt = DateTime.UtcNow, Status = SubmissionStatuses.Submitted },
                new AssignmentSubmission { AssignmentId = _assignmentId, StudentId = _paperStudentId, SubmittedAt = DateTime.UtcNow, Status = SubmissionStatuses.Submitted });
            await db.SaveChangesAsync();
        }

        var channel = new FakeMessageChannel();
        await using var verify = _harness.CreateDbContext();
        var result = await new RemindNonSubmittersCommandHandler(verify, [channel], _harness.CurrentUser)
            .Handle(new RemindNonSubmittersCommand { AssignmentId = _assignmentId }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        channel.Sent.Should().BeEmpty();
    }
}
