using FluentAssertions;
using FluentValidation.TestHelper;
using SMS.Application.Features.Students.Commands;
using Xunit;

namespace SMS.Application.Tests.Features.Students;

public class CreateStudentCommandValidatorTests
{
    private readonly CreateStudentCommandValidator _validator = new();

    [Fact]
    public void Validate_ShouldPass_WhenValidCommand()
    {
        // Arrange
        var command = new CreateStudentCommand
        {
            FirstName = "John",
            LastName = "Doe",
            DateOfBirth = DateTime.Today.AddYears(-10),
            Gender = "Male",
            AdmissionDate = DateTime.Today
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ShouldFail_WhenFirstNameIsEmpty()
    {
        // Arrange
        var command = new CreateStudentCommand
        {
            FirstName = "",
            LastName = "Doe",
            DateOfBirth = DateTime.Today.AddYears(-10),
            Gender = "Male",
            AdmissionDate = DateTime.Today
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FirstName);
    }

    [Fact]
    public void Validate_ShouldFail_WhenLastNameIsEmpty()
    {
        // Arrange
        var command = new CreateStudentCommand
        {
            FirstName = "John",
            LastName = "",
            DateOfBirth = DateTime.Today.AddYears(-10),
            Gender = "Male",
            AdmissionDate = DateTime.Today
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.LastName);
    }

    [Fact]
    public void Validate_ShouldFail_WhenDateOfBirthIsInFuture()
    {
        // Arrange
        var command = new CreateStudentCommand
        {
            FirstName = "John",
            LastName = "Doe",
            DateOfBirth = DateTime.Today.AddDays(1),
            Gender = "Male",
            AdmissionDate = DateTime.Today
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DateOfBirth);
    }

    [Fact]
    public void Validate_ShouldFail_WhenGenderIsInvalid()
    {
        // Arrange
        var command = new CreateStudentCommand
        {
            FirstName = "John",
            LastName = "Doe",
            DateOfBirth = DateTime.Today.AddYears(-10),
            Gender = "InvalidGender",
            AdmissionDate = DateTime.Today
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Gender);
    }
}
