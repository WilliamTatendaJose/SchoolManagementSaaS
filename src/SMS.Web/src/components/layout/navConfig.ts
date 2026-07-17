export interface NavItem {
  label: string
  path: string
  /** Omit to show the item to every authenticated user regardless of permissions. */
  permission?: string
}

export const navItems: NavItem[] = [
  { label: 'Dashboard', path: '/dashboard' },
  { label: 'Students', path: '/students', permission: 'students.view' },
  { label: 'Guardians', path: '/guardians', permission: 'guardians.view' },
  { label: 'Enrollments', path: '/enrollments', permission: 'enrollments.view' },
  { label: 'Attendance', path: '/attendance', permission: 'attendance.view' },
  { label: 'Timetable', path: '/timetable', permission: 'timetable.view' },
  { label: 'Assessments & results', path: '/results', permission: 'results.view' },
  { label: 'Assignments', path: '/assignments', permission: 'assignments.view' },
  { label: 'Fees & invoices', path: '/finance', permission: 'finance.view' },
  { label: 'Discipline', path: '/discipline', permission: 'discipline.view' },
  { label: 'Boarding', path: '/hostel', permission: 'hostel.view' },
  { label: 'Transport', path: '/transport', permission: 'transport.view' },
  { label: 'Library', path: '/library', permission: 'library.view' },
  { label: 'Staff', path: '/staff', permission: 'staff.view' },
  { label: 'Messages', path: '/messages', permission: 'messages.view' },
  { label: 'Assets', path: '/assets', permission: 'assets.view' },
  { label: 'Reports', path: '/reports', permission: 'reports.dashboard' },
  { label: 'Users & roles', path: '/users', permission: 'users.view' },
  { label: 'Settings', path: '/settings', permission: 'settings.view' },
]
