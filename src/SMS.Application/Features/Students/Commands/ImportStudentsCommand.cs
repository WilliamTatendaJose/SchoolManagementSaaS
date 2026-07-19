using MediatR;
using SMS.Application.Common.Models;

namespace SMS.Application.Features.Students.Commands;

/// <summary>
/// Bulk-imports students (with optional guardian, class enrollment and opening balance)
/// from rows parsed out of a spreadsheet. With <see cref="Commit"/> false the command
/// only validates and returns a per-row report; with true it validates and, when every
/// row is valid, creates all records in a single transaction.
/// </summary>
public record ImportStudentsCommand : IRequest<Result<StudentImportResultDto>>
{
    public List<StudentImportRowDto> Rows { get; init; } = [];
    public bool Commit { get; init; }
}

/// <summary>
/// One spreadsheet row. All values arrive as raw strings so parsing problems can be
/// reported per row/field instead of failing JSON model binding for the whole file.
/// </summary>
public record StudentImportRowDto
{
    public string? FirstName { get; init; }
    public string? MiddleName { get; init; }
    public string? LastName { get; init; }
    public string? Gender { get; init; }
    public string? DateOfBirth { get; init; }
    public string? NationalId { get; init; }
    public string? BirthCertificateNumber { get; init; }
    public string? Address { get; init; }
    public string? City { get; init; }
    public string? Religion { get; init; }
    public string? ClassName { get; init; }
    public string? AdmissionDate { get; init; }
    public string? GuardianFirstName { get; init; }
    public string? GuardianLastName { get; init; }
    public string? GuardianPhone { get; init; }
    public string? GuardianEmail { get; init; }
    public string? GuardianRelationship { get; init; }
    public string? OpeningBalance { get; init; }
}

public record StudentImportRowResultDto
{
    public int RowNumber { get; init; }
    public string StudentName { get; init; } = string.Empty;
    public List<string> Errors { get; init; } = [];
    public List<string> Warnings { get; init; } = [];
    public bool IsValid => Errors.Count == 0;
    /// <summary>Resolved class name when the row's class matched, null otherwise.</summary>
    public string? ResolvedClass { get; init; }
    /// <summary>"create", "link-existing" or null when the row has no guardian.</summary>
    public string? GuardianAction { get; init; }
}

public record StudentImportResultDto
{
    public bool Committed { get; init; }
    public int TotalRows { get; init; }
    public int ValidRows { get; init; }
    public int ErrorRows { get; init; }
    public List<StudentImportRowResultDto> Rows { get; init; } = [];

    // Populated only after a successful commit
    public int StudentsCreated { get; init; }
    public int GuardiansCreated { get; init; }
    public int GuardiansLinked { get; init; }
    public int EnrollmentsCreated { get; init; }
    public int OpeningBalanceInvoices { get; init; }
}
