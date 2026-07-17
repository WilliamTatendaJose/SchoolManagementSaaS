import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import {
  GraduationCap,
  Mail,
  Lock,
  Building2,
  Loader2,
  AlertCircle,
  Wallet,
  CalendarCheck,
  MessageSquareText,
} from 'lucide-react'
import { apiClient } from '../api/client'
import { useAuthStore } from '../auth/authStore'
import type { LoginResponse, TenantInfo } from '../api/types'

const highlights = [
  { icon: Wallet, text: 'Fees, invoicing and Paynow collections in one place' },
  { icon: CalendarCheck, text: 'Attendance, timetables and results, always in sync' },
  { icon: MessageSquareText, text: 'SMS & WhatsApp updates straight to every guardian' },
]

export function LoginPage() {
  const navigate = useNavigate()
  const setTenant = useAuthStore((s) => s.setTenant)
  const setSession = useAuthStore((s) => s.setSession)
  const storedTenantId = useAuthStore((s) => s.tenantId)

  const [tenantId, setTenantId] = useState(storedTenantId ?? '')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  const { data: tenants, isLoading: tenantsLoading } = useQuery({
    queryKey: ['tenants'],
    queryFn: async () => {
      const { data } = await apiClient.get<TenantInfo[]>('/auth/tenants')
      return data
    },
  })

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError(null)

    if (!tenantId) {
      setError('Select your school to continue')
      return
    }

    setSubmitting(true)
    try {
      const { data } = await apiClient.post<LoginResponse>('/auth/login', {
        tenantId,
        email,
        password,
      })

      const tenant = tenants?.find((t) => t.id === tenantId)
      setTenant(tenantId, tenant?.name ?? '')
      setSession(data.accessToken, data.refreshToken, data.user)
      navigate('/dashboard', { replace: true })
    } catch {
      setError('Invalid email or password')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="flex min-h-screen bg-white dark:bg-slate-950">
      {/* Brand panel */}
      <div className="relative hidden w-[46%] flex-col justify-between overflow-hidden bg-gradient-to-br from-brand-950 via-brand-900 to-brand-700 p-12 text-white lg:flex">
        <div
          className="pointer-events-none absolute inset-0 opacity-[0.07]"
          style={{
            backgroundImage:
              'radial-gradient(circle at 1px 1px, white 1px, transparent 0)',
            backgroundSize: '28px 28px',
          }}
        />
        <div
          className="pointer-events-none absolute -right-32 -top-32 h-96 w-96 rounded-full bg-brand-400/20 blur-3xl"
          aria-hidden
        />
        <div
          className="pointer-events-none absolute -bottom-40 -left-20 h-96 w-96 rounded-full bg-brand-300/10 blur-3xl"
          aria-hidden
        />

        <div className="relative flex items-center gap-3">
          <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-white/10 ring-1 ring-white/20 backdrop-blur">
            <GraduationCap className="h-6 w-6 text-white" strokeWidth={2.25} />
          </div>
          <span className="text-lg font-semibold tracking-tight">School SMS</span>
        </div>

        <div className="relative max-w-md">
          <h1 className="text-3xl font-semibold leading-tight tracking-tight text-white">
            Run your school with clarity, from fees to results.
          </h1>
          <p className="mt-4 text-sm leading-relaxed text-brand-200/90">
            Built for Zimbabwean schools — multi-currency billing, offline-tolerant
            attendance, and a single place for every term.
          </p>

          <ul className="mt-10 space-y-4">
            {highlights.map(({ icon: Icon, text }) => (
              <li key={text} className="flex items-start gap-3">
                <div className="mt-0.5 flex h-8 w-8 shrink-0 items-center justify-center rounded-lg bg-white/10 ring-1 ring-white/10">
                  <Icon className="h-4 w-4 text-brand-200" strokeWidth={2} />
                </div>
                <span className="text-sm text-brand-100/90">{text}</span>
              </li>
            ))}
          </ul>
        </div>

        <p className="relative text-xs text-brand-300/60">
          &copy; {new Date().getFullYear()} School Management SaaS
        </p>
      </div>

      {/* Form panel */}
      <div className="flex flex-1 items-center justify-center px-6 py-12">
        <div className="w-full max-w-sm">
          <div className="mb-8 flex items-center gap-2.5 lg:hidden">
            <div className="flex h-9 w-9 items-center justify-center rounded-lg bg-gradient-to-br from-brand-500 to-brand-700">
              <GraduationCap className="h-5 w-5 text-white" strokeWidth={2.25} />
            </div>
            <span className="text-base font-semibold text-slate-900 dark:text-white">School SMS</span>
          </div>

          <h2 className="text-2xl font-semibold tracking-tight text-slate-900 dark:text-white">
            Welcome back
          </h2>
          <p className="mt-1.5 text-sm text-slate-500 dark:text-slate-400">
            Sign in to your school's workspace.
          </p>

          <form onSubmit={handleSubmit} className="mt-8 space-y-4">
            <div>
              <label htmlFor="tenant" className="mb-1.5 block text-sm font-medium text-slate-700 dark:text-slate-300">
                School
              </label>
              <div className="relative">
                <Building2 className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" strokeWidth={2} />
                <select
                  id="tenant"
                  value={tenantId}
                  onChange={(e) => setTenantId(e.target.value)}
                  disabled={tenantsLoading}
                  className="w-full appearance-none rounded-lg border border-slate-300 bg-white py-2.5 pl-10 pr-3 text-sm text-slate-900 transition-shadow focus:border-brand-500 focus:outline-none focus:ring-4 focus:ring-brand-500/10 dark:border-slate-700 dark:bg-slate-900 dark:text-white"
                >
                  <option value="">Select a school…</option>
                  {tenants?.map((t) => (
                    <option key={t.id} value={t.id}>
                      {t.name}
                    </option>
                  ))}
                </select>
              </div>
            </div>

            <div>
              <label htmlFor="email" className="mb-1.5 block text-sm font-medium text-slate-700 dark:text-slate-300">
                Email
              </label>
              <div className="relative">
                <Mail className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" strokeWidth={2} />
                <input
                  id="email"
                  type="email"
                  autoComplete="username"
                  required
                  placeholder="you@school.co.zw"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  className="w-full rounded-lg border border-slate-300 bg-white py-2.5 pl-10 pr-3 text-sm text-slate-900 placeholder:text-slate-400 transition-shadow focus:border-brand-500 focus:outline-none focus:ring-4 focus:ring-brand-500/10 dark:border-slate-700 dark:bg-slate-900 dark:text-white"
                />
              </div>
            </div>

            <div>
              <label htmlFor="password" className="mb-1.5 block text-sm font-medium text-slate-700 dark:text-slate-300">
                Password
              </label>
              <div className="relative">
                <Lock className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" strokeWidth={2} />
                <input
                  id="password"
                  type="password"
                  autoComplete="current-password"
                  required
                  placeholder="••••••••"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  className="w-full rounded-lg border border-slate-300 bg-white py-2.5 pl-10 pr-3 text-sm text-slate-900 placeholder:text-slate-400 transition-shadow focus:border-brand-500 focus:outline-none focus:ring-4 focus:ring-brand-500/10 dark:border-slate-700 dark:bg-slate-900 dark:text-white"
                />
              </div>
            </div>

            {error && (
              <div className="flex items-center gap-2 rounded-lg bg-red-50 px-3 py-2.5 text-sm text-red-700 dark:bg-red-950/40 dark:text-red-300">
                <AlertCircle className="h-4 w-4 shrink-0" strokeWidth={2} />
                {error}
              </div>
            )}

            <button
              type="submit"
              disabled={submitting}
              className="flex w-full items-center justify-center gap-2 rounded-lg bg-gradient-to-br from-brand-600 to-brand-700 px-3 py-2.5 text-sm font-semibold text-white shadow-lg shadow-brand-600/20 transition-all hover:shadow-brand-600/30 hover:brightness-110 active:scale-[0.99] disabled:cursor-not-allowed disabled:opacity-60"
            >
              {submitting && <Loader2 className="h-4 w-4 animate-spin" strokeWidth={2.5} />}
              {submitting ? 'Signing in…' : 'Sign in'}
            </button>
          </form>
        </div>
      </div>
    </div>
  )
}
