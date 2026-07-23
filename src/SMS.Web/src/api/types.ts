export interface TenantInfo {
  id: string
  name: string
  code: string
}

export interface LoginRequest {
  tenantId: string
  email: string
  password: string
}

export interface UserInfo {
  id: string
  email: string
  firstName: string
  lastName: string
  roles: string[]
}

export interface LoginResponse {
  accessToken: string
  refreshToken: string
  expiresIn: number
  user?: UserInfo
}

export interface RoleDto {
  id: string
  name: string
  description?: string | null
}

export interface UserDetailDto {
  id: string
  email: string
  firstName: string
  lastName: string
  fullName: string
  phone?: string | null
  profilePicture?: string | null
  isActive: boolean
  emailConfirmed: boolean
  lastLoginAt?: string | null
  createdAt: string
  roles: RoleDto[]
  permissions: string[]
  staffId?: string | null
  staffNumber?: string | null
  guardianId?: string | null
}

export interface ClassEnrollmentDto {
  className: string
  studentCount: number
}

export interface DashboardDto {
  activeStudents: number
  boardingStudents: number
  totalStaff: number
  totalClasses: number
  totalBilled: number
  totalCollected: number
  totalOutstanding: number
  collectionRatePercent: number
  attendanceRatePercent: number
  enrollmentByClass: ClassEnrollmentDto[]
}

export interface PaginatedList<T> {
  items: T[]
  pageNumber: number
  pageSize: number
  totalCount: number
  totalPages: number
  hasPreviousPage: boolean
  hasNextPage: boolean
}

export const GENDERS = ['Male', 'Female', 'Other'] as const
export type Gender = (typeof GENDERS)[number]

export const STUDENT_STATUSES = [
  'Active',
  'Transferred',
  'Suspended',
  'Expelled',
  'Graduated',
  'Alumni',
] as const
export type StudentStatus = (typeof STUDENT_STATUSES)[number]

export interface StudentDto {
  id: string
  studentNumber: string
  firstName: string
  middleName: string
  lastName: string
  fullName: string
  dateOfBirth: string
  gender: string
  status: string
  photo?: string | null
  className?: string | null
  houseName?: string | null
  admissionDate: string
  primaryGuardianName?: string | null
  primaryGuardianPhone?: string | null
}

export interface StudentImportRow {
  firstName?: string
  middleName?: string
  lastName?: string
  gender?: string
  dateOfBirth?: string
  nationalId?: string
  birthCertificateNumber?: string
  address?: string
  city?: string
  religion?: string
  className?: string
  admissionDate?: string
  guardianFirstName?: string
  guardianLastName?: string
  guardianPhone?: string
  guardianEmail?: string
  guardianRelationship?: string
  openingBalance?: string
}

export interface StudentImportRowResult {
  rowNumber: number
  studentName: string
  errors: string[]
  warnings: string[]
  isValid: boolean
  resolvedClass?: string | null
  guardianAction?: 'create' | 'link-existing' | null
}

export interface StudentImportResult {
  committed: boolean
  totalRows: number
  validRows: number
  errorRows: number
  rows: StudentImportRowResult[]
  studentsCreated: number
  guardiansCreated: number
  guardiansLinked: number
  enrollmentsCreated: number
  openingBalanceInvoices: number
}

export interface StudentGuardianDto {
  id: string
  firstName: string
  lastName: string
  fullName: string
  phone?: string | null
  email?: string | null
  relationship: string
  isPrimaryContact: boolean
  isEmergencyContact: boolean
}

export interface StudentDetailDto {
  id: string
  studentNumber: string
  firstName: string
  middleName: string
  lastName: string
  fullName: string
  dateOfBirth: string
  gender: string
  nationalId?: string | null
  birthCertificateNumber?: string | null
  photo?: string | null
  address?: string | null
  city?: string | null
  nationality?: string | null
  religion?: string | null
  status: string
  admissionDate: string
  graduationDate?: string | null
  medicalNotes?: string | null
  specialNeeds?: string | null
  previousSchool?: string | null
  currentClassId?: string | null
  currentClassName?: string | null
  houseId?: string | null
  houseName?: string | null
  dormitoryId?: string | null
  dormitoryName?: string | null
  guardians: StudentGuardianDto[]
  outstandingBalance: number
  attendancePercentage: number
  totalDemerits: number
  totalMerits: number
}

