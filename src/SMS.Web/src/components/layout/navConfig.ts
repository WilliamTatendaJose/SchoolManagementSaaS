import {
  LayoutDashboard,
  Users,
  UserRound,
  ClipboardList,
  CalendarCheck,
  CalendarClock,
  GraduationCap,
  BookOpenCheck,
  Wallet,
  ShieldAlert,
  BedDouble,
  Bus,
  Library,
  Briefcase,
  MessageSquare,
  Package,
  BarChart3,
  UsersRound,
  Settings,
  type LucideIcon,
} from 'lucide-react'

export interface NavItem {
  label: string
  path: string
  icon: LucideIcon
  /** Omit to show the item to every authenticated user regardless of permissions. */
  permission?: string
}

export interface NavGroup {
  label: string
  items: NavItem[]
}

export const navGroups: NavGroup[] = [
  {
    label: 'Overview',
    items: [{ label: 'Dashboard', path: '/dashboard', icon: LayoutDashboard }],
  },
  {
    label: 'People',
    items: [
      { label: 'Students', path: '/students', icon: Users, permission: 'students.view' },
      { label: 'Guardians', path: '/guardians', icon: UserRound, permission: 'guardians.view' },
      { label: 'Staff', path: '/staff', icon: Briefcase, permission: 'staff.view' },
    ],
  },
  {
    label: 'Academic',
    items: [
      { label: 'Enrollments', path: '/enrollments', icon: ClipboardList, permission: 'enrollments.view' },
      { label: 'Attendance', path: '/attendance', icon: CalendarCheck, permission: 'attendance.view' },
      { label: 'Timetable', path: '/timetable', icon: CalendarClock, permission: 'timetable.view' },
      { label: 'Assessments & results', path: '/results', icon: GraduationCap, permission: 'results.view' },
      { label: 'Assignments', path: '/assignments', icon: BookOpenCheck, permission: 'assignments.view' },
    ],
  },
  {
    label: 'Operations',
    items: [
      { label: 'Fees & invoices', path: '/finance', icon: Wallet, permission: 'finance.view' },
      { label: 'Discipline', path: '/discipline', icon: ShieldAlert, permission: 'discipline.view' },
      { label: 'Boarding', path: '/hostel', icon: BedDouble, permission: 'hostel.view' },
      { label: 'Transport', path: '/transport', icon: Bus, permission: 'transport.view' },
      { label: 'Library', path: '/library', icon: Library, permission: 'library.view' },
      { label: 'Assets', path: '/assets', icon: Package, permission: 'assets.view' },
    ],
  },
  {
    label: 'Admin',
    items: [
      { label: 'Messages', path: '/messages', icon: MessageSquare, permission: 'messages.view' },
      { label: 'Reports', path: '/reports', icon: BarChart3, permission: 'reports.dashboard' },
      { label: 'Users & roles', path: '/users', icon: UsersRound, permission: 'users.view' },
      { label: 'Settings', path: '/settings', icon: Settings, permission: 'settings.view' },
    ],
  },
]

export const navItems: NavItem[] = navGroups.flatMap((g) => g.items)
