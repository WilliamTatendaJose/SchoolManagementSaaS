using System.Globalization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Common.Models;
using SMS.Application.Interfaces;
using SMS.Domain.Entities;
using SMS.Domain.Enums;

namespace SMS.Application.Features.Students.Commands;

public class ImportStudentsCommandHandler : IRequestHandler<ImportStudentsCommand, Result<StudentImportResultDto>>
{
    private static readonly string[] DateFormats =
    [
        "yyyy-MM-dd", "d/M/yyyy", "dd/MM/yyyy", "d-M-yyyy", "dd-MM-yyyy", "d.M.yyyy", "dd.MM.yyyy"
    ];

    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public ImportStudentsCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<StudentImportResultDto>> Handle(ImportStudentsCommand request, CancellationToken cancellationToken)
    {
        var classRows = await _context.Classes
            .Select(c => new ClassLookupRow(c.Id, c.Name, c.Code))
            .ToListAsync(cancellationToken);
        var classesByKey = BuildClassLookup(classRows);

        var currentYear = await _context.AcademicYears
            .FirstOrDefaultAsync(y => y.IsCurrent, cancellationToken);
        var currentTerm = await _context.AcademicTerms
            .FirstOrDefaultAsync(t => t.IsCurrent, cancellationToken);

        var existingStudentKeys = (await _context.Students
            .Select(s => new { s.FirstName, s.LastName, s.DateOfBirth })
            .ToListAsync(cancellationToken))
            .Select(s => StudentKey(s.FirstName, s.LastName, s.DateOfBirth))
            .ToHashSet();

        var existingNationalIds = (await _context.Students
            .Where(s => s.NationalId != null && s.NationalId != "")
            .Select(s => s.NationalId!)
            .ToListAsync(cancellationToken))
            .Select(Normalize)
            .ToHashSet();

        var existingGuardiansByPhone = (await _context.Guardians
            .Where(g => g.Phone != null && g.Phone != "")
            .Select(g => new GuardianLookupRow(g.Id, g.Phone!, g.FirstName, g.LastName))
            .ToListAsync(cancellationToken))
            .GroupBy(g => NormalizePhone(g.Phone))
            .Where(g => g.Key.Length > 0)
            .ToDictionary(g => g.Key, g => g.First());

        // First pass: parse + validate every row
        var parsed = new List<ParsedRow>();
        var seenStudentKeys = new Dictionary<string, int>();
        var seenNationalIds = new Dictionary<string, int>();

        for (var i = 0; i < request.Rows.Count; i++)
        {
            var row = ParseRow(request.Rows[i], i + 1, classesByKey, currentYear, currentTerm,
                existingStudentKeys, existingNationalIds, existingGuardiansByPhone);

            if (row.StudentKey != null)
            {
                if (seenStudentKeys.TryGetValue(row.StudentKey, out var firstRow))
                    row.Errors.Add($"Duplicate of row {firstRow} (same name and date of birth)");
                else
                    seenStudentKeys[row.StudentKey] = row.RowNumber;
            }

            if (!string.IsNullOrEmpty(row.NormalizedNationalId))
            {
                if (seenNationalIds.TryGetValue(row.NormalizedNationalId, out var firstRow))
                    row.Errors.Add($"Duplicate national ID also on row {firstRow}");
                else
                    seenNationalIds[row.NormalizedNationalId] = row.RowNumber;
            }

            parsed.Add(row);
        }

        var anyErrors = parsed.Any(r => r.Errors.Count > 0);

        if (!request.Commit || anyErrors)
        {
            return Result<StudentImportResultDto>.Success(BuildReport(parsed, committed: false));
        }

        // Second pass: create everything in one SaveChanges (single transaction)
        var counters = await CreateRecordsAsync(parsed, currentYear, currentTerm, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return Result<StudentImportResultDto>.Success(BuildReport(parsed, committed: true) with
        {
            StudentsCreated = counters.Students,
            GuardiansCreated = counters.GuardiansCreated,
            GuardiansLinked = counters.GuardiansLinked,
            EnrollmentsCreated = counters.Enrollments,
            OpeningBalanceInvoices = counters.Invoices
        });
    }

    private sealed class ParsedRow
    {
        public required int RowNumber { get; init; }
        public List<string> Errors { get; } = [];
        public List<string> Warnings { get; } = [];

        public string FirstName = string.Empty;
        public string MiddleName = string.Empty;
        public string LastName = string.Empty;
        public Gender Gender;
        public DateTime DateOfBirth;
        public DateTime AdmissionDate;
        public string? NationalId;
        public string? BirthCertificateNumber;
        public string? Address;
        public string? City;
        public string? Religion;

        public string? StudentKey;
        public string? NormalizedNationalId;

        public Guid? ClassId;
        public string? ResolvedClass;

        public bool HasGuardian;
        public Guid? ExistingGuardianId;
        public string GuardianFirstName = string.Empty;
        public string GuardianLastName = string.Empty;
        public string? GuardianPhone;
        public string? NormalizedGuardianPhone;
        public string? GuardianEmail;
        public string GuardianRelationship = "Parent";

        public decimal OpeningBalance;
    }

    private sealed record Counters(int Students, int GuardiansCreated, int GuardiansLinked, int Enrollments, int Invoices);

    private sealed record ClassLookupRow(Guid Id, string Name, string? Code);

    private sealed record GuardianLookupRow(Guid Id, string Phone, string FirstName, string LastName);

    private static ParsedRow ParseRow(
        StudentImportRowDto dto,
        int rowNumber,
        IReadOnlyDictionary<string, (Guid Id, string Name)> classesByKey,
        AcademicYear? currentYear,
        Domain.Entities.AcademicTerm? currentTerm,
        IReadOnlySet<string> existingStudentKeys,
        IReadOnlySet<string> existingNationalIds,
        IReadOnlyDictionary<string, GuardianLookupRow> existingGuardiansByPhone)
    {
        var row = new ParsedRow { RowNumber = rowNumber };

        row.FirstName = Clean(dto.FirstName);
        row.MiddleName = Clean(dto.MiddleName);
        row.LastName = Clean(dto.LastName);

        if (row.FirstName.Length == 0) row.Errors.Add("First name is required");
        if (row.LastName.Length == 0) row.Errors.Add("Last name is required");

        row.Gender = ParseGender(Clean(dto.Gender), row.Errors);

        var dob = ParseDate(Clean(dto.DateOfBirth));
        if (dob == null)
        {
            row.Errors.Add(string.IsNullOrEmpty(Clean(dto.DateOfBirth))
                ? "Date of birth is required"
                : $"Unrecognised date of birth '{dto.DateOfBirth}' (use YYYY-MM-DD or DD/MM/YYYY)");
        }
        else if (dob.Value > DateTime.UtcNow.Date)
        {
            row.Errors.Add("Date of birth is in the future");
        }
        else if (dob.Value < new DateTime(1970, 1, 1))
        {
            row.Errors.Add("Date of birth is unrealistically old");
        }
        else
        {
            row.DateOfBirth = dob.Value;
        }

        var admissionRaw = Clean(dto.AdmissionDate);
        if (admissionRaw.Length == 0)
        {
            row.AdmissionDate = DateTime.UtcNow.Date;
        }
        else
        {
            var admission = ParseDate(admissionRaw);
            if (admission == null)
                row.Errors.Add($"Unrecognised admission date '{dto.AdmissionDate}' (use YYYY-MM-DD or DD/MM/YYYY)");
            else
                row.AdmissionDate = admission.Value;
        }

        row.NationalId = NullIfEmpty(Clean(dto.NationalId));
        row.BirthCertificateNumber = NullIfEmpty(Clean(dto.BirthCertificateNumber));
        row.Address = NullIfEmpty(Clean(dto.Address));
        row.City = NullIfEmpty(Clean(dto.City));
        row.Religion = NullIfEmpty(Clean(dto.Religion));

        if (row.FirstName.Length > 0 && row.LastName.Length > 0 && row.DateOfBirth != default)
        {
            row.StudentKey = StudentKey(row.FirstName, row.LastName, row.DateOfBirth);
            if (existingStudentKeys.Contains(row.StudentKey))
                row.Errors.Add("A student with this name and date of birth already exists");
        }

        if (row.NationalId != null)
        {
            row.NormalizedNationalId = Normalize(row.NationalId);
            if (existingNationalIds.Contains(row.NormalizedNationalId))
                row.Errors.Add($"A student with national ID '{row.NationalId}' already exists");
        }

        // Class -> enrollment in the current academic year
        var className = Clean(dto.ClassName);
        if (className.Length > 0)
        {
            if (!classesByKey.TryGetValue(Normalize(className), out var cls))
            {
                row.Errors.Add($"Class '{className}' not found (must match a class name or code exactly)");
            }
            else if (currentYear == null)
            {
                row.Errors.Add("No current academic year is set - create one in Academic Setup before importing enrollments");
            }
            else
            {
                row.ClassId = cls.Id;
                row.ResolvedClass = cls.Name;
            }
        }

        ParseGuardian(dto, row, existingGuardiansByPhone);

        var balanceRaw = Clean(dto.OpeningBalance).Replace("$", "").Replace(",", "");
        if (balanceRaw.Length > 0)
        {
            if (!decimal.TryParse(balanceRaw, NumberStyles.Number, CultureInfo.InvariantCulture, out var balance) || balance < 0)
            {
                row.Errors.Add($"Opening balance '{dto.OpeningBalance}' is not a valid amount");
            }
            else if (balance > 0)
            {
                if (currentTerm == null)
                    row.Errors.Add("Opening balances need a current academic term - set one in Academic Setup first");
                else
                    row.OpeningBalance = decimal.Round(balance, 2);
            }
        }

        return row;
    }

    private static void ParseGuardian(StudentImportRowDto dto, ParsedRow row, IReadOnlyDictionary<string, GuardianLookupRow> existingGuardiansByPhone)
    {
        var first = Clean(dto.GuardianFirstName);
        var last = Clean(dto.GuardianLastName);
        var phone = Clean(dto.GuardianPhone);
        var email = Clean(dto.GuardianEmail);
        var relationship = Clean(dto.GuardianRelationship);

        if (first.Length == 0 && last.Length == 0 && phone.Length == 0 && email.Length == 0)
        {
            row.Warnings.Add("No guardian - parent messaging and the parent portal will not reach this student's family");
            return;
        }

        row.HasGuardian = true;
        row.GuardianPhone = NullIfEmpty(phone);
        row.NormalizedGuardianPhone = phone.Length > 0 ? NormalizePhone(phone) : null;
        row.GuardianEmail = NullIfEmpty(email);
        if (relationship.Length > 0) row.GuardianRelationship = relationship;

        if (row.NormalizedGuardianPhone != null &&
            existingGuardiansByPhone.TryGetValue(row.NormalizedGuardianPhone, out var existing))
        {
            row.ExistingGuardianId = existing.Id;
            row.Warnings.Add($"Phone matches existing guardian {existing.FirstName} {existing.LastName} - will link instead of creating a duplicate");
            return;
        }

        if (first.Length == 0 || last.Length == 0)
        {
            row.Errors.Add("Guardian needs both a first and last name (or a phone number matching an existing guardian)");
            return;
        }

        row.GuardianFirstName = first;
        row.GuardianLastName = last;
    }

    private async Task<Counters> CreateRecordsAsync(
        List<ParsedRow> rows,
        AcademicYear? currentYear,
        Domain.Entities.AcademicTerm? currentTerm,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentUserService.TenantId;

        // Per-admission-year sequences for student numbers; soft-deleted rows still
        // count as "used" so regenerated numbers can't collide with their unique index.
        var admissionYears = rows.Select(r => r.AdmissionDate.Year).Distinct().ToList();
        var studentSequences = new Dictionary<int, int>();
        foreach (var year in admissionYears)
        {
            studentSequences[year] = await _context.Students
                .IgnoreQueryFilters()
                .CountAsync(s => s.TenantId == tenantId && s.AdmissionDate.Year == year, cancellationToken);
        }

        var invoiceYear = DateTime.UtcNow.Year;
        var invoiceSequence = await _context.Invoices
            .IgnoreQueryFilters()
            .CountAsync(i => i.TenantId == tenantId && i.InvoiceDate.Year == invoiceYear, cancellationToken);

        // Guardians deduped within the file by normalized phone so siblings on
        // consecutive rows share one new guardian record.
        var newGuardiansByPhone = new Dictionary<string, Guardian>();
        var guardiansCreated = 0;
        var guardiansLinked = 0;
        var enrollments = 0;
        var invoices = 0;

        foreach (var row in rows)
        {
            var sequence = ++studentSequences[row.AdmissionDate.Year];
            var student = new Student
            {
                StudentNumber = $"STU-{row.AdmissionDate.Year}-{sequence:D5}",
                FirstName = row.FirstName,
                MiddleName = row.MiddleName,
                LastName = row.LastName,
                DateOfBirth = row.DateOfBirth,
                Gender = row.Gender,
                NationalId = row.NationalId,
                BirthCertificateNumber = row.BirthCertificateNumber,
                Address = row.Address,
                City = row.City,
                Religion = row.Religion,
                AdmissionDate = row.AdmissionDate,
                CurrentClassId = row.ClassId,
                Status = StudentStatus.Active
            };
            _context.Students.Add(student);

            if (row.HasGuardian)
            {
                Guardian? guardian = null;

                if (row.ExistingGuardianId.HasValue)
                {
                    guardiansLinked++;
                }
                else if (row.NormalizedGuardianPhone != null &&
                         newGuardiansByPhone.TryGetValue(row.NormalizedGuardianPhone, out var shared))
                {
                    guardian = shared;
                    guardiansLinked++;
                }
                else
                {
                    guardian = new Guardian
                    {
                        FirstName = row.GuardianFirstName,
                        LastName = row.GuardianLastName,
                        Phone = row.GuardianPhone,
                        Email = row.GuardianEmail
                    };
                    _context.Guardians.Add(guardian);
                    guardiansCreated++;
                    if (row.NormalizedGuardianPhone != null)
                        newGuardiansByPhone[row.NormalizedGuardianPhone] = guardian;
                }

                var link = new StudentGuardian
                {
                    Student = student,
                    Relationship = row.GuardianRelationship,
                    IsPrimaryContact = true,
                    IsEmergencyContact = true
                };
                if (guardian != null) link.Guardian = guardian;
                else link.GuardianId = row.ExistingGuardianId!.Value;
                _context.StudentGuardians.Add(link);
            }

            if (row.ClassId.HasValue && currentYear != null)
            {
                _context.Enrollments.Add(new Enrollment
                {
                    Student = student,
                    ClassId = row.ClassId.Value,
                    AcademicYearId = currentYear.Id,
                    EnrollmentDate = row.AdmissionDate,
                    IsActive = true
                });
                enrollments++;
            }

            if (row.OpeningBalance > 0 && currentTerm != null)
            {
                invoiceSequence++;
                var invoice = new Invoice
                {
                    InvoiceNumber = $"INV-{invoiceYear}-{invoiceSequence:D6}",
                    Student = student,
                    AcademicTermId = currentTerm.Id,
                    InvoiceDate = DateTime.UtcNow,
                    DueDate = DateTime.UtcNow.Date.AddDays(30),
                    TotalAmount = row.OpeningBalance,
                    Currency = "USD",
                    Notes = "Opening balance brought forward at import"
                };
                _context.Invoices.Add(invoice);
                _context.InvoiceItems.Add(new InvoiceItem
                {
                    Invoice = invoice,
                    Description = "Opening balance brought forward",
                    Amount = row.OpeningBalance
                });
                invoices++;
            }
        }

        return new Counters(rows.Count, guardiansCreated, guardiansLinked, enrollments, invoices);
    }

    private static StudentImportResultDto BuildReport(List<ParsedRow> rows, bool committed) => new()
    {
        Committed = committed,
        TotalRows = rows.Count,
        ValidRows = rows.Count(r => r.Errors.Count == 0),
        ErrorRows = rows.Count(r => r.Errors.Count > 0),
        Rows = rows.Select(r => new StudentImportRowResultDto
        {
            RowNumber = r.RowNumber,
            StudentName = $"{r.FirstName} {r.LastName}".Trim(),
            Errors = r.Errors,
            Warnings = r.Warnings,
            ResolvedClass = r.ResolvedClass,
            GuardianAction = !r.HasGuardian ? null : r.ExistingGuardianId.HasValue ? "link-existing" : "create"
        }).ToList()
    };

    private static Dictionary<string, (Guid Id, string Name)> BuildClassLookup(IEnumerable<ClassLookupRow> classes)
    {
        var lookup = new Dictionary<string, (Guid, string)>();
        foreach (var cls in classes)
        {
            lookup.TryAdd(Normalize(cls.Name), (cls.Id, cls.Name));
            if (!string.IsNullOrWhiteSpace(cls.Code))
                lookup.TryAdd(Normalize(cls.Code!), (cls.Id, cls.Name));
        }
        return lookup;
    }

    private static Gender ParseGender(string raw, List<string> errors)
    {
        switch (raw.ToLowerInvariant())
        {
            case "m" or "male" or "boy": return Gender.Male;
            case "f" or "female" or "girl": return Gender.Female;
            case "other": return Gender.Other;
            case "": errors.Add("Gender is required"); return Gender.Other;
            default: errors.Add($"Unrecognised gender '{raw}' (use M/Male or F/Female)"); return Gender.Other;
        }
    }

    private static DateTime? ParseDate(string raw)
    {
        if (raw.Length == 0) return null;
        if (DateTime.TryParseExact(raw, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            return date.Date;
        // ISO timestamps (e.g. exported JSON dates) still parse
        if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            return date.Date;
        return null;
    }

    private static string Clean(string? value) => (value ?? string.Empty).Trim();

    private static string? NullIfEmpty(string value) => value.Length == 0 ? null : value;

    private static string Normalize(string value) => value.Trim().ToLowerInvariant();

    private static string StudentKey(string first, string last, DateTime dob) =>
        $"{Normalize(first)}|{Normalize(last)}|{dob:yyyy-MM-dd}";

    /// <summary>Digits only, with a leading 263 country code collapsed to local format.</summary>
    private static string NormalizePhone(string phone)
    {
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (digits.StartsWith("263") && digits.Length > 9)
            digits = "0" + digits[3..];
        return digits;
    }
}