export interface CreateStudentRequest {
  firstName: string
  middleName?: string
  lastName: string
  dateOfBirth: string
  gender: string
  nationalId?: string
  birthCertificateNumber?: string
  address?: string
  city?: string
  nationality?: string
  religion?: string
  medicalNotes?: string
  specialNeeds?: string
  previousSchool?: string
  classId?: string | null
  houseId?: string | null
  admissionDate: string
}

export interface UpdateStudentRequest {
  id: string
  firstName: string
  middleName?: string
  lastName: string
  dateOfBirth: string
  gender: string
  nationalId?: string
  birthCertificateNumber?: string
  address?: string
  city?: string
  nationality?: string
  religion?: string
  medicalNotes?: string
  specialNeeds?: string
  classId?: string | null
  houseId?: string | null
  dormitoryId?: string | null
}

export interface StreamDto {
  id: string
  name: string
  capacity: number
  studentCount: number
}

export interface ClassDto {
  id: string
  name: string
  code?: string | null
  level: number
  capacity: number
  studentCount: number
  classTeacherName?: string | null
  classroomName?: string | null
  streams: StreamDto[]
}

export interface AcademicYearDto {
  id: string
  name: string
  year: number
  startDate: string
  endDate: string
  isCurrent: boolean
  termCount: number
}

export interface EnrollmentDto {
  id: string
  studentId: string
  studentNumber: string
  studentName: string
  classId: string
  className: string
  streamId?: string | null
  streamName?: string | null
  academicYearId: string
  academicYearName: string
  enrollmentDate: string
  isActive: boolean
}

export interface EnrollStudentRequest {
  studentId: string
  classId: string
  streamId?: string | null
  academicYearId: string
  enrollmentDate?: string
}

export interface TransferStudentRequest {
  studentId: string
  academicYearId: string
  toClassId: string
  toStreamId?: string | null
}

export interface PromoteStudentsRequest {
  toClassId: string
  toStreamId?: string | null
  toAcademicYearId: string
  enrollmentDate?: string
  studentIds: string[]
}

export interface PromotionResultDto {
  promotedCount: number
  skippedStudentIds: string[]
}

export interface CreateClassRequest {
  name: string
  code?: string
  level: number
  capacity: number
  classTeacherId?: string | null
  classroomId?: string | null
}

export interface UpdateClassRequest {
  id: string
  name: string
  code?: string
  level: number
  capacity: number
  classTeacherId?: string | null
  classroomId?: string | null
}

export interface CreateSubjectRequest {
  name: string
  code: string
  description?: string
  isCore: boolean
}

export interface UpdateSubjectRequest {
  id: string
  name: string
  code: string
  description?: string
  isCore: boolean
  isActive: boolean
}

export interface CreateAcademicYearTermRequest {
  name: string
  termNumber: number
  startDate: string
  endDate: string
}

export interface CreateAcademicYearRequest {
  name: string
  year: number
  startDate: string
  endDate: string
  setAsCurrent: boolean
  terms: CreateAcademicYearTermRequest[]
}

export interface UpdateAcademicYearRequest {
  id: string
  name: string
  year: number
  startDate: string
  endDate: string
  setAsCurrent: boolean
}

export interface GuardianListDto {
  id: string
  firstName: string
  lastName: string
  fullName: string
  gender: string
  nationalId?: string | null
  phone?: string | null
  email?: string | null
  occupation?: string | null
  studentCount: number
}

export interface LinkedStudentDto {
  studentId: string
  studentNumber: string
  fullName: string
  className?: string | null
  relationship: string
  isPrimaryContact: boolean
  isEmergencyContact: boolean
  canPickup: boolean
}

export interface GuardianDetailDto {
  id: string
  userId?: string | null
  firstName: string
  lastName: string
  fullName: string
  gender: string
  nationalId?: string | null
  phone?: string | null
  alternatePhone?: string | null
  email?: string | null
  address?: string | null
  occupation?: string | null
  employer?: string | null
  students: LinkedStudentDto[]
}

export interface GuardianStudentLink {
  studentId: string
  relationship: string
  isPrimaryContact: boolean
  isEmergencyContact: boolean
  canPickup: boolean
}

export interface CreateGuardianRequest {
  firstName: string
  lastName: string
  gender: string
  nationalId?: string
  phone?: string
  alternatePhone?: string
  email?: string
  address?: string
  occupation?: string
  employer?: string
  linkToStudent?: GuardianStudentLink | null
}

