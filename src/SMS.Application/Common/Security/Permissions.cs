namespace SMS.Application.Common.Security;

/// <summary>
/// Defines all permission codes used in the system
/// </summary>
public static class Permissions
{
    // Student Management
    public const string StudentsView = "students.view";
    public const string StudentsCreate = "students.create";
    public const string StudentsEdit = "students.edit";
    public const string StudentsDelete = "students.delete";
    public const string StudentsExport = "students.export";

    // Guardian Management
    public const string GuardiansView = "guardians.view";
    public const string GuardiansCreate = "guardians.create";
    public const string GuardiansEdit = "guardians.edit";
    public const string GuardiansDelete = "guardians.delete";

    // Enrollment Management
    public const string EnrollmentsView = "enrollments.view";
    public const string EnrollmentsManage = "enrollments.manage";
    public const string EnrollmentsPromote = "enrollments.promote";

    // Academic Management
    public const string ClassesView = "classes.view";
    public const string ClassesManage = "classes.manage";
    public const string SubjectsView = "subjects.view";
    public const string SubjectsManage = "subjects.manage";
    public const string TimetableView = "timetable.view";
    public const string TimetableManage = "timetable.manage";

    // Assessment & Results
    public const string AssessmentsView = "assessments.view";
    public const string AssessmentsCreate = "assessments.create";
    public const string AssessmentsEdit = "assessments.edit";
    public const string ResultsView = "results.view";
    public const string ResultsRecord = "results.record";
    public const string ResultsPublish = "results.publish";
    public const string ResultsModerate = "results.moderate";

    // Attendance
    public const string AttendanceView = "attendance.view";
    public const string AttendanceMark = "attendance.mark";
    public const string AttendanceReport = "attendance.report";

    // Discipline
    public const string DisciplineView = "discipline.view";
    public const string DisciplineManage = "discipline.manage";

    // Hostel / Boarding
    public const string HostelView = "hostel.view";
    public const string HostelManage = "hostel.manage";

    // Transport
    public const string TransportView = "transport.view";
    public const string TransportManage = "transport.manage";

    // Library
    public const string LibraryView = "library.view";
    public const string LibraryManage = "library.manage";

    // LMS-lite (homework/assignments)
    public const string AssignmentsView = "assignments.view";
    public const string AssignmentsManage = "assignments.manage";

    // Assets
    public const string AssetsView = "assets.view";
    public const string AssetsManage = "assets.manage";

    // Finance
    public const string FinanceView = "finance.view";
    public const string FeeStructuresView = "feestructures.view";
    public const string FeeStructuresManage = "feestructures.manage";
    public const string InvoicesCreate = "invoices.create";
    public const string InvoicesEdit = "invoices.edit";
    public const string PaymentsRecord = "payments.record";
    public const string PaymentsRefund = "payments.refund";
    public const string FinanceReport = "finance.report";

    // Communication
    public const string MessagesView = "messages.view";
    public const string MessagesSend = "messages.send";
    public const string MessagesBulk = "messages.bulk";

    // Staff Management
    public const string StaffView = "staff.view";
    public const string StaffCreate = "staff.create";
    public const string StaffEdit = "staff.edit";
    public const string StaffDelete = "staff.delete";
    public const string LeaveApprove = "leave.approve";

    // User Management
    public const string UsersView = "users.view";
    public const string UsersCreate = "users.create";
    public const string UsersEdit = "users.edit";
    public const string UsersDelete = "users.delete";
    public const string RolesManage = "roles.manage";

    // System Administration
    public const string SettingsView = "settings.view";
    public const string SettingsManage = "settings.manage";
    public const string AuditLogsView = "auditlogs.view";
    public const string TenantManage = "tenant.manage";

    // Reports
    public const string ReportsAcademic = "reports.academic";
    public const string ReportsFinance = "reports.finance";
    public const string ReportsAttendance = "reports.attendance";
    public const string ReportsDashboard = "reports.dashboard";
}
