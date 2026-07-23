using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Features.Lms.Commands;
using SMS.Application.Features.Lms.Queries;
using SMS.Application.Features.ParentPortal.Queries;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using Xunit;
using AcademicTermEntity = SMS.Domain.Entities.AcademicTerm;

namespace SMS.Infrastructure.Tests;

/// <summary>
/// Course-materials library: staff publish links/files to a class + subject; the portal
/// query is scoped to a student's class and to published rows only, and the parent wrapper
/// enforces ownership. (Reuses FakeFileStorageService from LmsTests.)
/// </summary>
public class CourseMaterialsTests : IAsyncLifetime
{
    private readonly TenantTestContext _harness = new();
    private Guid _classId;
    private Guid _otherClassId;
    private Guid _subjectId;
    private Guid _termId;
    private Guid _studentId;      // in _classId, owned by _parentUserId
    private Guid _otherStudentId; // not owned by the parent
    private Guid _parentUserId;

    public async Task InitializeAsync()
    {
        var tenant = new Tenant { Name = "Materials School", Code = "MAT" };
        await using (var db = _harness.CreateDbContext())
        {
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync();
        }
        _harness.UseTenant(tenant.Id);

        await using (var db = _harness.CreateDbContext())
        {
            var parent = new User { Email = "p@mat.zw", PasswordHash = "x", FirstName = "Pat", LastName = "Parent" };
            var cls = new Class { Name = "Form 3", Level = 3, Capacity = 40 };
            var otherClass = new Class { Name = "Form 4", Level = 4, Capacity = 40 };
            var subject = new Subject { Name = "Biology", Code = "BIO" };
            var year = new AcademicYear { Name = "2026", Year = 2026, StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 12, 31) };
            db.Users.Add(parent);
            db.Classes.AddRange(cls, otherClass);
            db.Subjects.Add(subject);
            db.AcademicYears.Add(year);
            await db.SaveChangesAsync();

            var term = new AcademicTermEntity { AcademicYearId = year.Id, Name = "Term 1", TermNumber = 1, StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 4, 30) };
            db.AcademicTerms.Add(term);

            var student = new Student { StudentNumber = "S-MAT-1", FirstName = "Mia", LastName = "Learner", DateOfBirth = new DateTime(2012, 1, 1), Gender = Gender.Other, AdmissionDate = new DateTime(2026, 1, 1), Status = StudentStatus.Active, CurrentClassId = cls.Id };
            var otherStudent = new Student { StudentNumber = "S-MAT-2", FirstName = "Ola", LastName = "Other", DateOfBirth = new DateTime(2012, 1, 1), Gender = Gender.Other, AdmissionDate = new DateTime(2026, 1, 1), Status = StudentStatus.Active, CurrentClassId = otherClass.Id };
            var guardian = new Guardian { FirstName = "Pat", LastName = "Parent", Gender = Gender.Other, UserId = parent.Id };
            db.Students.AddRange(student, otherStudent);
            db.Guardians.Add(guardian);
            await db.SaveChangesAsync();

            db.StudentGuardians.Add(new StudentGuardian { StudentId = student.Id, GuardianId = guardian.Id, Relationship = "Parent", IsPrimaryContact = true });
            await db.SaveChangesAsync();