export interface UpdateGuardianRequest {
  id: string
  firstName: string
  lastName: string
  gender: string
  nationalId?: string
  phone?: string
  alternatePhone?: string
  email?: string
  address?: string
  occupation?: string
  employer?: string
}

export interface LinkGuardianToStudentRequest {
  guardianId: string
  studentId: string
  relationship: string
  isPrimaryContact: boolean
  isEmergencyContact: boolean
  canPickup: boolean
}

export interface UserListDto {
  id: string
  email: string
  firstName: string
  lastName: string
  fullName: string
  phone?: string | null
  isActive: boolean
  emailConfirmed: boolean
  lastLoginAt?: string | null
  createdAt: string
  roles: string[]
}

export interface TeacherSubjectDto {
  subjectId: string
  subjectName: string
  isPrimary: boolean
}

export interface StaffListDto {
  id: string
  staffNumber: string
  fullName: string
  email: string
  department?: string | null
  jobTitle?: string | null
  isTeacher: boolean
  isActive: boolean
}

export interface StaffDetailDto {
  id: string
  userId: string
  staffNumber: string
  fullName: string
  email: string
  phone?: string | null
  department?: string | null
  jobTitle?: string | null
  dateOfJoining?: string | null
  qualifications?: string | null
  specialization?: string | null
  isTeacher: boolean
  isActive: boolean
  subjects: TeacherSubjectDto[]
  pendingLeaveRequests: number
}

export interface CreateStaffRequest {
  userId: string
  department?: string
  jobTitle?: string
  dateOfJoining?: string
  qualifications?: string
  specialization?: string
  isTeacher: boolean
}

export interface UpdateStaffRequest {
  id: string
  department?: string
  jobTitle?: string
  dateOfJoining?: string
  qualifications?: string
  specialization?: string
  isTeacher: boolean
  isActive: boolean
}

export interface AssignSubjectRequest {
  staffId: string
  subjectId: string
  isPrimary: boolean
}

export interface SubjectDto {
  id: string
  name: string
  code: string
  description?: string | null
  isCore: boolean
  isActive: boolean
  teacherCount: number
  classCount: number
}

export interface PermissionDto {
  id: string
  name: string
  code: string
  description?: string | null
  module: string
}

export interface PermissionGroupDto {
  module: string
  permissions: PermissionDto[]
}

export interface RoleDetailDto {
  id: string
  name: string
  description?: string | null
  isSystemRole: boolean
  userCount: number
  permissions: PermissionDto[]
}

export interface CreateRoleRequest {
  name: string
  description?: string
  permissionCodes: string[]
}

export interface UpdateRolePermissionsRequest {
  roleId: string
  permissionCodes: string[]
}

export interface RegisterUserRequest {
  email: string
  password: string
  firstName: string
  lastName: string
  phone?: string
  roles: string[]
  staffId?: string | null
  guardianId?: string | null
}

export interface UpdateUserRequest {
  id: string
  firstName: string
  lastName: string
  phone?: string
  isActive: boolean
  /** Guardian this login belongs to (parent portal access), or null for none. */
  guardianId?: string | null
}

export interface UpdateMyProfileRequest {
  firstName: string
  lastName: string
  phone?: string
}

export interface ChangePasswordRequest {
  currentPassword: string
  newPassword: string
}

export interface AssignRolesRequest {
  userId: string
  roles: string[]
}

export interface AcademicTermDto {
  id: string
  academicYearId: string
  academicYearName: string
  name: string
  termNumber: number
  startDate: string
  endDate: string
  isCurrent: boolean
}

export interface ClassroomDto {
  id: string
  name: string
  building?: string | null
  capacity: number
  hasProjector: boolean
  hasWhiteboard: boolean
  isActive: boolean
}

export interface CreateClassroomRequest {
  name: string
  building?: string
  capacity: number
  hasProjector: boolean
  hasWhiteboard: boolean
}

export interface UpdateClassroomRequest {
  id: string
  name: string
  building?: string
  capacity: number
  hasProjector: boolean
  hasWhiteboard: boolean
  isActive: boolean
}

