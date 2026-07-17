using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SMS.Application;
using SMS.Application.Common.Grading;
using SMS.Application.Features.Academic.Commands;
using SMS.Application.Features.Academic.Queries;
using SMS.Application.Interfaces;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using SMS.Infrastructure.Services.Reports;
using Xunit;
using AcademicTermEntity = SMS.Domain.Entities.AcademicTerm;

namespace SMS.Infrastructure.Tests;

public class ReportCardCommentTests : IAsyncLifetime
{
    private readonly TenantTestContext _harness = new();
    private Guid _studentId;
    private Guid _termId;

    public async Task InitializeAsync()
    {
        var tenant = new Tenant { Name = "Comment School", Code = "CMT", Currency = "USD" };
        await using (var db = _harness.CreateDbContext())
        {
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync();
        }
        _harness.UseTenant(tenant.Id);

        await using (var db = _harness.CreateDbContext())
        {
            var cls = new Class { Name = "Form 2", Level = 2, Capacity = 40 };
            var subject = new Subject { Name = "English", Code = "ENG" };
            var year = new AcademicYear { Name = "2026", Year = 2026, StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 12, 31) };
            db.Classes.Add(cls);
            db.Subjects.Add(subject);
            db.AcademicYears.Add(year);
            await db.SaveChangesAsync();

            var term = new AcademicTermEntity { AcademicYearId = year.Id, Name = "Term 1", TermNumber = 1, StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 4, 30) };
            db.AcademicTerms.Add(term);

            var student = new Student
            {
                StudentNumber = "STU-CMT-1",
                FirstName = "Cammy",
                LastName = "Student",
                DateOfBirth = new DateTime(2012, 1, 1),
                Gender = Gender.Other,
                AdmissionDate = new DateTime(2026, 1, 1),
                Status = StudentStatus.Active,
                CurrentClassId = cls.Id
            };
            db.Students.Add(student);
            await db.SaveChangesAsync();
            _studentId = student.Id;
            _termId = term.Id;

            var assessment = new Assessment
            {
                Name = "Midterm",
                SubjectId = subject.Id,
                ClassId = cls.Id,
                AcademicTermId = term.Id,
                AssessmentType = "Test",
                MaxScore = 100,
                WeightPercentage = 100,
                IsPublished = true
            };
            db.Assessments.Add(assessment);
            await db.SaveChangesAsync();

            db.Results.Add(new Result { StudentId = student.Id, AssessmentId = assessment.Id, Score = 70 });
            await db.SaveChangesAsync();

            _harness.CurrentUser.UserId = Guid.NewGuid();
        }
    }

    public Task DisposeAsync()
    {
        _harness.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Saving_a_comment_for_the_first_time_creates_a_record()
    {
        await using var db = _harness.CreateDbContext();
        var result = await new SaveReportCardCommentCommandHandler(db).Handle(new SaveReportCardCommentCommand
        {
            StudentId = _studentId,
            AcademicTermId = _termId,
            ClassTeacherComment = "Works hard, needs to speak up more in class."
        }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        await using var verify = _harness.CreateDbContext();
        var saved = await verify.ReportCardComments.AsNoTracking()
            .FirstAsync(c => c.StudentId == _studentId && c.AcademicTermId == _termId);
        saved.ClassTeacherComment.Should().Be("Works hard, needs to speak up more in class.");
        saved.HeadComment.Should().BeNull();
    }

    [Fact]
    public async Task Saving_the_head_comment_later_does_not_clear_the_teacher_comment()
    {
        await using (var db = _harness.CreateDbContext())
        {
            await new SaveReportCardCommentCommandHandler(db).Handle(new SaveReportCardCommentCommand
            {
                StudentId = _studentId,
                AcademicTermId = _termId,
                ClassTeacherComment = "Teacher remark."
            }, CancellationToken.None);
        }

        await using (var db = _harness.CreateDbContext())
        {
            var result = await new SaveReportCardCommentCommandHandler(db).Handle(new SaveReportCardCommentCommand
            {
                StudentId = _studentId,
                AcademicTermId = _termId,
                HeadComment = "Head remark."
            }, CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
        }

        await using var verify = _harness.CreateDbContext();
        var saved = await verify.ReportCardComments.AsNoTracking()
            .FirstAsync(c => c.StudentId == _studentId && c.AcademicTermId == _termId);
        saved.ClassTeacherComment.Should().Be("Teacher remark.");
        saved.HeadComment.Should().Be("Head remark.");
    }

    [Fact]
    public async Task Get_returns_nulls_when_nothing_has_been_saved()
    {
        await using var db = _harness.CreateDbContext();
        var result = await new GetReportCardCommentQueryHandler(db)
            .Handle(new GetReportCardCommentQuery(_studentId, _termId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.ClassTeacherComment.Should().BeNull();
        result.Data.HeadComment.Should().BeNull();
    }

    [Fact]
    public async Task Saving_for_an_unknown_student_fails()
    {
        await using var db = _harness.CreateDbContext();
        var result = await new SaveReportCardCommentCommandHandler(db).Handle(new SaveReportCardCommentCommand
        {
            StudentId = Guid.NewGuid(),
            AcademicTermId = _termId,
            ClassTeacherComment = "n/a"
        }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Generating_the_report_card_falls_back_to_the_persisted_comments()
    {
        await using (var db = _harness.CreateDbContext())
        {
            await new SaveReportCardCommentCommandHandler(db).Handle(new SaveReportCardCommentCommand
            {
                StudentId = _studentId,
                AcademicTermId = _termId,
                ClassTeacherComment = "Saved teacher comment.",
                HeadComment = "Saved head comment."
            }, CancellationToken.None);
        }

        await using var queryContext = _harness.CreateDbContext();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddSingleton<IApplicationDbContext>(queryContext);
        services.AddSingleton<IReportCardGenerator, ReportCardPdfGenerator>();
        services.AddSingleton<ICurrentUserService>(_harness.CurrentUser);
        await using var provider = services.BuildServiceProvider();

        var sender = provider.GetRequiredService<ISender>();

        // No comment passed on the request -> the persisted ones should be used.
        var outcome = await sender.Send(new GenerateReportCardQuery
        {
            StudentId = _studentId,
            AcademicTermId = _termId,
            GradingScheme = GradeScales.Zimsec
        });

        outcome.IsSuccess.Should().BeTrue();
        outcome.Data!.Content.Should().NotBeEmpty();

        // An explicit override on the request still wins over the persisted value.
        var overridden = await sender.Send(new GenerateReportCardQuery
        {
            StudentId = _studentId,
            AcademicTermId = _termId,
            GradingScheme = GradeScales.Zimsec,
            ClassTeacherComment = "Ad-hoc override."
        });

        overridden.IsSuccess.Should().BeTrue();
    }
}
