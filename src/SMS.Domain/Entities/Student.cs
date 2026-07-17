using SMS.Domain.Common;
using SMS.Domain.Enums;

namespace SMS.Domain.Entities;

/// <summary>
/// Represents a student in the school
/// </summary>
public class Student : AggregateRoot
{
    public string StudentNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string MiddleName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public Gender Gender { get; set; }
    public string? NationalId { get; set; }
    public string? BirthCertificateNumber { get; set; }
    public string? Photo { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Nationality { get; set; } = "Zimbabwean";
    public string? Religion { get; set; }
    public StudentStatus Status { get; set; } = StudentStatus.Active;
    public DateTime AdmissionDate { get; set; }
    public DateTime? GraduationDate { get; set; }
    public string? MedicalNotes { get; set; }
    public string? SpecialNeeds { get; set; }
    public string? PreviousSchool { get; set; }
    
    // Current academic placement
    public Guid? CurrentClassId { get; set; }
    public Guid? HouseId { get; set; }
    public Guid? DormitoryId { get; set; }
    public Guid? RouteStopId { get; set; }

    public string FullName => $"{FirstName} {MiddleName} {LastName}".Replace("  ", " ").Trim();

    // Navigation properties
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual Class? CurrentClass { get; set; }
    public virtual House? House { get; set; }
    public virtual Dormitory? Dormitory { get; set; }
    public virtual RouteStop? RouteStop { get; set; }
    public virtual ICollection<StudentGuardian> Guardians { get; set; } = [];
    public virtual ICollection<Enrollment> Enrollments { get; set; } = [];
    public virtual ICollection<Attendance> Attendances { get; set; } = [];
    public virtual ICollection<Result> Results { get; set; } = [];
    public virtual ICollection<Invoice> Invoices { get; set; } = [];
    public virtual ICollection<DisciplineRecord> DisciplineRecords { get; set; } = [];
}