/** Sunday = 0 .. Saturday = 6, matching .NET's System.DayOfWeek. */
export const DAYS_OF_WEEK = [
  { value: 1, label: 'Monday' },
  { value: 2, label: 'Tuesday' },
  { value: 3, label: 'Wednesday' },
  { value: 4, label: 'Thursday' },
  { value: 5, label: 'Friday' },
  { value: 6, label: 'Saturday' },
  { value: 0, label: 'Sunday' },
] as const

export interface TimetableSlotDto {
  id: string
  classId: string
  className: string
  subjectId: string
  subjectName: string
  teacherId: string
  teacherName: string
  classroomId?: string | null
  classroomName?: string | null
  dayOfWeek: string
  startTime: string
  endTime: string
}

export interface CreateTimetableSlotRequest {
  classId: string
  subjectId: string
  teacherId: string
  classroomId?: string | null
  academicTermId: string
  dayOfWeek: number
  startTime: string
  endTime: string
}

export interface UpdateTimetableSlotRequest {
  id: string
  classId: string
  subjectId: string
  teacherId: string
  classroomId?: string | null
  academicTermId: string
  dayOfWeek: number
  startTime: string
  endTime: string
}

export interface FeeStructureDto {
  id: string
  classId: string
  className: string
  academicYearId: string
  academicYearName: string
  name: string
  description?: string | null
  amount: number
  feeType: string
  isRecurring: boolean
  isOptional: boolean
}

export interface CreateFeeStructureRequest {
  classId: string
  academicYearId: string
  name: string
  description?: string
  amount: number
  feeType: string
  isRecurring: boolean
  isOptional: boolean
}

export interface UpdateFeeStructureRequest {
  id: string
  name: string
  description?: string
  amount: number
  feeType: string
  isRecurring: boolean
  isOptional: boolean
}

export interface GenerateInvoicesRequest {
  academicTermId: string
  classId?: string | null
  dueDate: string
  includeOptionalFees: boolean
  siblingDiscountPercent: number
  carryForwardArrears: boolean
}

export interface InvoiceGenerationResultDto {
  invoicesCreated: number
  studentsSkipped: number
  studentsWithoutFees: number
  totalBilled: number
  totalDiscount: number
  totalArrearsCarriedForward: number
}

export interface InvoiceDto {
  id: string
  invoiceNumber: string
  studentId: string
  studentName: string
  studentNumber: string
  termName: string
  invoiceDate: string
  dueDate: string
  totalAmount: number
  discountAmount: number
  paidAmount: number
  balance: number
  currency: string
}

export interface InvoiceItemDto {
  id: string
  feeStructureId?: string | null
  description: string
  amount: number
  quantity: number
}

export interface InvoicePaymentDto {
  id: string
  receiptNumber: string
  amount: number
  paymentMethod: string
  paymentDate: string
  transactionReference?: string | null
}

export interface InvoiceDetail {
  id: string
  invoiceNumber: string
  studentId: string
  academicTermId: string
  invoiceDate: string
  dueDate: string
  totalAmount: number
  discountAmount: number
  paidAmount: number
  balance: number
  currency: string
  notes?: string | null
  isPaid: boolean
  student: { id: string; fullName: string; studentNumber: string }
  academicTerm: { id: string; name: string }
  items: InvoiceItemDto[]
  payments: InvoicePaymentDto[]
}

export interface CreateInvoiceItemRequest {
  feeStructureId?: string | null
  description: string
  amount: number
  quantity: number
}

export interface CreateInvoiceRequest {
  studentId: string
  academicTermId: string
  dueDate: string
  discountAmount?: number
  currency?: string
  notes?: string
  items: CreateInvoiceItemRequest[]
}

export const PAYMENT_METHODS = ['Cash', 'MobileMoney', 'BankTransfer', 'Card', 'Cheque', 'AccountCredit'] as const

export const PAYMENT_METHOD_LABELS: Record<(typeof PAYMENT_METHODS)[number], string> = {
  Cash: 'Cash',
  MobileMoney: 'Mobile money',
  BankTransfer: 'Bank transfer',
  Card: 'Card',
  Cheque: 'Cheque',
  AccountCredit: 'Account balance',
}

export interface RecordPaymentRequest {
  invoiceId: string
  amount: number
  currency?: string
  exchangeRate?: number
  paymentMethod: string
  paymentDate?: string
  transactionReference?: string
  mobileMoneyNumber?: string
  bankName?: string
  notes?: string
  amountTendered?: number
  excessHandling?: 'Change' | 'Credit'
}

