using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SMS.Application.Features.Lms;
using SMS.Application.Features.Lms.Commands;
using SMS.Application.Features.Lms.Queries;
using SMS.Application.Interfaces;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using Xunit;
using AcademicTermEntity = SMS.Domain.Entities.AcademicTerm;
using SysStream = System.IO.Stream;

namespace SMS.Infrastructure.Tests;

internal sealed class FakeFileStorageService : IFileStorageService
{
    public List<(string FileName, string ContentType)> Uploaded { get; } = [];

    public Task<string> UploadAsync(SysStream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        Uploaded.Add((fileName, contentType));
        return Task.FromResult($"fake-key/{Guid.NewGuid()}/{fileName}");
    }

    public Task<SysStream?> DownloadAsync(string fileKey, CancellationToken cancellationToken = default) =>
        Task.FromResult<SysStream?>(null);

    public Task<bool> DeleteAsync(string fileKey, CancellationToken cancellationToken = default) =>
        Task.FromResult(true);

    public string GetFileUrl(string fileKey) => $"https://files.example/{fileKey}";

    /// <summary>Wraps this fake behind an IServiceProvider - the Lms handlers resolve
    /// IFileStorageService lazily via IServiceProvider.GetRequiredService rather than
    /// taking a constructor dependency, so they don't eagerly build the real S3 client.</summary>
    public IServiceProvider AsServiceProvider() =>
        new ServiceCollection().AddSingleton<IFileStorageService>(this).BuildServiceProvider();
}

public class LmsTests : IAsyncLifetime
{
    private readonly TenantTestContext _harness = new();
    private Guid _classId;
    private Guid _subjectId;
    private Guid _termId;
    private Guid _studentId;

    public async Task InitializeAsync()
    {
        var tenant = new Tenant { Name = "LMS School", Code = "LMS" };
        await using (var db = _harness.CreateDbContext())
        {
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync();
        }
        _harness.UseTenant(tenant.Id);

        await using (var db = _harness.CreateDbContext())
        {
            var cls = new Class { Name = "Form 3", Level = 3, Capacity = 40 };
            var subject = new Subject { Name = "Biology", Code = "BIO" };
            var year = new AcademicYear { Name = "2026", Year = 2026, StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 12, 31) };
            db.Classes.Add(cls);
            db.Subjects.Add(subject);
            db.AcademicYears.Add(year);
            await db.SaveChangesAsync();

            var term = new AcademicTermEntity { AcademicYearId = year.Id, Name = "Term 1", TermNumber = 1, StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 4, 30) };
            db.AcademicTerms.Add(term);

            var student = new Student { StudentNumber = "S-LMS-1", FirstName = "Lee", LastName = "Learner", DateOfBirth = new DateTime(2012, 1, 1), Gender = Gender.Other, AdmissionDate = new DateTime(2026, 1, 1), Status = StudentStatus.Active, CurrentClassId = cls.Id };
            db.Students.Add(student);
            await db.SaveChangesAsync();

