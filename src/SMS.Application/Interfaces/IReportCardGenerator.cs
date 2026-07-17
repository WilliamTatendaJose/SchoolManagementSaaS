namespace SMS.Application.Interfaces;

/// <summary>
/// Renders a report card model to a PDF document. Implemented in Infrastructure.
/// </summary>
public interface IReportCardGenerator
{
    byte[] Generate(ReportCardModel model);
}

public record ReportCardModel
{
    public string SchoolName { get; init; } = string.Empty;
    public string StudentName { get; init; } = string.Empty;
    public string StudentNumber { get; init; } = string.Empty;
    public string ClassName { get; init; } = string.Empty;
    public string TermName { get; init; } = string.Empty;
    public string GradingScheme { get; init; } = string.Empty;
    public decimal OverallAverage { get; init; }
    public string OverallGrade { get; init; } = string.Empty;
    public int ClassRank { get; init; }
    public int TotalInClass { get; init; }
    public List<ReportCardSubjectLine> Subjects { get; init; } = [];
    public string? ClassTeacherComment { get; init; }
    public string? HeadComment { get; init; }
    public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;
}

public record ReportCardSubjectLine
{
    public string SubjectName { get; init; } = string.Empty;
    public decimal Percentage { get; init; }
    public string Grade { get; init; } = string.Empty;
}
