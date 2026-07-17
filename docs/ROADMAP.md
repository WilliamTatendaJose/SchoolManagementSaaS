# Product Roadmap & Strategy

A working document capturing product direction, feature priorities, frontend approach, and hardening work for the School Management SaaS. Zimbabwe-first, with an architecture that generalizes to the wider African market later.

## Where the codebase stands today

**Phase 1 (Sellable MVP) is complete** — all seven items below are implemented end-to-end (entity → CQRS handler → controller) with a build-verified test suite (100+ tests across domain, application, and SQLite-backed infrastructure integration tests).

**Genuinely implemented end-to-end:**

- Multi-tenancy: EF Core global query filter combining tenant isolation and soft-delete on every `ITenantEntity`, referenced via a context member so it re-evaluates per request; `TenantMiddleware` resolves the tenant from JWT claims; audit/tenant/soft-delete rules applied on both sync and async `SaveChanges`. (A cross-tenant leak from the original captured-service filter was found and fixed.)
- Auth: JWT bearer tokens plus permission-based RBAC (`PermissionPolicyProvider`, `PermissionAuthorizationHandler`, `[RequirePermission]`, permission catalog in `SMS.Application/Common/Security/Permissions.cs`).
- Vertical slices: Students, **Guardians**, **Enrollments** (enroll/transfer/withdraw/promote), Academic (classes, subjects, assessments, results, **report cards**), Attendance, Finance (invoices + payments), **fee structures + bulk invoicing** (sibling discounts, arrears carry-forward), **online payments (Paynow)**, **notifications (SMS + WhatsApp)**, Users/Roles, Tenant provisioning.
- Integrations: AWS S3 file storage; Paynow payment gateway; config-driven SMS + WhatsApp channels; QuestPDF report cards.

**Now implemented end-to-end (Phase 2/3):** Staff/HR, Timetable (+clash detection), Discipline (+guardian notify), Parent portal, Finance depth (defaulters/reconciliation/receipts, payment plans/installments), Boarding/Hostel (+weekend-leave register), Transport, Library, an LMS-lite (assignments + submissions + grading, file attachments via S3), Assets register, and an analytics dashboard. Multi-currency (USD+ZWG) is on Invoice/Payment, and notification dispatch runs through a resilient outbox + background worker. `AuditLog` is wired via a MediatR pipeline behavior; every input-bearing command has a validator.

**Still data-model only / not started:** `Stream` (class streams).

**Backend feature roadmap (Phases 1-3) is now complete.** The only remaining major item is the frontend (no UI yet) — see §3 below.

---

## 1. Product & Business Strategy (Zimbabwe)

### Target segments, in order

1. **Private/independent & trust schools** — they collect meaningful fees, feel fee-leakage pain acutely, and can pay in USD. This is the beachhead.
2. **Mission and boarding schools** — the existing Dormitory/House entities become a differentiator; boarding management is underserved locally.
3. **Government schools** later — procurement is slow and price-sensitive; likely via ministry/district-level deals.

### The wedge: fee collection, not "school admin"

The #1 pain for a Zimbabwean school bursar is fees: term invoicing, arrears carried across terms, sibling discounts, payment plans, reconciling EcoCash/bank/cash receipts, and chasing defaulters. The pitch is "get your fees collected and reconciled" — attendance and academics ride along. The second wedge is **parent communication**: results and fee reminders over SMS/WhatsApp.

### Pricing

Per-student-per-term (indicatively US$0.50–$1.00/student/term), mapped to the existing `SubscriptionPlan` enum:

| Plan | Includes |
|------|----------|
| **Basic** | Students, attendance, fees/invoicing, manual payment recording, SMS bundle |
| **Standard** | + Paynow online payments, report cards, timetable, parent portal |
| **Premium** | + boarding/hostel, HR/leave, discipline, assets, analytics, WhatsApp channel |

SMS/WhatsApp are sold as top-up bundles (pass-through cost + margin) — a real recurring revenue line.

### Zimbabwe-specific realities to bake in

- **Three-term academic year** (Jan–Apr / May–Aug / Sep–Dec) — the `AcademicTerm` enum already matches.
- **Multi-currency**: USD + ZWG. Per-invoice currency with an exchange-rate snapshot at payment time. Schools quote in USD but accept ZWG; clean cross-currency reconciliation is a killer feature.
- **ZIMSEC and Cambridge** grading schemes for assessments and report cards.
- **Patchy connectivity** — the frontend must tolerate offline (see §3).
- **Parents live on WhatsApp**; SMS is the fallback; email is nearly irrelevant for parent communication.

---

## 2. Feature Roadmap (backend)

Every item follows the established slice pattern: entity (mostly already exists) → MediatR command/query + FluentValidation validator under `src/SMS.Application/Features/<Area>` → controller in `src/SMS.API/Controllers` gated by `[RequirePermission]` → new permissions registered in `Permissions.cs`.

### Phase 1 — Sellable MVP (close the money + communication loop) — ✅ complete