export interface RecordPaymentResult {
  receiptNumber: string
  paymentId: string
  amountApplied: number
  changeDue: number
  creditedToAccount: number
}

export interface PaymentDto {
  id: string
  receiptNumber: string
  invoiceId: string
  invoiceNumber: string
  studentName: string
  amount: number
  paymentMethod: string
  paymentDate: string
  transactionReference?: string | null
}

export interface StudentAccountBalanceDto {
  studentId: string
  balance: number
}

export interface StudentAccountTransactionDto {
  id: string
  amount: number
  type: string
  invoiceId?: string | null
  notes?: string | null
  transactionDate: string
}

export interface FinancePage<T> {
  data: T[]
  total: number
  page: number
  pageSize: number
}

export interface FinanceSummaryDto {
  totalBilled: number
  totalCollected: number
  totalOutstanding: number
  collectionRate: number
  totalInvoices: number
  paidInvoices: number
  unpaidInvoices: number
}

export interface AssessmentDto {
  id: string
  name: string
  description?: string | null
  subjectId: string
  subjectName: string
  classId: string
  className: string
  academicTermId: string
  termName: string
  assessmentType: string
  maxScore: number
  weightPercentage: number
  date?: string | null
  isPublished: boolean
  resultCount: number
  averageScore?: number | null
}

export interface CreateAssessmentRequest {
  name: string
  description?: string
  subjectId: string
  classId: string
  academicTermId: string
  assessmentType: string
  maxScore: number
  weightPercentage: number
  date?: string
}

export interface AssessmentStatisticsDto {
  totalStudents: number
  resultsRecorded: number
  averageScore?: number | null
  averagePercentage?: number | null
  highestScore?: number | null
  lowestScore?: number | null
  passCount: number
  failCount: number
  passRate: number
}

export interface StudentResultDetailDto {
  resultId: string
  studentId: string
  studentNumber: string
  studentName: string
  score: number
  percentage: number
  grade: string
  comment?: string | null
  rank: number
}

export interface AssessmentResultsDto {
  assessmentId: string
  assessmentName: string
  subjectName: string
  classId: string
  className: string
  maxScore: number
  isPublished: boolean
  statistics: AssessmentStatisticsDto
  results: StudentResultDetailDto[]
}

export interface StudentResultInput {
  studentId: string
  score: number
  comment?: string
}

export interface AssignmentDto {
  id: string
  title: string
  description?: string | null
  className: string
  subjectName: string
  dueDate: string
  attachmentFileName?: string | null
  isPublished: boolean
  submissionCount: number
  gradedCount: number
  rosterCount: number
}

export interface StudentAssignmentDto {
  assignmentId: string
  title: string
  description?: string | null
  subjectName: string
  dueDate: string
  attachmentFileName?: string | null
  attachmentUrl?: string | null
  hasSubmitted: boolean
  submissionStatus?: string | null
  submittedAt?: string | null
  grade?: number | null
  feedback?: string | null
  submissionAttachmentFileName?: string | null
  submissionAttachmentUrl?: string | null
}

export interface CourseMaterialDto {
  id: string
  title: string
  description?: string | null
  className: string
  subjectName: string
  termName?: string | null
  kind: 'Link' | 'File'
  url?: string | null
  attachmentFileName?: string | null
  downloadUrl?: string | null
  uploadedByName?: string | null
  createdAt: string
  isPublished: boolean
}

export interface StudentCourseMaterialDto {
  id: string
  title: string
  description?: string | null
  subjectName: string
  termName?: string | null
  kind: 'Link' | 'File'
  link?: string | null
  fileName?: string | null
  createdAt: string
}

export interface AssignmentRosterRowDto {
  studentId: string
  studentName: string
  studentNumber: string
  submissionId?: string | null
  status: 'NotSubmitted' | 'Submitted' | 'Late' | 'Graded'
  submittedAt?: string | null
  comment?: string | null
  feedback?: string | null
  grade?: number | null
  attachmentFileName?: string | null
  attachmentUrl?: string | null
}

export interface AssignmentRosterDto {
  id: string
  title: string
  description?: string | null
  className: string
  subjectName: string
  dueDate: string
  attachmentFileName?: string | null
  attachmentUrl?: string | null
  rosterCount: number
  submittedCount: number
  gradedCount: number
  missingCount: number
  rows: AssignmentRosterRowDto[]
}

