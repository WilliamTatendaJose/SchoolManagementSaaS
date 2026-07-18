import { Navigate, Route, Routes } from 'react-router-dom'
import { AppShell } from './components/layout/AppShell'
import { FeatureRoute } from './components/layout/FeatureRoute'
import { PermissionRoute } from './components/layout/PermissionRoute'
import { ProtectedRoute } from './components/layout/ProtectedRoute'
import { RoleRoute } from './components/layout/RoleRoute'
import { navItems } from './components/layout/navConfig'
import { ComingSoonPage } from './pages/ComingSoonPage'
import { DashboardPage } from './pages/DashboardPage'
import { LoginPage } from './pages/LoginPage'
import { AcademicSetupPage } from './pages/academic/AcademicSetupPage'
import { AttendancePage } from './pages/attendance/AttendancePage'
import { DisciplineListPage } from './pages/discipline/DisciplineListPage'
import { AssetsListPage } from './pages/assets/AssetsListPage'
import { SettingsPage } from './pages/settings/SettingsPage'
import { LibraryPage } from './pages/library/LibraryPage'
import { MessageDetailPage } from './pages/messages/MessageDetailPage'
import { MessagesListPage } from './pages/messages/MessagesListPage'
import { ReportsPage } from './pages/reports/ReportsPage'
import { TenantsListPage } from './pages/tenants/TenantsListPage'
import { TransportPage } from './pages/transport/TransportPage'
import { TransportRouteDetailPage } from './pages/transport/TransportRouteDetailPage'
import { AssignmentSubmissionsPage } from './pages/assignments/AssignmentSubmissionsPage'
import { AssignmentsListPage } from './pages/assignments/AssignmentsListPage'
import { EnrollmentsListPage } from './pages/enrollments/EnrollmentsListPage'
import { FinancePage } from './pages/finance/FinancePage'
import { InvoiceDetailPage } from './pages/finance/InvoiceDetailPage'
import { InvoicePrintPage } from './pages/finance/InvoicePrintPage'
import { GuardianDetailPage } from './pages/guardians/GuardianDetailPage'
import { GuardiansListPage } from './pages/guardians/GuardiansListPage'
import { BoardingPage } from './pages/hostel/BoardingPage'
import { AssessmentResultsPage } from './pages/results/AssessmentResultsPage'
import { AssessmentsListPage } from './pages/results/AssessmentsListPage'
import { StaffDetailPage } from './pages/staff/StaffDetailPage'
import { StaffListPage } from './pages/staff/StaffListPage'
import { StudentDetailPage } from './pages/students/StudentDetailPage'
import { StudentsListPage } from './pages/students/StudentsListPage'
import { TimetablePage } from './pages/timetable/TimetablePage'
import { UserDetailPage } from './pages/users/UserDetailPage'
import { UsersRolesPage } from './pages/users/UsersRolesPage'

