using FluentValidation.TestHelper;
using SMS.Application.Features.Academic.Queries;
using Xunit;

namespace SMS.Application.Tests.Features.Academic;

public class GenerateReportCardQueryValidatorTests
{
    private readonly GenerateReportCardQueryValidator _validator = new();

    [Fact]
    public void Validate_ShouldPass_WhenStudentAndTermProvided()
    {
        var query = new GenerateReportCardQuery { StudentId = Guid.NewGuid(), AcademicTermId = Guid.NewGuid() };

        _validator.TestValidate(query).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ShouldFail_WhenStudentIdIsEmpty()
    {
        var query = new GenerateReportCardQuery { StudentId = Guid.Empty, AcademicTermId = Guid.NewGuid() };

        _validator.TestValidate(query).ShouldHaveValidationErrorFor(x => x.StudentId);
    }

    [Fact]
    public void Validate_ShouldFail_WhenTermIsEmpty()
    {
        var query = new GenerateReportCardQuery { StudentId = Guid.NewGuid(), AcademicTermId = Guid.Empty };

        _validator.TestValidate(query).ShouldHaveValidationErrorFor(x => x.AcademicTermId);
    }
}