export interface AssignmentGradeEntry {
  studentId: string
  grade?: number | null
  feedback?: string | null
}

export interface AssignmentSubmissionDto {
  id: string
  studentId: string
  studentName: string
  submittedAt: string
  comment?: string | null
  attachmentFileName?: string | null
  attachmentUrl?: string | null
  grade?: number | null
  feedback?: string | null
  status: string
}

export interface GradeSubmissionRequest {
  submissionId: string
  grade: number
  feedback?: string
}

export interface DormitoryDto {
  id: string
  name: string
  capacity: number
  gender?: string | null
  wardenId?: string | null
  wardenName?: string | null
  isActive: boolean
  occupants: number
  availableBeds: number
}

export interface CreateDormitoryRequest {
  name: string
  capacity: number
  gender?: string | null
  wardenId?: string | null
}

export interface DormitoryOccupantDto {
  studentId: string
  studentNumber: string
  fullName: string
  className?: string | null
}

export interface AssignDormitoryRequest {
  studentId: string
  dormitoryId?: string | null
}

export interface HouseDto {
  id: string
  name: string
  color?: string | null
  houseMasterId?: string | null
  houseMasterName?: string | null
  memberCount: number
}

export interface CreateHouseRequest {
  name: string
  color?: string
  houseMasterId?: string | null
}

export interface AssignHouseRequest {
  studentId: string
  houseId?: string | null
}

export const WEEKEND_LEAVE_STATUSES = ['Pending', 'Approved', 'Rejected', 'Departed', 'Returned'] as const

export interface WeekendLeaveDto {
  id: string
  studentId: string
  studentName: string
  dormitoryName?: string | null
  departureDate: string
  expectedReturnDate: string
  destination: string
  reason?: string | null
  status: string
  reviewNote?: string | null
  collectedBy?: string | null
  actualDepartureAt?: string | null
  actualReturnAt?: string | null
  guardianNotified: boolean
}

export interface RequestWeekendLeaveRequest {
  studentId: string
  departureDate: string
  expectedReturnDate: string
  destination: string
  reason?: string
}

export interface ReviewWeekendLeaveRequest {
  leaveId: string
  approve: boolean
  reviewNote?: string
  notifyGuardian: boolean
}

export interface RecordLeaveDepartureRequest {
  leaveId: string
  collectedBy: string
  departedAt?: string
}

export interface RecordLeaveReturnRequest {
  leaveId: string
  returnedAt?: string
}

// ---- Attendance ----

export const ATTENDANCE_STATUSES = ['Present', 'Absent', 'Late', 'Excused'] as const
export type AttendanceStatus = (typeof ATTENDANCE_STATUSES)[number]

export interface AttendanceDto {
  id: string
  studentId: string
  studentName: string
  studentNumber: string
  status: string
  timeIn?: string | null
  reason?: string | null
}

export interface AttendanceRecord {
  studentId: string
  status: AttendanceStatus
  timeIn?: string
  reason?: string
}

export interface MarkAttendanceRequest {
  classId: string
  subjectId?: string
  date: string
  sendNotifications: boolean
  records: AttendanceRecord[]
}

export interface AttendanceSummaryDto {
  totalDays: number
  presentDays: number
  absentDays: number
  lateDays: number
  excusedDays: number
  attendancePercentage: number
}

// ---- Discipline ----

export interface DisciplineRecordDto {
  id: string
  studentId: string
  studentName: string
  incidentDate: string
  incidentType: string
  description: string
  actionTaken?: string | null
  demeritsAwarded?: number | null
  meritsAwarded?: number | null
  guardianNotified: boolean
  notificationDate?: string | null
}

export interface CreateDisciplineRecordRequest {
  studentId: string
  incidentDate: string
  incidentType: string
  description: string
  actionTaken?: string
  demeritsAwarded?: number
  meritsAwarded?: number
  notifyGuardian: boolean
  /** Channel for the guardian notification (SMS/WhatsApp); ignored when notifyGuardian is false. */
  channel?: string
}

// ---- Transport ----

export interface TransportRouteDto {
  id: string
  name: string
  vehicleRegistration?: string | null
  driverId?: string | null
  driverName?: string | null
  capacity: number
  isActive: boolean
  stopCount: number
  riders: number
  availableSeats: number
}

export interface CreateTransportRouteRequest {
  name: string
  vehicleRegistration?: string
  driverId?: string
  capacity: number
}