// Nav items with a dedicated route implementation; everything else in navConfig
// still falls back to a ComingSoonPage placeholder below.
const BUILT_PATHS = new Set([
  '/dashboard',
  '/students',
  '/guardians',
  '/staff',
  '/enrollments',
  '/academic-setup',
  '/users',
  '/timetable',
  '/finance',
  '/results',
  '/assignments',
  '/hostel',
  '/attendance',
  '/discipline',
  '/transport',
  '/library',
  '/assets',
  '/settings',
  '/messages',
  '/reports',
  '/tenants',
])

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />

      <Route element={<ProtectedRoute />}>
        <Route path="/finance/invoices/:id/print" element={<InvoicePrintPage />} />

        <Route element={<AppShell />}>
          <Route path="/dashboard" element={<DashboardPage />} />

          <Route element={<PermissionRoute permission="students.view" />}>
            <Route path="/students" element={<StudentsListPage />} />
            <Route path="/students/:id" element={<StudentDetailPage />} />
          </Route>

          <Route element={<PermissionRoute permission="guardians.view" />}>
            <Route path="/guardians" element={<GuardiansListPage />} />
            <Route path="/guardians/:id" element={<GuardianDetailPage />} />
          </Route>

          <Route element={<PermissionRoute permission="staff.view" />}>
            <Route path="/staff" element={<StaffListPage />} />
            <Route path="/staff/:id" element={<StaffDetailPage />} />
          </Route>

          <Route element={<PermissionRoute permission="enrollments.view" />}>
            <Route path="/enrollments" element={<EnrollmentsListPage />} />
          </Route>

          <Route element={<PermissionRoute permission="classes.view" />}>
            <Route path="/academic-setup" element={<AcademicSetupPage />} />
          </Route>

          <Route element={<PermissionRoute permission="users.view" />}>
            <Route path="/users" element={<UsersRolesPage />} />
            <Route path="/users/:id" element={<UserDetailPage />} />
          </Route>

          <Route element={<PermissionRoute permission="timetable.view" />}>
            <Route path="/timetable" element={<TimetablePage />} />
          </Route>

          <Route element={<PermissionRoute permission="finance.view" />}>
            <Route path="/finance" element={<FinancePage />} />
            <Route path="/finance/invoices/:id" element={<InvoiceDetailPage />} />
          </Route>

          <Route element={<PermissionRoute permission="results.view" />}>
            <Route path="/results" element={<AssessmentsListPage />} />
            <Route path="/results/:id" element={<AssessmentResultsPage />} />
          </Route>

          <Route element={<FeatureRoute permission="assignments.view" module="lms" />}>
            <Route path="/assignments" element={<AssignmentsListPage />} />
            <Route path="/assignments/:id" element={<AssignmentSubmissionsPage />} />
          </Route>

          <Route element={<FeatureRoute permission="hostel.view" module="hostel" />}>
            <Route path="/hostel" element={<BoardingPage />} />
          </Route>

          <Route element={<PermissionRoute permission="attendance.view" />}>
            <Route path="/attendance" element={<AttendancePage />} />
          </Route>

          <Route element={<PermissionRoute permission="discipline.view" />}>
            <Route path="/discipline" element={<DisciplineListPage />} />
          </Route>

          <Route element={<FeatureRoute permission="transport.view" module="transport" />}>
            <Route path="/transport" element={<TransportPage />} />
            <Route path="/transport/routes/:id" element={<TransportRouteDetailPage />} />
          </Route>

          <Route element={<FeatureRoute permission="library.view" module="library" />}>
            <Route path="/library" element={<LibraryPage />} />
          </Route>

          <Route element={<PermissionRoute permission="assets.view" />}>
            <Route path="/assets" element={<AssetsListPage />} />
          </Route>

          <Route element={<PermissionRoute permission="settings.view" />}>
            <Route path="/settings" element={<SettingsPage />} />
          </Route>

          <Route element={<PermissionRoute permission="messages.view" />}>
            <Route path="/messages" element={<MessagesListPage />} />
            <Route path="/messages/:id" element={<MessageDetailPage />} />
          </Route>

          <Route element={<PermissionRoute permission="finance.report" />}>
            <Route path="/reports" element={<ReportsPage />} />
          </Route>

          <Route element={<RoleRoute role="SuperAdmin" />}>
            <Route path="/tenants" element={<TenantsListPage />} />
          </Route>

          {navItems
            .filter((item) => !BUILT_PATHS.has(item.path))
            .map((item) =>
              item.permission ? (
                <Route key={item.path} element={<PermissionRoute permission={item.permission} />}>
                  <Route path={item.path} element={<ComingSoonPage title={item.label} icon={item.icon} />} />
                </Route>
              ) : (
                <Route
                  key={item.path}
                  path={item.path}
                  element={<ComingSoonPage title={item.label} icon={item.icon} />}
                />
              ),
            )}
        </Route>
      </Route>

      <Route path="*" element={<Navigate to="/dashboard" replace />} />
    </Routes>
  )
}
