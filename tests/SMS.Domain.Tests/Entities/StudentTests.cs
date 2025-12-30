using FluentAssertions;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using Xunit;

namespace SMS.Domain.Tests.Entities;

public class StudentTests
{
    [Fact]
    public void Student_FullName_ShouldCombineNames()
    {
        // Arrange
        var student = new Student
        {
            FirstName = "John",
            MiddleName = "Paul",
            LastName = "Doe"
        };

        // Act
        var fullName = student.FullName;

        // Assert
        fullName.Should().Be("John Paul Doe");
    }

    [Fact]
    public void Student_FullName_ShouldHandleEmptyMiddleName()
    {
        // Arrange
        var student = new Student
        {
            FirstName = "John",
            MiddleName = "",
            LastName = "Doe"
        };

        // Act
        var fullName = student.FullName;

        // Assert
        fullName.Should().Be("John Doe");
    }

    [Fact]
    public void Student_DefaultStatus_ShouldBeActive()
    {
        // Arrange & Act
        var student = new Student();

        // Assert
        student.Status.Should().Be(StudentStatus.Active);
    }

    [Fact]
    public void Student_ShouldHaveEmptyGuardiansCollection()
    {
        // Arrange & Act
        var student = new Student();

        // Assert
        student.Guardians.Should().NotBeNull();
        student.Guardians.Should().BeEmpty();
    }
}