export interface RouteStopDto {
  id: string
  name: string
  sequenceNumber: number
  pickupTime?: string | null
  dropoffTime?: string | null
  riderCount: number
}

export interface CreateRouteStopRequest {
  transportRouteId: string
  name: string
  sequenceNumber: number
  pickupTime?: string
  dropoffTime?: string
}

export interface RouteStopOccupantDto {
  studentId: string
  studentNumber: string
  fullName: string
  className?: string | null
}

export interface AssignStudentToRouteStopRequest {
  studentId: string
  routeStopId?: string | null
}

// ---- Library ----

export interface BookDto {
  id: string
  title: string
  author: string
  isbn?: string | null
  category?: string | null
  totalCopies: number
  onLoan: number
  availableCopies: number
  isActive: boolean
}

export interface CreateBookRequest {
  title: string
  author: string
  isbn?: string
  category?: string
  totalCopies: number
}

export interface BookLoanDto {
  id: string
  bookId: string
  bookTitle: string
  studentId: string
  studentName: string
  borrowedDate: string
  dueDate: string
  returnedDate?: string | null
  status: string
}

export interface BorrowBookRequest {
  bookId: string
  studentId: string
  borrowedDate?: string
  dueDate?: string
}

export interface ReturnBookRequest {
  loanId: string
  returnedDate?: string
  lost: boolean
}

// ---- Assets ----

export const ASSET_CONDITIONS = ['New', 'Good', 'Fair', 'Poor', 'Damaged'] as const

export interface AssetDto {
  id: string
  assetNumber: string
  name: string
  category: string
  description?: string | null
  location?: string | null
  purchasePrice?: number | null
  purchaseDate?: string | null
  condition: string
  assignedToId?: string | null
  assignedToName?: string | null
  isActive: boolean
}

export interface CreateAssetRequest {
  name: string
  category: string
  description?: string
  location?: string
  purchasePrice?: number
  purchaseDate?: string
  condition: string
  assignedToId?: string
}

export interface UpdateAssetRequest extends CreateAssetRequest {
  id: string
  isActive: boolean
}

// ---- Settings ----

export interface SchoolSettingsDto {
  id: string
  name: string
  code: string
  logo?: string | null
  primaryColor?: string | null
  accentColor?: string | null
  address?: string | null
  city?: string | null
  country?: string | null
  phone?: string | null
  email?: string | null
  website?: string | null
  timeZone?: string | null
  currency?: string | null
  status: string
  subscriptionPlan: string
  subscriptionStartDate?: string | null
  subscriptionEndDate?: string | null
  maxStudents: number
  currentStudentCount: number
  smsCredits: number
  hasLmsModule: boolean
  hasTransportModule: boolean
  hasHostelModule: boolean
  hasLibraryModule: boolean
}

export const SUBSCRIPTION_PLANS = ['Basic', 'Standard', 'Premium'] as const

export interface UpdateSchoolSettingsRequest {
  name: string
  logo?: string
  primaryColor?: string
  accentColor?: string
  address?: string
  city?: string
  country?: string
  phone?: string
  email?: string
  website?: string
  timeZone?: string
  currency?: string
}

/** Booleans a tenant can act on for nav/route gating - fetched for every authenticated
 *  user regardless of role, since even a Teacher needs to know whether Transport is on. */
export interface TenantFeaturesDto {
  subscriptionPlan: string
  hasLmsModule: boolean
  hasTransportModule: boolean
  hasHostelModule: boolean
  hasLibraryModule: boolean
}

export type FeatureModule = 'lms' | 'transport' | 'hostel' | 'library'

export const FEATURE_MODULE_LABELS: Record<FeatureModule, string> = {
  lms: 'Learning management',
  transport: 'Transport',
  hostel: 'Boarding / hostel',
  library: 'Library',
}

// ---- Messages ----

export const MESSAGE_CHANNELS = ['SMS', 'WhatsApp'] as const
export const MESSAGE_AUDIENCES = ['SpecificStudents', 'Class', 'AllActiveStudents'] as const

export const MESSAGE_AUDIENCE_LABELS: Record<(typeof MESSAGE_AUDIENCES)[number], string> = {
  SpecificStudents: 'Specific students',
  Class: 'A whole class',
  AllActiveStudents: 'All active students',
}

