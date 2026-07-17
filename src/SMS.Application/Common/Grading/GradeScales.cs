namespace SMS.Application.Common.Grading;

/// <summary>
/// Maps a percentage to a letter grade for a particular examination board.
/// </summary>
public interface IGradeScale
{
    string Name { get; }
    string GetGrade(decimal percentage);
}

/// <summary>Cambridge-style scale (A* down to U).</summary>
public sealed class CambridgeGradeScale : IGradeScale
{
    public string Name => GradeScales.Cambridge;

    public string GetGrade(decimal percentage) => percentage switch
    {
        >= 90 => "A*",
        >= 80 => "A",
        >= 70 => "B",
        >= 60 => "C",
        >= 50 => "D",
        >= 40 => "E",
        _ => "U"
    };
}

/// <summary>ZIMSEC O-Level-style scale (A down to U; A-C are passes).</summary>
public sealed class ZimsecGradeScale : IGradeScale
{
    public string Name => GradeScales.Zimsec;

    public string GetGrade(decimal percentage) => percentage switch
    {
        >= 75 => "A",
        >= 65 => "B",
        >= 50 => "C",
        >= 45 => "D",
        >= 40 => "E",
        _ => "U"
    };
}

public static class GradeScales
{
    public const string Cambridge = "Cambridge";
    public const string Zimsec = "ZIMSEC";

    public static IGradeScale Resolve(string? name) => name?.Trim().ToUpperInvariant() switch
    {
        "ZIMSEC" => new ZimsecGradeScale(),
        _ => new CambridgeGradeScale()
    };
}
