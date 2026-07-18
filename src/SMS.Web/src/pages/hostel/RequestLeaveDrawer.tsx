import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { getErrorMessage } from '../../api/errors'
import { requestWeekendLeave } from '../../api/hostel'
import { fetchStudents } from '../../api/students'
import { Avatar } from '../../components/ui/Avatar'
import { Button } from '../../components/ui/Button'
import { Drawer } from '../../components/ui/Drawer'
import { TextareaField, TextField } from '../../components/ui/Field'
import { SearchInput } from '../../components/ui/SearchInput'
import { useDebouncedValue } from '../../hooks/useDebouncedValue'

export function RequestLeaveDrawer({ open, onClose }: { open: boolean; onClose: () => void }) {
  const queryClient = useQueryClient()
  const [search, setSearch] = useState('')
  const debouncedSearch = useDebouncedValue(search, 300)
  const [studentId, setStudentId] = useState<string | null>(null)
  const [studentLabel, setStudentLabel] = useState('')
  const [departureDate, setDepartureDate] = useState('')
  const [expectedReturnDate, setExpectedReturnDate] = useState('')
  const [destination, setDestination] = useState('')
  const [reason, setReason] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  const { data: results, isFetching } = useQuery({
    queryKey: ['students-search', debouncedSearch],
    queryFn: () => fetchStudents({ pageNumber: 1, pageSize: 6, searchTerm: debouncedSearch }),
    enabled: debouncedSearch.length >= 2 && !studentId,
  })

  function reset() {
    setSearch('')
    setStudentId(null)
    setStudentLabel('')
    setDepartureDate('')
    setExpectedReturnDate('')
    setDestination('')
    setReason('')
    setError(null)
  }

  useEffect(() => {
    if (open) reset()
  }, [open])

  function handleClose() {
    reset()
    onClose()
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!studentId) {
      setError('Search for and select a student first')
      return
    }
    setError(null)
    setSubmitting(true)
    try {
      await requestWeekendLeave({
        studentId,
        departureDate,
        expectedReturnDate,
        destination,
        reason: reason || undefined,
      })
      await queryClient.invalidateQueries({ queryKey: ['weekend-leaves'] })
      handleClose()
    } catch (err) {
      setError(getErrorMessage(err, 'Could not request leave'))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Drawer
      open={open}
      onClose={handleClose}
      title="Request weekend leave"
      description="Only boarding students (assigned to a dormitory) can be granted leave"
      footer={
        <>
          <Button variant="secondary" type="button" onClick={handleClose}>
            Cancel
          </Button>
          <Button type="submit" form="request-leave-form" loading={submitting}>
            Submit request
          </Button>
        </>
      }
    >
      <form id="request-leave-form" onSubmit={handleSubmit} className="space-y-4">
        <div>
          <span className="mb-1.5 block text-sm font-medium text-slate-700 dark:text-slate-300">
            Student <span className="text-red-500">*</span>
          </span>
          {studentId ? (
            <div className="flex items-center justify-between rounded-lg border border-brand-200 bg-brand-50 px-3 py-2 dark:border-brand-900 dark:bg-brand-950/40">
              <span className="text-sm font-medium text-brand-800 dark:text-brand-200">{studentLabel}</span>
              <button
                type="button"
                onClick={() => {
                  setStudentId(null)
                  setStudentLabel('')
                }}
                className="text-xs font-medium text-brand-600 hover:underline dark:text-brand-300"
              >
                Change
              </button>
            </div>
          ) : (
            <>
              <SearchInput
                placeholder="Search by name or student number…"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                autoFocus
              />
              {debouncedSearch.length >= 2 && (
                <div className="mt-2 max-h-40 overflow-y-auto rounded-lg border border-slate-200 dark:border-slate-800">
                  {isFetching ? (
                    <p className="px-3 py-3 text-sm text-slate-400">Searching…</p>
                  ) : results && results.items.length > 0 ? (
                    results.items.map((s) => (
                      <button
                        type="button"
                        key={s.id}
                        onClick={() => {
                          setStudentId(s.id)
                          setStudentLabel(`${s.fullName} (${s.studentNumber})`)
                        }}
                        className="flex w-full items-center gap-3 border-b border-slate-100 px-3 py-2 text-left last:border-0 hover:bg-slate-50 dark:border-slate-800 dark:hover:bg-slate-800"
                      >
                        <Avatar name={s.fullName} size="sm" />
                        <div>
                          <p className="text-sm font-medium text-slate-800 dark:text-slate-100">{s.fullName}</p>
                          <p className="text-xs text-slate-400">{s.studentNumber}</p>
                        </div>
                      </button>
                    ))
                  ) : (
                    <p className="px-3 py-3 text-sm text-slate-400">No students found.</p>
                  )}
                </div>
              )}
            </>
          )}
        </div>

        <div className="grid grid-cols-2 gap-4">
          <TextField
            label="Departure date"
            type="date"
            required
            value={departureDate}
            onChange={(e) => setDepartureDate(e.target.value)}
          />
          <TextField
            label="Expected return"
            type="date"
            required
            value={expectedReturnDate}
            onChange={(e) => setExpectedReturnDate(e.target.value)}
          />
        </div>

        <TextField
          label="Destination"
          required
          value={destination}
          onChange={(e) => setDestination(e.target.value)}
        />
        <TextareaField label="Reason" rows={2} value={reason} onChange={(e) => setReason(e.target.value)} />

        {error && (
          <p className="rounded-lg bg-red-50 px-3 py-2.5 text-sm text-red-700 dark:bg-red-950/40 dark:text-red-300">
            {error}
          </p>
        )}
      </form>
    </Drawer>
  )
}