export interface MessageListDto {
  id: string
  subject: string
  channel: string
  messageType: string
  status: string
  totalRecipients: number
  deliveredCount: number
  failedCount: number
  sentAt?: string | null
  createdAt: string
}

export interface MessageRecipientDto {
  recipientName?: string | null
  recipientPhone: string
  studentId?: string | null
  status: string
  deliveredAt?: string | null
  readAt?: string | null
  failureReason?: string | null
}

export interface MessageDetailDto {
  id: string
  subject: string
  content: string
  channel: string
  messageType: string
  status: string
  totalRecipients: number
  deliveredCount: number
  failedCount: number
  sentAt?: string | null
  createdAt: string
  recipients: MessageRecipientDto[]
}

export interface SendMessageRequest {
  channel: string
  subject: string
  content: string
  audience: (typeof MESSAGE_AUDIENCES)[number]
  studentIds?: string[]
  classId?: string
  scheduledAt?: string
}

export interface SendFeeRemindersRequest {
  channel: string
  classId?: string
}

export interface MessageDispatchResultDto {
  messageId: string
  totalRecipients: number
  delivered: number
  failed: number
}

// ---- Reports ----

export interface DefaulterDto {
  studentId: string
  studentNumber: string
  studentName: string
  className?: string | null
  guardianName?: string | null
  guardianPhone?: string | null
  outstandingBalance: number
}

export interface MethodBreakdownDto {
  paymentMethod: string
  count: number
  amount: number
}

export interface CurrencyReconciliationDto {
  currency: string
  totalCollected: number
  count: number
  byMethod: MethodBreakdownDto[]
}

export interface ReconciliationDto {
  fromDate: string
  toDate: string
  paymentCount: number
  byCurrency: CurrencyReconciliationDto[]
}

// ---- Tenants (platform / SuperAdmin) ----

export const TENANT_STATUSES = ['Pending', 'Active', 'Suspended', 'Deactivated'] as const

export interface TenantDto {
  id: string
  name: string
  code: string
  email?: string | null
  phone?: string | null
  status: string
  subscriptionPlan: string
  maxStudents: number
  subscriptionEndDate?: string | null
  createdAt: string
  hasLmsModule: boolean
  hasTransportModule: boolean
  hasHostelModule: boolean
  hasLibraryModule: boolean
}

export interface CreateTenantRequest {
  name: string
  code: string
  email?: string
  phone?: string
  address?: string
  city?: string
  country?: string
  subscriptionPlan?: string
  maxStudents?: number
}

export interface UpdateTenantStatusRequest {
  status: string
}

export interface UpdateTenantSubscriptionRequest {
  subscriptionPlan: string
  maxStudents: number
  subscriptionEndDate?: string
}

export interface UpdateTenantModulesRequest {
  hasLmsModule: boolean
  hasTransportModule: boolean
  hasHostelModule: boolean
  hasLibraryModule: boolean
}

// ---- Parent portal ----

export interface MyChildDto {
  studentId: string
  studentNumber: string
  fullName: string
  className?: string | null
  outstandingBalance: number
}

export interface MyChildInvoiceDto {
  invoiceId: string
  invoiceNumber: string
  invoiceDate: string
  dueDate: string
  totalAmount: number
  balance: number
  payable: boolean
}

export interface MyChildFinanceDto {
  studentId: string
  totalBilled: number
  totalPaid: number
  outstandingBalance: number
  invoices: MyChildInvoiceDto[]
}

export interface AssessmentResultItemDto {
  assessmentId: string
  assessmentName: string
  assessmentType: string
  score: number
  maxScore: number
  percentage: number
  grade: string
  weightPercentage: number
}

export interface SubjectResultSummaryDto {
  subjectId: string
  subjectName: string
  averageScore: number
  averagePercentage: number
  grade: string
  assessments: AssessmentResultItemDto[]
}

export interface StudentAcademicResultsDto {
  studentId: string
  studentName: string
  studentNumber: string
  className: string
  termName?: string | null
  overallAverage: number
  overallGrade: string
  classRank: number
  totalInClass: number
  subjectResults: SubjectResultSummaryDto[]
}

export interface OnlinePaymentInitiationDto {
  paymentId: string
  reference: string
  redirectUrl?: string | null
  pollUrl?: string | null
  instructions?: string | null
}

export interface PaymentSettlementDto {
  paymentId: string
  status: string
  settled: boolean
  invoiceBalance: number
}