1. ✅ **Guardians API** (`Guardian`, `StudentGuardian`) — CRUD and student linking. Prerequisite for all parent communication and the parent portal.
2. ✅ **Enrollment API** (`Enrollment`) — enroll, promote, transfer, and withdraw students per class and academic year.
3. ✅ **Fee structures → bulk invoicing** (`FeeStructure` + `Invoice`) — define fees per class/term; one command generates term invoices for all enrolled students; sibling discounts and arrears carry-forward.
4. ✅ **Paynow payment gateway** — `IPaymentGatewayService` in Infrastructure. Redirect + status-poll flow with an idempotent callback handler that settles invoices. Manual recording stays for cash/bank. *(Live HTTP unverified — needs sandbox credentials.)*
5. ✅ **SMS + WhatsApp channels** — config-driven `SmsService` and a channel abstraction (`IMessageChannel`) with SMS and WhatsApp Business implementations, provider-neutral and buildable without credentials.
6. ✅ **Notification dispatch** (`Message`, `MessageRecipient`) — recipient resolution to guardians, per-recipient delivery tracking, and templates for announcements and fee reminders. *(Synchronous; resilient outbox + background worker is a follow-up.)*
7. ✅ **Report cards** — QuestPDF PDF generation from `Assessment`/`Result` data, with ZIMSEC and Cambridge grade scales and persisted, editable teacher/head comments (`ReportCardComment`, one per student per term).

### Phase 2 — Full school office — ✅ complete (with noted deferrals)

8. ✅ **Staff/HR** (`Staff`, `TeacherSubject`, `LeaveRequest`) — staff records, subject allocation, leave approve-once workflow.
9. ✅ **Timetable** (`Classroom`, `TimetableSlot`) — manual slot editor with class/teacher/classroom clash detection. *(Auto-solver out of scope.)*
10. ✅ **Discipline** (`DisciplineRecord`) with a guardian-notification hook (reuses the messaging pipeline).
11. ✅ **Parent portal API** — read-only endpoints scoped to a guardian's own children (ownership boundary): children list, results, attendance, and finance/balances with payable-invoice flags.
12. ✅ **Finance depth** — PDF receipts, defaulters report, cashier daily reconciliation, and payment plans (split an invoice balance into scheduled installments, mark installments paid, auto-complete the plan).

### Phase 3 — Differentiation — ✅ core complete (with noted deferrals)

13. ✅ **Boarding/hostel** (`Dormitory`, `House`, `WeekendLeave`) — dormitory/house CRUD and student allocation with gender + capacity enforcement, occupancy reporting, and a weekend-leave register (request → approve/reject with guardian SMS → sign-out/sign-in). *(Boarding fees ride on fee structures.)*
14. ✅ **Assets register** (`Asset`) and an **analytics dashboard** (student/staff/class counts, fee-collection rate, 30-day attendance rate, enrollment by class).
15. ✅ **Transport** (`TransportRoute`, `RouteStop`) — routes with vehicle/driver, ordered stops with pickup/dropoff times, student-to-stop assignment with route-wide (vehicle) capacity enforcement, and a `TransportOfficer` role.
16. ✅ **Library** (`Book`, `BookLoan`) — catalog with search, borrow/return (one active copy per student per title, capacity enforced against total copies), lost-copy handling that permanently reduces the catalog total, overdue-loans report, per-student loan history, and a `Librarian` role.
17. ✅ **LMS-lite** (`Assignment`, `AssignmentSubmission`) — teachers set assignments per class/subject/term with an optional file attachment (uploaded to S3 via the existing `IFileStorageService`); submissions are recorded per student (auto-flagged Late past the due date, resubmission allowed until graded), then graded with a score and feedback; a student's assignment list shows their own submission status and grade.

---

## 3. Frontend Approach

- **Stack: React + TypeScript + Vite SPA**, Tailwind CSS, TanStack Query, consuming the existing REST API. Chosen over Blazor for PWA/offline maturity and hiring pool, and over Next.js because the API already exists — no Node backend layer needed.
- **One responsive, PWA-enabled web app** with role-based navigation (admin/bursar/teacher/parent) driven by the existing `GET /api/users/me/permissions` endpoint — not separate apps per role.
- **Offline-first where it matters most:** teacher attendance marking and marks entry work offline (IndexedDB queue, sync on reconnect; attendance is naturally idempotent per student/date). Everything else degrades gracefully.
- **Low-bandwidth discipline:** small bundles, no heavy component libraries, paginated lists throughout (the API already paginates).
- **Parent access** via the same PWA — installable on Android straight from a WhatsApp-shared link. Native apps are deferred.
- **Phase 1 screens:** login/tenant select, dashboard, students + guardians, attendance register, fee invoicing + payments, results entry + report cards, messaging.

---

## 4. Architecture & Hardening

- **Tenant-isolation integration tests first** — this is the highest-risk failure mode of the product. xUnit + Testcontainers (PostgreSQL): seed two tenants and assert cross-tenant reads/writes are impossible through the API. Then handler tests for the finance flows.
- **Validators for every command** — the MediatR validation pipeline behavior already exists; this is pure gap-filling across ~23 unvalidated commands.
- **Background jobs: Hangfire** (PostgreSQL storage) — bulk invoice generation, notification outbox dispatch, and payment status polling (Paynow's poll URL is more dependable than inbound webhooks to locally hosted infrastructure).
- **Payment integrity** — idempotency keys on gateway callbacks, a `Payment` state machine over the existing `PaymentStatus` enum, and a daily reconciliation job comparing gateway records against the database.
- **Multi-currency** — add currency + exchange-rate snapshot to `Invoice`/`Payment` (migration required); report in both USD and ZWG.
- **Operations** — Docker Compose (API + PostgreSQL + Hangfire) on a VPS to start; nightly `pg_dump` shipped to S3 via the existing storage service; Serilog to file/Seq; rate limiting on auth endpoints; verify refresh-token rotation and revocation depth (endpoints exist, behavior unaudited).
- **Audit trail** — the `AuditLog` entity exists; wire it to sensitive actions (fee changes, result edits, payment records) via a MediatR pipeline behavior.
