import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { getErrorMessage } from '../../api/errors'
import { createStaff, updateStaff } from '../../api/staff'
import type { StaffDetailDto } from '../../api/types'
import { fetchUsers } from '../../api/users'
import { Avatar } from '../../components/ui/Avatar'
import { Button } from '../../components/ui/Button'
import { Drawer } from '../../components/ui/Drawer'
import { SearchInput } from '../../components/ui/SearchInput'
import { TextField } from '../../components/ui/Field'
import { useDebouncedValue } from '../../hooks/useDebouncedValue'

interface StaffFormDrawerProps {
  open: boolean
  onClose: () => void
  /** Present for edit mode; omitted for create. */
  staff?: StaffDetailDto
}

interface FormState {
  department: string
  jobTitle: string
  dateOfJoining: string
  qualifications: string
  specialization: string
  isTeacher: boolean
  isActive: boolean
}

function emptyForm(): FormState {
  return {
    department: '',
    jobTitle: '',
    dateOfJoining: new Date().toISOString().slice(0, 10),
    qualifications: '',
    specialization: '',
    isTeacher: true,
    isActive: true,
  }
}

function formFromStaff(s: StaffDetailDto): FormState {
  return {
    department: s.department ?? '',
    jobTitle: s.jobTitle ?? '',
    dateOfJoining: s.dateOfJoining?.slice(0, 10) ?? '',
    qualifications: s.qualifications ?? '',
    specialization: s.specialization ?? '',
    isTeacher: s.isTeacher,
    isActive: s.isActive,
  }
}

