import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { createDisciplineRecord } from '../../api/discipline'
import { getErrorMessage } from '../../api/errors'
import { fetchStudents } from '../../api/students'
import { Avatar } from '../../components/ui/Avatar'
import { Button } from '../../components/ui/Button'
import { Drawer } from '../../components/ui/Drawer'
import { SelectField, TextareaField, TextField } from '../../components/ui/Field'
import { SearchInput } from '../../components/ui/SearchInput'
import { useDebouncedValue } from '../../hooks/useDebouncedValue'

const INCIDENT_TYPES = ['Bullying', 'Fighting', 'Disruption', 'Cheating', 'Property damage', 'Uniform violation', 'Merit award']
const OTHER = 'Other'

export function CreateDisciplineDrawer({ open, onClose }: { open: boolean; onClose: () => void }) {
  const queryClient = useQueryClient()
  const [search, setSearch] = useState('')
  const debouncedSearch = useDebouncedValue(search, 300)
  const [studentId, setStudentId] = useState<string | null>(null)
  const [studentLabel, setStudentLabel] = useState('')
  const [incidentDate, setIncidentDate] = useState(new Date().toISOString().slice(0, 10))
  const [incidentType, setIncidentType] = useState(INCIDENT_TYPES[0])
  const [customIncidentType, setCustomIncidentType] = useState('')
  const [description, setDescription] = useState('')
  const [actionTaken, setActionTaken] = useState('')
  const [demerits, setDemerits] = useState('')
  const [merits, setMerits] = useState('')
  const [notifyGuardian, setNotifyGuardian] = useState(true)
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
    setIncidentDate(new Date().toISOString().slice(0, 10))
    setIncidentType(INCIDENT_TYPES[0])
    setCustomIncidentType('')
    setDescription('')
    setActionTaken('')
    setDemerits('')
    setMerits('')
    setNotifyGuardian(true)
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
    const resolvedIncidentType = incidentType === OTHER ? customIncidentType.trim() : incidentType
    if (!resolvedIncidentType) {
      setError('Specify the incident type')
      return
    }
    setError(null)
    setSubmitting(true)
    try {
      await createDisciplineRecord({
        studentId,
        incidentDate,
        incidentType: resolvedIncidentType,
        description,
        actionTaken: actionTaken || undefined,
        demeritsAwarded: demerits ? Number(demerits) : undefined,
        meritsAwarded: merits ? Number(merits) : undefined,
        notifyGuardian,
      })
      await queryClient.invalidateQueries({ queryKey: ['discipline'] })
      handleClose()
    } catch (err) {
      setError(getErrorMessage(err, 'Could not save discipline record'))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Drawer
      open={open}
      onClose={handleClose}
      title="Record incident"
      description="Log a discipline incident or merit award for a student"
      footer={
        <>
          <Button variant="secondary" type="button" onClick={handleClose}>
            Cancel
          </Button>
          <Button type="submit" form="discipline-form" loading={submitting}>
            Save record
          </Button>
        </>
      }
    >
      <form id="discipline-form" onSubmit={handleSubmit} className="space-y-4">
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
                <div className="mt-2 max-h-48 overflow-y-auto rounded-lg border border-slate-200 dark:border-slate-800">
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
            label="Incident date"
            type="date"
            required
            value={incidentDate}
            onChange={(e) => setIncidentDate(e.target.value)}
          />
          <SelectField
            label="Incident type"
            required
            value={incidentType}
            onChange={(e) => setIncidentType(e.target.value)}
          >
            {INCIDENT_TYPES.map((t) => (
              <option key={t} value={t}>
                {t}
              </option>
            ))}
            <option value={OTHER}>Other…</option>
          </SelectField>
        </div>

        {incidentType === OTHER && (
          <TextField
            label="Specify incident type"
            required
            autoFocus
            maxLength={100}
            value={customIncidentType}
            onChange={(e) => setCustomIncidentType(e.target.value)}
            placeholder="e.g. Vandalism, Truancy…"
          />
        )}

        <TextareaField
          label="Description"
          required
          rows={3}
          maxLength={1000}
          value={description}
          onChange={(e) => setDescription(e.target.value)}
        />

        <TextareaField
          label="Action taken"
          rows={2}
          value={actionTaken}
          onChange={(e) => setActionTaken(e.target.value)}
        />

        <div className="grid grid-cols-2 gap-4">
          <TextField
            label="Demerits"
            type="number"
            min={0}
            value={demerits}
            onChange={(e) => setDemerits(e.target.value)}
          />
          <TextField
            label="Merits"
            type="number"
            min={0}
            value={merits}
            onChange={(e) => setMerits(e.target.value)}
          />
        </div>

        <label className="flex items-center gap-2 text-sm text-slate-600 dark:text-slate-300">
          <input
            type="checkbox"
            checked={notifyGuardian}
            onChange={(e) => setNotifyGuardian(e.target.checked)}
            className="h-4 w-4 rounded border-slate-300 text-brand-600 focus:ring-brand-500"
          />
          Notify guardian by SMS
        </label>

        {error && (
          <p className="rounded-lg bg-red-50 px-3 py-2.5 text-sm text-red-700 dark:bg-red-950/40 dark:text-red-300">
            {error}
          </p>
        )}
      </form>
    </Drawer>
  )
}