            _classId = cls.Id;
            _otherClassId = otherClass.Id;
            _subjectId = subject.Id;
            _termId = term.Id;
            _studentId = student.Id;
            _otherStudentId = otherStudent.Id;
            _parentUserId = parent.Id;
        }
    }

    public Task DisposeAsync()
    {
        _harness.Dispose();
        return Task.CompletedTask;
    }

    private CreateCourseMaterialCommand LinkMaterial() => new()
    {
        ClassId = _classId,
        SubjectId = _subjectId,
        AcademicTermId = _termId,
        Title = "Cell biology video",
        Url = "https://videos.example/cells"
    };

    [Fact]
    public async Task Creating_a_link_material_stores_the_url_and_uploads_nothing()
    {
        var storage = new FakeFileStorageService();
        await using var db = _harness.CreateDbContext();

        var result = await new CreateCourseMaterialCommandHandler(db, _harness.CurrentUser, storage.AsServiceProvider())
            .Handle(LinkMaterial(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        storage.Uploaded.Should().BeEmpty();

        await using var verify = _harness.CreateDbContext();
        var material = await verify.CourseMaterials.AsNoTracking().FirstAsync(m => m.Id == result.Data);
        material.Url.Should().Be("https://videos.example/cells");
        material.AttachmentKey.Should().BeNull();
    }

    [Fact]
    public async Task Creating_a_file_material_uploads_it_and_stores_the_key()
    {
        var storage = new FakeFileStorageService();
        await using var db = _harness.CreateDbContext();

        var result = await new CreateCourseMaterialCommandHandler(db, _harness.CurrentUser, storage.AsServiceProvider())
            .Handle(LinkMaterial() with
            {
                Title = "Worksheet",
                Url = null,
                AttachmentFileName = "worksheet.pdf",
                AttachmentContentType = "application/pdf",
                AttachmentContent = [1, 2, 3]
            }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        storage.Uploaded.Should().ContainSingle(u => u.FileName == "worksheet.pdf");

        await using var verify = _harness.CreateDbContext();
        var material = await verify.CourseMaterials.AsNoTracking().FirstAsync(m => m.Id == result.Data);
        material.AttachmentKey.Should().NotBeNullOrEmpty();
        material.AttachmentFileName.Should().Be("worksheet.pdf");
        material.Url.Should().BeNull();
    }

    [Fact]
    public async Task Staff_list_reports_kind_and_download_url()
    {
        var storage = new FakeFileStorageService();
        await using (var db = _harness.CreateDbContext())
        {
            await new CreateCourseMaterialCommandHandler(db, _harness.CurrentUser, storage.AsServiceProvider())
                .Handle(LinkMaterial(), CancellationToken.None);
            await new CreateCourseMaterialCommandHandler(db, _harness.CurrentUser, storage.AsServiceProvider())
                .Handle(LinkMaterial() with { Title = "Worksheet", Url = null, AttachmentFileName = "w.pdf", AttachmentContentType = "application/pdf", AttachmentContent = [1] }, CancellationToken.None);
        }

        await using var db2 = _harness.CreateDbContext();
        var list = await new GetCourseMaterialsQueryHandler(db2, storage.AsServiceProvider())
            .Handle(new GetCourseMaterialsQuery { ClassId = _classId }, CancellationToken.None);

        list.IsSuccess.Should().BeTrue();
        list.Data.Should().HaveCount(2);
        list.Data!.Single(m => m.Kind == "Link").Url.Should().Be("https://videos.example/cells");
        list.Data!.Single(m => m.Kind == "File").DownloadUrl.Should().StartWith("https://files.example/");
    }

    [Fact]
    public async Task Student_list_returns_only_published_materials_for_the_childs_class()
    {
        var storage = new FakeFileStorageService();
        await using (var db = _harness.CreateDbContext())
        {
            await new CreateCourseMaterialCommandHandler(db, _harness.CurrentUser, storage.AsServiceProvider())
                .Handle(LinkMaterial(), CancellationToken.None);

            // A material for another class - must not appear.
            await new CreateCourseMaterialCommandHandler(db, _harness.CurrentUser, storage.AsServiceProvider())
                .Handle(LinkMaterial() with { ClassId = _otherClassId, Title = "Other class only" }, CancellationToken.None);

            // An unpublished material for this class - must not appear.
            var unpublished = new CourseMaterial { ClassId = _classId, SubjectId = _subjectId, Title = "Draft", Url = "https://x", IsPublished = false };
            db.CourseMaterials.Add(unpublished);
            await db.SaveChangesAsync();
        }

        await using var db2 = _harness.CreateDbContext();
        var list = await new GetStudentCourseMaterialsQueryHandler(db2, storage.AsServiceProvider())
            .Handle(new GetStudentCourseMaterialsQuery(_studentId), CancellationToken.None);

        list.IsSuccess.Should().BeTrue();
        list.Data.Should().ContainSingle();
        list.Data![0].Title.Should().Be("Cell biology video");
        list.Data[0].Link.Should().Be("https://videos.example/cells");
    }

    [Fact]
    public async Task Portal_materials_for_an_own_child_delegate_to_the_student_query()
    {
        var sender = new StubSender(_ => Result<List<StudentCourseMaterialDto>>.Success([new StudentCourseMaterialDto { Title = "X" }]));
        _harness.CurrentUser.UserId = _parentUserId;

        await using var db = _harness.CreateDbContext();
        var result = await new GetMyChildCourseMaterialsQueryHandler(db, _harness.CurrentUser, sender)
            .Handle(new GetMyChildCourseMaterialsQuery(_studentId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        sender.WasCalled.Should().BeTrue();
        sender.LastRequest.Should().BeOfType<GetStudentCourseMaterialsQuery>();
    }

    [Fact]
    public async Task Portal_materials_for_another_persons_child_are_denied_without_delegating()
    {
        var sender = new StubSender(_ => throw new InvalidOperationException("must not delegate for a non-owned child"));
        _harness.CurrentUser.UserId = _parentUserId;

        await using var db = _harness.CreateDbContext();
        var result = await new GetMyChildCourseMaterialsQueryHandler(db, _harness.CurrentUser, sender)
            .Handle(new GetMyChildCourseMaterialsQuery(_otherStudentId), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        sender.WasCalled.Should().BeFalse();
    }
}