            _classId = cls.Id;
            _subjectId = subject.Id;
            _termId = term.Id;
            _studentId = student.Id;
        }
    }

    public Task DisposeAsync()
    {
        _harness.Dispose();
        return Task.CompletedTask;
    }

    private CreateAssignmentCommand ValidAssignment(DateTime dueDate) => new()
    {
        ClassId = _classId,
        SubjectId = _subjectId,
        AcademicTermId = _termId,
        Title = "Cell structure worksheet",
        Description = "Label the diagram.",
        DueDate = dueDate
    };

    [Fact]
    public async Task Creating_an_assignment_with_an_attachment_uploads_it_and_stores_the_key()
    {
        var storage = new FakeFileStorageService();
        await using var db = _harness.CreateDbContext();

        var result = await new CreateAssignmentCommandHandler(db, storage.AsServiceProvider()).Handle(ValidAssignment(new DateTime(2026, 3, 1)) with
        {
            AttachmentFileName = "worksheet.pdf",
            AttachmentContentType = "application/pdf",
            AttachmentContent = [1, 2, 3]
        }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        storage.Uploaded.Should().ContainSingle(u => u.FileName == "worksheet.pdf");

        await using var verify = _harness.CreateDbContext();
        var assignment = await verify.Assignments.AsNoTracking().FirstAsync(a => a.Id == result.Data);
        assignment.AttachmentKey.Should().NotBeNullOrEmpty();
        assignment.AttachmentFileName.Should().Be("worksheet.pdf");
    }

    [Fact]
    public async Task Submitting_before_the_due_date_is_marked_Submitted_and_after_is_marked_Late()
    {
        var storage = new FakeFileStorageService();
        Guid onTimeAssignmentId, lateAssignmentId;

        await using (var db = _harness.CreateDbContext())
        {
            var onTime = await new CreateAssignmentCommandHandler(db, storage.AsServiceProvider()).Handle(ValidAssignment(new DateTime(2026, 3, 10)), CancellationToken.None);
            onTimeAssignmentId = onTime.Data;
            var late = await new CreateAssignmentCommandHandler(db, storage.AsServiceProvider()).Handle(ValidAssignment(new DateTime(2026, 3, 1)) with { Title = "Late one" }, CancellationToken.None);
            lateAssignmentId = late.Data;
        }

        await using (var db = _harness.CreateDbContext())
        {
            var onTimeSub = await new RecordAssignmentSubmissionCommandHandler(db, storage.AsServiceProvider()).Handle(new RecordAssignmentSubmissionCommand
            {
                AssignmentId = onTimeAssignmentId,
                StudentId = _studentId,
                SubmittedAt = new DateTime(2026, 3, 9)
            }, CancellationToken.None);
            onTimeSub.IsSuccess.Should().BeTrue();

            var lateSub = await new RecordAssignmentSubmissionCommandHandler(db, storage.AsServiceProvider()).Handle(new RecordAssignmentSubmissionCommand
            {
                AssignmentId = lateAssignmentId,
                StudentId = _studentId,
                SubmittedAt = new DateTime(2026, 3, 5)
            }, CancellationToken.None);
            lateSub.IsSuccess.Should().BeTrue();
        }

        await using var verify = _harness.CreateDbContext();
        (await verify.AssignmentSubmissions.AsNoTracking().FirstAsync(s => s.AssignmentId == onTimeAssignmentId)).Status.Should().Be(SubmissionStatuses.Submitted);
        (await verify.AssignmentSubmissions.AsNoTracking().FirstAsync(s => s.AssignmentId == lateAssignmentId)).Status.Should().Be(SubmissionStatuses.Late);
    }

    [Fact]
    public async Task Resubmitting_before_grading_replaces_the_existing_submission()
    {
        var storage = new FakeFileStorageService();
        Guid assignmentId;
        await using (var db = _harness.CreateDbContext())
        {
            var created = await new CreateAssignmentCommandHandler(db, storage.AsServiceProvider()).Handle(ValidAssignment(new DateTime(2026, 3, 10)), CancellationToken.None);
            assignmentId = created.Data;
        }

        Guid firstSubmissionId;
        await using (var db = _harness.CreateDbContext())
        {
            var first = await new RecordAssignmentSubmissionCommandHandler(db, storage.AsServiceProvider()).Handle(new RecordAssignmentSubmissionCommand
            {
                AssignmentId = assignmentId,
                StudentId = _studentId,
                Comment = "First attempt"
            }, CancellationToken.None);
            firstSubmissionId = first.Data;
        }

        await using (var db = _harness.CreateDbContext())
        {
            var second = await new RecordAssignmentSubmissionCommandHandler(db, storage.AsServiceProvider()).Handle(new RecordAssignmentSubmissionCommand
            {
                AssignmentId = assignmentId,
                StudentId = _studentId,
                Comment = "Revised attempt"
            }, CancellationToken.None);
            second.IsSuccess.Should().BeTrue();
            second.Data.Should().Be(firstSubmissionId, "resubmission updates the same record rather than creating a duplicate");
        }

        await using var verify = _harness.CreateDbContext();
        var submissions = await verify.AssignmentSubmissions.AsNoTracking().Where(s => s.AssignmentId == assignmentId).ToListAsync();
        submissions.Should().ContainSingle();
        submissions[0].Comment.Should().Be("Revised attempt");
    }

    [Fact]
    public async Task A_graded_submission_can_no_longer_be_replaced()
    {
        var storage = new FakeFileStorageService();
        Guid assignmentId, submissionId;
        await using (var db = _harness.CreateDbContext())
        {
            var created = await new CreateAssignmentCommandHandler(db, storage.AsServiceProvider()).Handle(ValidAssignment(new DateTime(2026, 3, 10)), CancellationToken.None);
            assignmentId = created.Data;
        }

        await using (var db = _harness.CreateDbContext())
        {
            var sub = await new RecordAssignmentSubmissionCommandHandler(db, storage.AsServiceProvider()).Handle(new RecordAssignmentSubmissionCommand
            {
                AssignmentId = assignmentId,
                StudentId = _studentId
            }, CancellationToken.None);
            submissionId = sub.Data;
        }

        await using (var db = _harness.CreateDbContext())
        {
            var graded = await new GradeAssignmentSubmissionCommandHandler(db).Handle(new GradeAssignmentSubmissionCommand
            {
                SubmissionId = submissionId,
                Grade = 85,
                Feedback = "Well done."
            }, CancellationToken.None);
            graded.IsSuccess.Should().BeTrue();
        }

        await using var db2 = _harness.CreateDbContext();
        var resubmit = await new RecordAssignmentSubmissionCommandHandler(db2, storage.AsServiceProvider()).Handle(new RecordAssignmentSubmissionCommand
        {
            AssignmentId = assignmentId,
            StudentId = _studentId,
            Comment = "Too late"
        }, CancellationToken.None);

        resubmit.IsSuccess.Should().BeFalse();
        resubmit.Error.Should().Contain("graded");
    }

    [Fact]
    public async Task Submissions_list_includes_a_download_url_for_the_attachment()
    {
        var storage = new FakeFileStorageService();
        Guid assignmentId;
        await using (var db = _harness.CreateDbContext())
        {
            var created = await new CreateAssignmentCommandHandler(db, storage.AsServiceProvider()).Handle(ValidAssignment(new DateTime(2026, 3, 10)), CancellationToken.None);
            assignmentId = created.Data;
        }

        await using (var db = _harness.CreateDbContext())
        {
            await new RecordAssignmentSubmissionCommandHandler(db, storage.AsServiceProvider()).Handle(new RecordAssignmentSubmissionCommand
            {
                AssignmentId = assignmentId,
                StudentId = _studentId,
                AttachmentFileName = "answer.docx",
                AttachmentContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                AttachmentContent = [9, 9, 9]
            }, CancellationToken.None);
        }

        await using var db2 = _harness.CreateDbContext();
        var submissions = await new GetAssignmentSubmissionsQueryHandler(db2, storage.AsServiceProvider())
            .Handle(new GetAssignmentSubmissionsQuery(assignmentId), CancellationToken.None);

        submissions.IsSuccess.Should().BeTrue();
        submissions.Data.Should().ContainSingle();
        submissions.Data![0].AttachmentUrl.Should().StartWith("https://files.example/");
        submissions.Data[0].StudentName.Should().Contain("Lee");
    }

    [Fact]
    public async Task Student_assignment_list_shows_submission_status_and_grade()
    {
        var storage = new FakeFileStorageService();
        Guid assignmentId, submissionId;
        await using (var db = _harness.CreateDbContext())
        {
            var created = await new CreateAssignmentCommandHandler(db, storage.AsServiceProvider()).Handle(ValidAssignment(new DateTime(2026, 3, 10)), CancellationToken.None);
            assignmentId = created.Data;
        }

        await using (var db = _harness.CreateDbContext())
        {
            var sub = await new RecordAssignmentSubmissionCommandHandler(db, storage.AsServiceProvider()).Handle(new RecordAssignmentSubmissionCommand
            {
                AssignmentId = assignmentId,
                StudentId = _studentId
            }, CancellationToken.None);
            submissionId = sub.Data;

            await new GradeAssignmentSubmissionCommandHandler(db).Handle(new GradeAssignmentSubmissionCommand
            {
                SubmissionId = submissionId,
                Grade = 90
            }, CancellationToken.None);
        }

        await using var db2 = _harness.CreateDbContext();
        var list = await new GetStudentAssignmentsQueryHandler(db2, storage.AsServiceProvider()).Handle(new GetStudentAssignmentsQuery(_studentId), CancellationToken.None);

        list.IsSuccess.Should().BeTrue();
        var entry = list.Data!.Single(a => a.AssignmentId == assignmentId);
        entry.HasSubmitted.Should().BeTrue();
        entry.SubmissionStatus.Should().Be(SubmissionStatuses.Graded);
        entry.Grade.Should().Be(90);
    }
}
