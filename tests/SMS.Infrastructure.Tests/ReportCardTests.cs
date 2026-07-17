using System.Text;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SMS.Application;
using SMS.Application.Common.Grading;
using SMS.Application.Features.Academic.Queries;
using SMS.Application.Interfaces;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using SMS.Infrastructure.Services.Reports;
using Xunit;
using AcademicTermEntity = SMS.Domain.Entities.AcademicTerm;

namespace SMS.Infrastructure.Tests;

public class ReportCardTests : IAsyncLifetime
{
    private readonly TenantTestContext _harness = new();
    private Guid _studentId;
    private Guid _termId;
    private const string StudentNumber = "STU-RC-1";

    public async Task InitializeAsync()
    {
        var tenant = new Tenant { Name = "Report School", Code = "RPT", Currency = "USD" };
        await using (var db = _harness.CreateDbContext())
        {
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync();
        }
        _harness.UseTenant(tenant.Id);

        await using (var db = _harness.CreateDbContext())
        {
            var cls = new Class { Name = "Form 1", Level = 1, Capacity = 40 };
            var subject = new Subject { Name = "Mathematics", Code = "MATH" };
            var year = new AcademicYear { Name = "2026", Year = 2026, StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 12, 31) };
            db.Classes.Add(cls);
            db.Subjects.Add(subject);
            db.AcademicYears.Add(year);
            await db.SaveChangesAsync();

            var term = new AcademicTermEntity { AcademicYearId = year.Id, Name = "Term 1", TermNumber = 1, StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 4, 30) };
            db.AcademicTerms.Add(term);

            var student = new Student
            {
                StudentNumber = StudentNumber,
                FirstName = "Ray",
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
                Name = "End of Term Exam",
                SubjectId = subject.Id,
                ClassId = cls.Id,
                AcademicTermId = term.Id,
                AssessmentType = "Exam",
                MaxScore = 100,
                WeightPercentage = 100,
                IsPublished = true
            };
            db.Assessments.Add(assessment);
            await db.SaveChangesAsync();

            db.Results.Add(new Result { StudentId = student.Id, AssessmentId = assessment.Id, Score = 80 });
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
    public void Generator_produces_a_valid_pdf_document()
    {
        var model = new ReportCardModel
        {
            SchoolName = "Report School",
            StudentName = "Ray Student",
            StudentNumber = StudentNumber,
            ClassName = "Form 1",
            TermName = "Term 1",
            GradingScheme = GradeScales.Zimsec,
            OverallAverage = 80,
            OverallGrade = "A",
            ClassRank = 1,
            TotalInClass = 10,
            Subjects = [new ReportCardSubjectLine { SubjectName = "Mathematics", Percentage = 80, Grade = "A" }],
            ClassTeacherComment = "Good work."
        };

        var pdf = new ReportCardPdfGenerator().Generate(model);

        pdf.Should().NotBeEmpty();
        Encoding.ASCII.GetString(pdf, 0, 4).Should().Be("%PDF");
    }

    [Fact]
    public async Task Generate_report_card_query_returns_a_pdf_for_the_student_and_term()
    {
        await using var queryContext = _harness.CreateDbContext();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddSingleton<IApplicationDbContext>(queryContext);
        services.AddSingleton<IReportCardGenerator, ReportCardPdfGenerator>();
        services.AddSingleton<ICurrentUserService>(_harness.CurrentUser);
        await using var provider = services.BuildServiceProvider();

        var sender = provider.GetRequiredService<ISender>();
        var outcome = await sender.Send(new GenerateReportCardQuery
        {
            StudentId = _studentId,
            AcademicTermId = _termId,
            GradingScheme = GradeScales.Zimsec
        });

        outcome.IsSuccess.Should().BeTrue();
        outcome.Data!.Content.Should().NotBeEmpty();
        Encoding.ASCII.GetString(outcome.Data.Content, 0, 4).Should().Be("%PDF");
        outcome.Data.FileName.Should().Contain(StudentNumber).And.EndWith(".pdf");
    }
}
