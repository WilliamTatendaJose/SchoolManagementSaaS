namespace SMS.Application.Common.Security;

/// <summary>
/// Defines the default roles in the system
/// </summary>
public static class DefaultRoles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string SchoolAdmin = "SchoolAdmin";
    public const string HeadTeacher = "HeadTeacher";
    public const string DeputyHead = "DeputyHead";
    public const string Teacher = "Teacher";
    public const string Student = "Student";
    public const string Parent = "Parent";
    public const string Bursar = "Bursar";
    public const string Librarian = "Librarian";
    public const string HostelWarden = "HostelWarden";
    public const string TransportOfficer = "TransportOfficer";
    public const string IctOfficer = "IctOfficer";

    /// <summary>
    /// Gets default permissions for each role
    /// </summary>
    public static Dictionary<string, List<string>> GetRolePermissions()
    {
        return new Dictionary<string, List<string>>
        {
            [SuperAdmin] = new List<string>
            {
                // SuperAdmin has all permissions
                Permissions.StudentsView, Permissions.StudentsCreate, Permissions.StudentsEdit, Permissions.StudentsDelete, Permissions.StudentsExport, Permissions.StudentsImport,
                Permissions.GuardiansView, Permissions.GuardiansCreate, Permissions.GuardiansEdit, Permissions.GuardiansDelete,
                Permissions.EnrollmentsView, Permissions.EnrollmentsManage, Permissions.EnrollmentsPromote,
                Permissions.ClassesView, Permissions.ClassesManage, Permissions.SubjectsView, Permissions.SubjectsManage,
                Permissions.TimetableView, Permissions.TimetableManage,
                Permissions.AssessmentsView, Permissions.AssessmentsCreate, Permissions.AssessmentsEdit,
                Permissions.ResultsView, Permissions.ResultsRecord, Permissions.ResultsPublish, Permissions.ResultsModerate,
                Permissions.AttendanceView, Permissions.AttendanceMark, Permissions.AttendanceReport,
                Permissions.DisciplineView, Permissions.DisciplineManage,
                Permissions.FinanceView, Permissions.FeeStructuresView, Permissions.FeeStructuresManage, Permissions.InvoicesCreate, Permissions.InvoicesEdit, Permissions.PaymentsRecord, Permissions.PaymentsRefund, Permissions.FinanceReport,
                Permissions.MessagesView, Permissions.MessagesSend, Permissions.MessagesBulk,
                Permissions.StaffView, Permissions.StaffCreate, Permissions.StaffEdit, Permissions.StaffDelete, Permissions.LeaveApprove,
                Permissions.UsersView, Permissions.UsersCreate, Permissions.UsersEdit, Permissions.UsersDelete, Permissions.RolesManage,
                Permissions.SettingsView, Permissions.SettingsManage, Permissions.AuditLogsView, Permissions.TenantManage,
                Permissions.HostelView, Permissions.HostelManage,
                Permissions.TransportView, Permissions.TransportManage,
                Permissions.LibraryView, Permissions.LibraryManage,
                Permissions.AssignmentsView, Permissions.AssignmentsManage,
                Permissions.AssetsView, Permissions.AssetsManage,
                Permissions.ReportsAcademic, Permissions.ReportsFinance, Permissions.ReportsAttendance, Permissions.ReportsDashboard
            },

            [SchoolAdmin] = new List<string>
            {
                Permissions.StudentsView, Permissions.StudentsCreate, Permissions.StudentsEdit, Permissions.StudentsDelete, Permissions.StudentsExport, Permissions.StudentsImport,
                Permissions.GuardiansView, Permissions.GuardiansCreate, Permissions.GuardiansEdit, Permissions.GuardiansDelete,
                Permissions.EnrollmentsView, Permissions.EnrollmentsManage, Permissions.EnrollmentsPromote,
                Permissions.ClassesView, Permissions.ClassesManage, Permissions.SubjectsView, Permissions.SubjectsManage,
                Permissions.TimetableView, Permissions.TimetableManage,
                Permissions.AssessmentsView, Permissions.AssessmentsCreate, Permissions.AssessmentsEdit,
                Permissions.ResultsView, Permissions.ResultsRecord, Permissions.ResultsPublish, Permissions.ResultsModerate,
                Permissions.AttendanceView, Permissions.AttendanceMark, Permissions.AttendanceReport,
                Permissions.DisciplineView, Permissions.DisciplineManage,
                Permissions.FinanceView, Permissions.FeeStructuresView, Permissions.FeeStructuresManage, Permissions.InvoicesCreate, Permissions.InvoicesEdit, Permissions.PaymentsRecord, Permissions.FinanceReport,
                Permissions.MessagesView, Permissions.MessagesSend, Permissions.MessagesBulk,
                Permissions.StaffView, Permissions.StaffCreate, Permissions.StaffEdit, Permissions.StaffDelete, Permissions.LeaveApprove,
                Permissions.UsersView, Permissions.UsersCreate, Permissions.UsersEdit, Permissions.RolesManage,
                Permissions.SettingsView, Permissions.SettingsManage,
                Permissions.HostelView, Permissions.HostelManage,
                Permissions.TransportView, Permissions.TransportManage,
                Permissions.LibraryView, Permissions.LibraryManage,
                Permissions.AssignmentsView, Permissions.AssignmentsManage,
                Permissions.AssetsView, Permissions.AssetsManage,
                Permissions.ReportsAcademic, Permissions.ReportsFinance, Permissions.ReportsAttendance, Permissions.ReportsDashboard
            },

            [HeadTeacher] = new List<string>
            {
                Permissions.StudentsView, Permissions.StudentsCreate, Permissions.StudentsEdit, Permissions.StudentsExport, Permissions.StudentsImport,
                Permissions.GuardiansView, Permissions.GuardiansCreate, Permissions.GuardiansEdit,
                Permissions.EnrollmentsView, Permissions.EnrollmentsManage, Permissions.EnrollmentsPromote,
                Permissions.ClassesView, Permissions.ClassesManage, Permissions.SubjectsView, Permissions.SubjectsManage,
                Permissions.TimetableView, Permissions.TimetableManage,
                Permissions.AssessmentsView, Permissions.AssessmentsCreate, Permissions.AssessmentsEdit,
                Permissions.ResultsView, Permissions.ResultsPublish, Permissions.ResultsModerate,
                Permissions.AttendanceView, Permissions.AttendanceReport,
                Permissions.DisciplineView, Permissions.DisciplineManage,
                Permissions.FinanceView, Permissions.FeeStructuresView, Permissions.FinanceReport,
                Permissions.MessagesView, Permissions.MessagesSend, Permissions.MessagesBulk,
                Permissions.StaffView, Permissions.LeaveApprove,
                Permissions.AssignmentsView,
                Permissions.ReportsAcademic, Permissions.ReportsFinance, Permissions.ReportsAttendance, Permissions.ReportsDashboard
            },

            [Teacher] = new List<string>
            {
                Permissions.StudentsView,
                Permissions.GuardiansView,
                Permissions.EnrollmentsView,
                Permissions.ClassesView, Permissions.SubjectsView,
                Permissions.TimetableView,
                Permissions.AssessmentsView, Permissions.AssessmentsCreate, Permissions.AssessmentsEdit,
                Permissions.ResultsView, Permissions.ResultsRecord,
                Permissions.AttendanceView, Permissions.AttendanceMark,
                Permissions.DisciplineView, Permissions.DisciplineManage,
                Permissions.MessagesView, Permissions.MessagesSend,
                Permissions.AssignmentsView, Permissions.AssignmentsManage,
                Permissions.ReportsAcademic, Permissions.ReportsAttendance
            },

            [Bursar] = new List<string>
            {
                Permissions.StudentsView,
                Permissions.GuardiansView,
                Permissions.EnrollmentsView,
                Permissions.FinanceView, Permissions.FeeStructuresView, Permissions.FeeStructuresManage, Permissions.InvoicesCreate, Permissions.InvoicesEdit, Permissions.PaymentsRecord, Permissions.PaymentsRefund, Permissions.FinanceReport,
                Permissions.MessagesView, Permissions.MessagesSend,
                Permissions.ReportsFinance
            },

            // Parent and Student have NO staff permissions. Every staff "View" permission
            // (StudentsView, FinanceView, ResultsView, MessagesView, ...) gates a *list*
            // endpoint scoped to the whole tenant (e.g. GET /finance/invoices with no
            // studentId returns every family's invoices) - granting any of them to a
            // household-facing role would let a parent page through every other family's
            // fees, results and messages. The parent/student portal is scoped entirely by
            // ownership (ParentPortalController checks the caller's own Guardian/Student
            // link, not a permission), so no permission grant is needed for it to work.
            [Parent] = [],

            [Student] = [],

            [HostelWarden] = new List<string>
            {
                Permissions.StudentsView,
                Permissions.HostelView, Permissions.HostelManage,
                Permissions.MessagesView, Permissions.MessagesSend
            },

            [TransportOfficer] = new List<string>
            {
                Permissions.StudentsView,
                Permissions.TransportView, Permissions.TransportManage,
                Permissions.MessagesView, Permissions.MessagesSend
            },

            [Librarian] = new List<string>
            {
                Permissions.StudentsView,
                Permissions.LibraryView, Permissions.LibraryManage,
                Permissions.MessagesView, Permissions.MessagesSend
            }
        };
    }
}
