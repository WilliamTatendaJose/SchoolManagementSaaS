using FluentAssertions;
using SMS.Application.Common.Grading;
using Xunit;

namespace SMS.Application.Tests.Common;

public class GradeScalesTests
{
    [Theory]
    [InlineData(95, "A*")]
    [InlineData(85, "A")]
    [InlineData(72, "B")]
    [InlineData(61, "C")]
    [InlineData(55, "D")]
    [InlineData(42, "E")]
    [InlineData(30, "U")]
    public void Cambridge_maps_percentage_to_grade(decimal percentage, string expected)
    {
        new CambridgeGradeScale().GetGrade(percentage).Should().Be(expected);
    }

    [Theory]
    [InlineData(80, "A")]
    [InlineData(70, "B")]
    [InlineData(55, "C")]
    [InlineData(47, "D")]
    [InlineData(42, "E")]
    [InlineData(20, "U")]
    public void Zimsec_maps_percentage_to_grade(decimal percentage, string expected)
    {
        new ZimsecGradeScale().GetGrade(percentage).Should().Be(expected);
    }

    [Theory]
    [InlineData("ZIMSEC", GradeScales.Zimsec)]
    [InlineData("zimsec", GradeScales.Zimsec)]
    [InlineData("Cambridge", GradeScales.Cambridge)]
    [InlineData(null, GradeScales.Cambridge)]
    [InlineData("something-else", GradeScales.Cambridge)]
    public void Resolve_selects_the_scale_by_name_defaulting_to_cambridge(string? name, string expectedScaleName)
    {
        GradeScales.Resolve(name).Name.Should().Be(expectedScaleName);
    }
}
