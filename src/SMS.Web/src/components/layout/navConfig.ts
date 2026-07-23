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
  FolderOpen,
  Briefcase,
  MessageSquare,
  Package,
  BarChart3,
  UsersRound,
  Settings,
  Layers,
  Building2,
  type LucideIcon,
} from 'lucide-react'
import type { FeatureModule } from '../../api/types'

export interface NavItem {
  label: string
  path: string
  icon: LucideIcon
  /** Omit to show the item to every authenticated user regardless of permissions. */
  permission?: string
  /** Gates on role membership instead of a permission - for platform-level areas
   *  (e.g. Tenants) that TenantsController itself checks by role, not permission. */
  role?: string
  /** Optional subscription feature module this item requires. Unlike `permission`/`role`,
   *  this does NOT hide the item - it's shown locked so the user can discover and request
   *  an upgrade, matching FeatureRoute's Paywall behavior at the route level. */
  module?: FeatureModule
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
      { label: 'Classes & subjects', path: '/academic-setup', icon: Layers, permission: 'classes.view' },
      { label: 'Attendance', path: '/attendance', icon: CalendarCheck, permission: 'attendance.view' },
      { label: 'Timetable', path: '/timetable', icon: CalendarClock, permission: 'timetable.view' },
      { label: 'Assessments & results', path: '/results', icon: GraduationCap, permission: 'results.view' },
      { label: 'Assignments', path: '/assignments', icon: BookOpenCheck, permission: 'assignments.view', module: 'lms' },
      { label: 'Course materials', path: '/materials', icon: FolderOpen, permission: 'assignments.view', module: 'lms' },
    ],
  },
  {
    label: 'Operations',
    items: [
      { label: 'Fees & invoices', path: '/finance', icon: Wallet, permission: 'finance.view' },
      { label: 'Discipline', path: '/discipline', icon: ShieldAlert, permission: 'discipline.view' },
      { label: 'Boarding', path: '/hostel', icon: BedDouble, permission: 'hostel.view', module: 'hostel' },
      { label: 'Transport', path: '/transport', icon: Bus, permission: 'transport.view', module: 'transport' },
      { label: 'Library', path: '/library', icon: Library, permission: 'library.view', module: 'library' },
      { label: 'Assets', path: '/assets', icon: Package, permission: 'assets.view' },
    ],
  },
  {
    label: 'Admin',
    items: [
      { label: 'Messages', path: '/messages', icon: MessageSquare, permission: 'messages.view' },
      { label: 'Reports', path: '/reports', icon: BarChart3, permission: 'finance.report' },
      { label: 'Users & roles', path: '/users', icon: UsersRound, permission: 'users.view' },
      { label: 'Settings', path: '/settings', icon: Settings, permission: 'settings.view' },
    ],
  },
  {
    label: 'Platform',
    items: [{ label: 'Schools', path: '/tenants', icon: Building2, role: 'SuperAdmin' }],
  },
]

export const navItems: NavItem[] = navGroups.flatMap((g) => g.items)
