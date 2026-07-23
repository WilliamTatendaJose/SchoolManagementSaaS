import { Navigate, Outlet, Route, Routes } from 'react-router-dom'
import { AppShell } from './components/layout/AppShell'
import { FeatureRoute } from './components/layout/FeatureRoute'
import { PermissionRoute } from './components/layout/PermissionRoute'
import { ProtectedRoute } from './components/layout/ProtectedRoute'
import { RoleRoute } from './components/layout/RoleRoute'
import { navItems } from './components/layout/navConfig'
import { useAuthStore } from './auth/authStore'
import { useProfile } from './auth/useProfile'
import { ComingSoonPage } from './pages/ComingSoonPage'
import { DashboardPage } from './pages/DashboardPage'
import { LoginPage } from './pages/LoginPage'
import { PaymentReturnPage } from './pages/portal/PaymentReturnPage'
import { PortalHomePage } from './pages/portal/PortalHomePage'
import { PortalShell } from './pages/portal/PortalShell'
import { AcademicSetupPage } from './pages/academic/AcademicSetupPage'
import { AttendancePage } from './pages/attendance/AttendancePage'
import { DisciplineListPage } from './pages/discipline/DisciplineListPage'
import { AssetsListPage } from './pages/assets/AssetsListPage'
import { SettingsPage } from './pages/settings/SettingsPage'
import { LibraryPage } from './pages/library/LibraryPage'
import { MessageDetailPage } from './pages/messages/MessageDetailPage'
import { MessagesListPage } from './pages/messages/MessagesListPage'
import { ProfilePage } from './pages/profile/ProfilePage'
import { ReportsPage } from './pages/reports/ReportsPage'
import { TenantsListPage } from './pages/tenants/TenantsListPage'
import { TransportPage } from './pages/transport/TransportPage'
import { TransportRouteDetailPage } from './pages/transport/TransportRouteDetailPage'
import { AssignmentSubmissionsPage } from './pages/assignments/AssignmentSubmissionsPage'
import { AssignmentsListPage } from './pages/assignments/AssignmentsListPage'
import { MaterialsListPage } from './pages/materials/MaterialsListPage'
import { EnrollmentsListPage } from './pages/enrollments/EnrollmentsListPage'
import { FinancePage } from './pages/finance/FinancePage'
import { InvoiceDetailPage } from './pages/finance/InvoiceDetailPage'
import { InvoicePrintPage } from './pages/finance/InvoicePrintPage'
import { GuardianDetailPage } from './pages/guardians/GuardianDetailPage'
import { GuardiansListPage } from './pages/guardians/GuardiansListPage'
import { BoardingPage } from './pages/hostel/BoardingPage'
import { ImportStudentsPage } from './pages/students/ImportStudentsPage'
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

/**
 * Fetches the current user's profile (roles/permissions) for every authenticated route
 * that needs to know who's logged in, regardless of whether they end up in the staff
 * shell or the parent portal. This has to sit ABOVE both RoleRoute("Parent") and
 * StaffAreaGuard, not inside PortalShell/AppShell: RoleRoute refuses to render its
 * Outlet (and so mount PortalShell) until `profile` is already populated, so if nothing
 * outside that gate fetches it first, the fetch never happens - profile stays null
 * forever (endless spinner on a fresh session) or, worse, stays whatever stale profile
 * was left over from a previous session in this browser, which made a parent get bounced
 * straight to /dashboard because RoleRoute evaluated someone else's roles.
 */
function AuthenticatedRoot() {
  useProfile()
  return <Outlet />
}

/** Parents have no staff permissions (the portal is ownership-scoped, not RBAC), so
 * most staff routes already deny them via PermissionRoute - but /dashboard itself has
 * no permission gate, so this catches that case and sends them to their own portal. */
function StaffAreaGuard() {
  const profile = useAuthStore((s) => s.profile)
  const hasRole = useAuthStore((s) => s.hasRole)

  if (profile && hasRole('Parent')) {
    return <Navigate to="/portal" replace />
  }

  return <Outlet />
}

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />

      <Route element={<ProtectedRoute />}>
        <Route path="/finance/invoices/:id/print" element={<InvoicePrintPage />} />
        <Route path="/payments/return" element={<PaymentReturnPage />} />

        <Route element={<AuthenticatedRoot />}>
          <Route element={<RoleRoute role="Parent" />}>
            <Route element={<PortalShell />}>
              <Route path="/portal" element={<PortalHomePage />} />
            </Route>
          </Route>

          <Route element={<StaffAreaGuard />}>
            <Route element={<AppShell />}>
              <Route path="/dashboard" element={<DashboardPage />} />
              <Route path="/profile" element={<ProfilePage />} />

              <Route element={<PermissionRoute permission="students.view" />}>
                <Route path="/students" element={<StudentsListPage />} />
                <Route path="/students/:id" element={<StudentDetailPage />} />
              </Route>

              <Route element={<PermissionRoute permission="students.import" />}>
                <Route path="/students/import" element={<ImportStudentsPage />} />
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
                <Route path="/materials" element={<MaterialsListPage />} />
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
        </Route>
      </Route>

      <Route path="*" element={<Navigate to="/dashboard" replace />} />
    </Routes>
  )
}