export function StaffFormDrawer({ open, onClose, staff }: StaffFormDrawerProps) {
  const isEdit = !!staff
  const queryClient = useQueryClient()
  const navigate = useNavigate()
  const [form, setForm] = useState<FormState>(staff ? formFromStaff(staff) : emptyForm())
  const [userId, setUserId] = useState<string | null>(null)
  const [userLabel, setUserLabel] = useState('')
  const [userSearch, setUserSearch] = useState('')
  const debouncedUserSearch = useDebouncedValue(userSearch, 300)
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  const { data: userResults, isFetching: searchingUsers } = useQuery({
    queryKey: ['users-search', debouncedUserSearch],
    queryFn: () => fetchUsers({ pageNumber: 1, pageSize: 6, searchTerm: debouncedUserSearch }),
    enabled: !isEdit && debouncedUserSearch.length >= 2 && !userId,
  })

  useEffect(() => {
    if (open) {
      setForm(staff ? formFromStaff(staff) : emptyForm())
      setUserId(null)
      setUserLabel('')
      setUserSearch('')
      setError(null)
    }
  }, [open, staff])

  function set<K extends keyof FormState>(key: K, value: FormState[K]) {
    setForm((f) => ({ ...f, [key]: value }))
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!isEdit && !userId) {
      setError('Search for and select a user account first')
      return
    }
    setError(null)
    setSubmitting(true)

    try {
      if (isEdit && staff) {
        await updateStaff({
          id: staff.id,
          department: form.department || undefined,
          jobTitle: form.jobTitle || undefined,
          dateOfJoining: form.dateOfJoining || undefined,
          qualifications: form.qualifications || undefined,
          specialization: form.specialization || undefined,
          isTeacher: form.isTeacher,
          isActive: form.isActive,
        })
        await queryClient.invalidateQueries({ queryKey: ['staff-member', staff.id] })
        await queryClient.invalidateQueries({ queryKey: ['staff'] })
        onClose()
      } else {
        const created = await createStaff({
          userId: userId!,
          department: form.department || undefined,
          jobTitle: form.jobTitle || undefined,
          dateOfJoining: form.dateOfJoining || undefined,
          qualifications: form.qualifications || undefined,
          specialization: form.specialization || undefined,
          isTeacher: form.isTeacher,
        })
        await queryClient.invalidateQueries({ queryKey: ['staff'] })
        onClose()
        navigate(`/staff/${created.id}`)
      }
    } catch (err) {
      setError(getErrorMessage(err, isEdit ? 'Could not update staff member' : 'Could not create staff member'))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Drawer
      open={open}
      onClose={onClose}
      title={isEdit ? 'Edit staff member' : 'Add staff'}
      description={isEdit ? staff?.fullName : 'Create a staff profile for an existing user account'}
      footer={
        <>
          <Button variant="secondary" onClick={onClose} type="button">
            Cancel
          </Button>
          <Button type="submit" form="staff-form" loading={submitting}>
            {isEdit ? 'Save changes' : 'Create staff profile'}
          </Button>
        </>
      }
    >
      <form id="staff-form" onSubmit={handleSubmit} className="space-y-4">
        {!isEdit && (
          <div>
            <span className="mb-1.5 block text-sm font-medium text-slate-700 dark:text-slate-300">
              User account <span className="text-red-500">*</span>
            </span>
            {userId ? (
              <div className="flex items-center justify-between rounded-lg border border-brand-200 bg-brand-50 px-3 py-2 dark:border-brand-900 dark:bg-brand-950/40">
                <span className="text-sm font-medium text-brand-800 dark:text-brand-200">{userLabel}</span>
                <button
                  type="button"
                  onClick={() => {
                    setUserId(null)
                    setUserLabel('')
                  }}
                  className="text-xs font-medium text-brand-600 hover:underline dark:text-brand-300"
                >
                  Change
                </button>
              </div>
            ) : (
              <>
                <SearchInput
                  placeholder="Search by name or email…"
                  value={userSearch}
                  onChange={(e) => setUserSearch(e.target.value)}
                />
                {debouncedUserSearch.length >= 2 && (
                  <div className="mt-2 max-h-48 overflow-y-auto rounded-lg border border-slate-200 dark:border-slate-800">
                    {searchingUsers ? (
                      <p className="px-3 py-3 text-sm text-slate-400">Searching…</p>
                    ) : userResults && userResults.items.length > 0 ? (
                      userResults.items.map((u) => (
                        <button
                          type="button"
                          key={u.id}
                          onClick={() => {
                            setUserId(u.id)
                            setUserLabel(`${u.fullName} (${u.email})`)
                          }}
                          className="flex w-full items-center gap-3 border-b border-slate-100 px-3 py-2 text-left last:border-0 hover:bg-slate-50 dark:border-slate-800 dark:hover:bg-slate-800"
                        >
                          <Avatar name={u.fullName} size="sm" />
                          <div>
                            <p className="text-sm font-medium text-slate-800 dark:text-slate-100">{u.fullName}</p>
                            <p className="text-xs text-slate-400">{u.email}</p>
                          </div>
                        </button>
                      ))
                    ) : (
                      <p className="px-3 py-3 text-sm text-slate-400">No users found.</p>
                    )}
                  </div>
                )}
                <p className="mt-1.5 text-xs text-slate-400">
                  Staff profiles attach to an existing user account. Create the login under Users &amp; roles first if
                  it doesn't exist yet.
                </p>
              </>
            )}
          </div>
        )}

        <div className="grid grid-cols-2 gap-4">
          <TextField
            label="Department"
            value={form.department}
            onChange={(e) => set('department', e.target.value)}
          />
          <TextField label="Job title" value={form.jobTitle} onChange={(e) => set('jobTitle', e.target.value)} />
        </div>

        <div className="grid grid-cols-2 gap-4">
          <TextField
            label="Date of joining"
            type="date"
            value={form.dateOfJoining}
            onChange={(e) => set('dateOfJoining', e.target.value)}
          />
          <TextField
            label="Specialization"
            value={form.specialization}
            onChange={(e) => set('specialization', e.target.value)}
          />
        </div>

        <TextField
          label="Qualifications"
          value={form.qualifications}
          onChange={(e) => set('qualifications', e.target.value)}
        />

        <div className="space-y-2.5">
          <label className="flex items-center gap-2.5 text-sm text-slate-700 dark:text-slate-300">
            <input
              type="checkbox"
              checked={form.isTeacher}
              onChange={(e) => set('isTeacher', e.target.checked)}
              className="h-4 w-4 rounded border-slate-300 text-brand-600 focus:ring-brand-500"
            />
            Teaching staff
          </label>
          {isEdit && (
            <label className="flex items-center gap-2.5 text-sm text-slate-700 dark:text-slate-300">
              <input
                type="checkbox"
                checked={form.isActive}
                onChange={(e) => set('isActive', e.target.checked)}
                className="h-4 w-4 rounded border-slate-300 text-brand-600 focus:ring-brand-500"
              />
              Active
            </label>
          )}
        </div>

        {error && (
          <p className="rounded-lg bg-red-50 px-3 py-2.5 text-sm text-red-700 dark:bg-red-950/40 dark:text-red-300">
            {error}
          </p>
        )}
      </form>
    </Drawer>
  )
}
