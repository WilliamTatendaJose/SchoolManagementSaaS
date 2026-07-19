import { useMutation, useQuery } from '@tanstack/react-query'
import {
  AlertTriangle,
  CheckCircle2,
  ChevronRight,
  CloudUpload,
  Download,
  FileSpreadsheet,
  Loader2,
  XCircle,
} from 'lucide-react'
import { useMemo, useRef, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { fetchClasses } from '../../api/classes'
import { getErrorMessage } from '../../api/errors'
import { importStudents } from '../../api/students'
import type { StudentImportResult, StudentImportRow } from '../../api/types'
import { Button } from '../../components/ui/Button'
import { PageHeader } from '../../components/ui/PageHeader'
import { parseCsv } from '../../lib/csv'

type Step = 'upload' | 'map' | 'review' | 'done'

interface FieldDef {
  key: keyof StudentImportRow
  label: string
  required?: boolean
  aliases: string[]
}

const FIELDS: FieldDef[] = [
  { key: 'firstName', label: 'First name', required: true, aliases: ['firstname', 'first name', 'first', 'givenname'] },
  { key: 'middleName', label: 'Middle name', aliases: ['middlename', 'middle name', 'middle'] },
  { key: 'lastName', label: 'Last name', required: true, aliases: ['lastname', 'last name', 'surname', 'familyname'] },
  { key: 'gender', label: 'Gender', required: true, aliases: ['gender', 'sex'] },
  { key: 'dateOfBirth', label: 'Date of birth', required: true, aliases: ['dateofbirth', 'dob', 'birthdate', 'date of birth'] },
  { key: 'nationalId', label: 'National ID', aliases: ['nationalid', 'national id', 'idnumber'] },
  { key: 'birthCertificateNumber', label: 'Birth cert. number', aliases: ['birthcertificatenumber', 'birth cert', 'birthcert'] },
  { key: 'address', label: 'Address', aliases: ['address', 'homeaddress'] },
  { key: 'city', label: 'City', aliases: ['city', 'town'] },
  { key: 'religion', label: 'Religion', aliases: ['religion'] },
  { key: 'className', label: 'Class', aliases: ['class', 'classname', 'grade', 'form'] },
  { key: 'admissionDate', label: 'Admission date', aliases: ['admissiondate', 'admission date', 'dateadmitted'] },
  { key: 'guardianFirstName', label: 'Guardian first name', aliases: ['guardianfirstname', 'guardian first name', 'parentfirstname'] },
  { key: 'guardianLastName', label: 'Guardian last name', aliases: ['guardianlastname', 'guardian last name', 'parentlastname'] },
  { key: 'guardianPhone', label: 'Guardian phone', aliases: ['guardianphone', 'guardian phone', 'parentphone', 'phone'] },
  { key: 'guardianEmail', label: 'Guardian email', aliases: ['guardianemail', 'guardian email', 'parentemail', 'email'] },
  { key: 'guardianRelationship', label: 'Relationship', aliases: ['guardianrelationship', 'relationship'] },
  { key: 'openingBalance', label: 'Opening balance', aliases: ['openingbalance', 'opening balance', 'balancebroughtforward'] },
]

function normalizeHeader(h: string) {
  return h.trim().toLowerCase().replace(/[\s_-]+/g, ' ').trim()
}

function autoDetectMapping(headers: string[]): Record<string, number> {
  const mapping: Record<string, number> = {}
  const normalized = headers.map(normalizeHeader)
  for (const field of FIELDS) {
    const idx = normalized.findIndex((h) => field.aliases.includes(h) || field.aliases.includes(h.replace(/ /g, '')))
    if (idx >= 0) mapping[field.key] = idx
  }
  return mapping
}

function downloadTemplate() {
  const header = FIELDS.map((f) => f.label).join(',')
  const example = [
    'Tinashe',
    '',
    'Moyo',
    'Male',
    '2015-03-14',
    '',
    '',
    '',
    'Harare',
    '',
    'Form 1',
    '',
    'Grace',
    'Moyo',
    '0772000111',
    '',
    'Mother',
    '',
  ].join(',')
  const blob = new Blob([`${header}\n${example}\n`], { type: 'text/csv;charset=utf-8;' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = 'student-import-template.csv'
  a.click()
  URL.revokeObjectURL(url)
}

export function ImportStudentsPage() {
  const navigate = useNavigate()
  const fileInputRef = useRef<HTMLInputElement>(null)

  const [step, setStep] = useState<Step>('upload')
  const [fileName, setFileName] = useState('')
  const [headers, setHeaders] = useState<string[]>([])
  const [dataRows, setDataRows] = useState<string[][]>([])
  const [mapping, setMapping] = useState<Record<string, number>>({})
  const [parseError, setParseError] = useState<string | null>(null)
  const [result, setResult] = useState<StudentImportResult | null>(null)
  const [showOnlyErrors, setShowOnlyErrors] = useState(false)

  const { data: classes } = useQuery({ queryKey: ['classes'], queryFn: fetchClasses })

  const rows = useMemo<StudentImportRow[]>(() => {
    return dataRows.map((cells) => {
      const row: StudentImportRow = {}
      for (const field of FIELDS) {
        const idx = mapping[field.key]
        if (idx !== undefined && idx >= 0 && cells[idx] !== undefined) {
          const value = cells[idx].trim()
          if (value) row[field.key] = value
        }
      }
      return row
    })
  }, [dataRows, mapping])

  const importMutation = useMutation({
    mutationFn: (commit: boolean) => importStudents(rows, commit),
    onSuccess: (data) => {
      setResult(data)
      setStep(data.committed ? 'done' : 'review')
    },
  })

  function handleFile(file: File) {
    setParseError(null)
    setFileName(file.name)
    const reader = new FileReader()
    reader.onload = () => {
      try {
        const text = String(reader.result ?? '')
        const parsed = parseCsv(text)
        if (parsed.length < 2) {
          setParseError('This file has no data rows below the header.')
          return
        }
        const [headerRow, ...body] = parsed
        setHeaders(headerRow)
        setDataRows(body)
        setMapping(autoDetectMapping(headerRow))
        setResult(null)
        setStep('map')
      } catch {
        setParseError('Could not read this file as CSV. Make sure it was saved as "CSV (Comma delimited)".')
      }
    }
    reader.onerror = () => setParseError('Could not read this file.')
    reader.readAsText(file)
  }

  function onDrop(e: React.DragEvent) {
    e.preventDefault()
    const file = e.dataTransfer.files?.[0]
    if (file) handleFile(file)
  }

  const requiredFieldsMapped = FIELDS.filter((f) => f.required).every((f) => mapping[f.key] !== undefined)
  const knownClassNames = new Set((classes ?? []).map((c) => c.name.toLowerCase()))

  const visibleRows = result ? (showOnlyErrors ? result.rows.filter((r) => !r.isValid) : result.rows) : []

  return (
    <div>
      <PageHeader
        title="Import students"
        description="Bring in students from a spreadsheet instead of adding them one by one."
        backTo="/students"
      />

      <div className="mb-6 flex items-center gap-2 text-sm">
        {(['upload', 'map', 'review', 'done'] as Step[]).map((s, i) => (
          <div key={s} className="flex items-center gap-2">
            <div
              className={`flex h-6 w-6 items-center justify-center rounded-full text-xs font-semibold ${
                step === s
                  ? 'bg-brand-600 text-white'
                  : (['upload', 'map', 'review', 'done'] as Step[]).indexOf(step) > i
                    ? 'bg-brand-100 text-brand-700 dark:bg-brand-900/40 dark:text-brand-300'
                    : 'bg-slate-100 text-slate-400 dark:bg-slate-800'
              }`}
            >
              {i + 1}
            </div>
            <span className={step === s ? 'font-medium text-slate-900 dark:text-white' : 'text-slate-400'}>
              {s === 'upload' ? 'Upload' : s === 'map' ? 'Map columns' : s === 'review' ? 'Review' : 'Done'}
            </span>
            {i < 3 && <ChevronRight className="h-4 w-4 text-slate-300 dark:text-slate-700" />}
          </div>
        ))}
      </div>

      {step === 'upload' && (
        <div className="rounded-2xl border border-slate-200 bg-white p-8 shadow-sm dark:border-slate-800 dark:bg-slate-900">
          <div
            onDragOver={(e) => e.preventDefault()}
            onDrop={onDrop}
            className="flex flex-col items-center justify-center rounded-xl border-2 border-dashed border-slate-300 px-6 py-16 text-center dark:border-slate-700"
          >
            <div className="flex h-12 w-12 items-center justify-center rounded-2xl bg-brand-50 dark:bg-brand-900/30">
              <CloudUpload className="h-6 w-6 text-brand-600 dark:text-brand-300" strokeWidth={1.75} />
            </div>
            <h3 className="mt-4 text-sm font-semibold text-slate-900 dark:text-white">
              Drop a CSV file here, or click to browse
            </h3>
            <p className="mt-1 max-w-sm text-sm text-slate-500 dark:text-slate-400">
              Export your spreadsheet as CSV first (File → Save As → CSV in Excel or Google Sheets).
            </p>
            <input
              ref={fileInputRef}
              type="file"
              accept=".csv,text/csv"
              className="hidden"
              onChange={(e) => {
                const file = e.target.files?.[0]
                if (file) handleFile(file)
              }}
            />
            <Button className="mt-5" onClick={() => fileInputRef.current?.click()}>
              <FileSpreadsheet className="h-4 w-4" strokeWidth={2.5} />
              Choose file
            </Button>
            {fileName && !parseError && <p className="mt-3 text-xs text-slate-400">{fileName}</p>}
            {parseError && <p className="mt-3 text-sm text-red-600 dark:text-red-400">{parseError}</p>}
          </div>

          <div className="mt-6 flex items-center justify-between rounded-xl bg-slate-50 px-4 py-3 text-sm dark:bg-slate-800/60">
            <span className="text-slate-600 dark:text-slate-300">
              Not sure of the format? Download a template with the columns we recognize.
            </span>
            <Button variant="secondary" onClick={downloadTemplate}>
              <Download className="h-4 w-4" strokeWidth={2} />
              Template
            </Button>
          </div>
        </div>
      )}

      {step === 'map' && (
        <div className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm dark:border-slate-800 dark:bg-slate-900">
          <p className="mb-4 text-sm text-slate-500 dark:text-slate-400">
            {dataRows.length} row{dataRows.length === 1 ? '' : 's'} found in <span className="font-medium">{fileName}</span>.
            Match each field to a column from your file. First name, last name, gender and date of birth are required.
          </p>

          <div className="overflow-hidden rounded-xl border border-slate-200 dark:border-slate-800">
            <table className="w-full text-left text-sm">
              <thead>
                <tr className="border-b border-slate-200 bg-slate-50 text-xs uppercase tracking-wider text-slate-400 dark:border-slate-800 dark:bg-slate-800/60">
                  <th className="px-4 py-2.5 font-medium">Field</th>
                  <th className="px-4 py-2.5 font-medium">Spreadsheet column</th>
                  <th className="px-4 py-2.5 font-medium">Preview</th>
                </tr>
              </thead>
              <tbody>
                {FIELDS.map((field) => {
                  const idx = mapping[field.key]
                  const preview = idx !== undefined && idx >= 0 ? dataRows[0]?.[idx] : undefined
                  return (
                    <tr key={field.key} className="border-b border-slate-100 last:border-0 dark:border-slate-800/60">
                      <td className="px-4 py-2.5">
                        <span className="font-medium text-slate-700 dark:text-slate-200">{field.label}</span>
                        {field.required && <span className="ml-1 text-red-500">*</span>}
                      </td>
                      <td className="px-4 py-2.5">
                        <select
                          value={idx ?? -1}
                          onChange={(e) =>
                            setMapping((m) => ({ ...m, [field.key]: Number(e.target.value) }))
                          }
                          className="rounded-lg border border-slate-300 bg-white px-2.5 py-1.5 text-sm text-slate-700 focus:border-brand-500 focus:outline-none focus:ring-4 focus:ring-brand-500/10 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200"
                        >
                          <option value={-1}>— Not mapped —</option>
                          {headers.map((h, i) => (
                            <option key={i} value={i}>
                              {h || `Column ${i + 1}`}
                            </option>
                          ))}
                        </select>
                      </td>
                      <td className="px-4 py-2.5 text-slate-400">{preview || '—'}</td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>

          <div className="mt-6 flex items-center justify-between">
            <Button variant="ghost" onClick={() => setStep('upload')}>
              Back
            </Button>
            <Button
              disabled={!requiredFieldsMapped}
              loading={importMutation.isPending}
              onClick={() => importMutation.mutate(false)}
            >
              Validate {dataRows.length} row{dataRows.length === 1 ? '' : 's'}
              <ChevronRight className="h-4 w-4" strokeWidth={2.5} />
            </Button>
          </div>
          {!requiredFieldsMapped && (
            <p className="mt-2 text-right text-xs text-amber-600 dark:text-amber-400">
              Map all required fields to continue.
            </p>
          )}
          {importMutation.isError && (
            <p className="mt-2 text-right text-sm text-red-600 dark:text-red-400">
              {getErrorMessage(importMutation.error, 'Validation failed.')}
            </p>
          )}
        </div>
      )}

      {step === 'review' && result && (
        <div className="space-y-4">
          <div className="grid grid-cols-2 gap-4 sm:grid-cols-4">
            <SummaryTile label="Total rows" value={result.totalRows} tone="slate" />
            <SummaryTile label="Ready to import" value={result.validRows} tone="emerald" />
            <SummaryTile label="Need fixing" value={result.errorRows} tone={result.errorRows > 0 ? 'red' : 'slate'} />
            <SummaryTile
              label="Classes not found"
              value={result.rows.filter((r) => r.errors.some((e) => e.includes('not found'))).length}
              tone="amber"
            />
          </div>

          {result.errorRows > 0 ? (
            <div className="flex items-start gap-3 rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-700 dark:border-red-900/50 dark:bg-red-950/30 dark:text-red-300">
              <XCircle className="mt-0.5 h-4 w-4 shrink-0" strokeWidth={2} />
              <span>
                {result.errorRows} row{result.errorRows === 1 ? '' : 's'} can't be imported yet. Fix your source file
                and re-upload, or go back and adjust the column mapping — nothing has been saved.
              </span>
            </div>
          ) : (
            <div className="flex items-start gap-3 rounded-xl border border-emerald-200 bg-emerald-50 p-4 text-sm text-emerald-700 dark:border-emerald-900/50 dark:bg-emerald-950/30 dark:text-emerald-300">
              <CheckCircle2 className="mt-0.5 h-4 w-4 shrink-0" strokeWidth={2} />
              <span>Every row is valid. Nothing has been saved yet — review the list below, then commit the import.</span>
            </div>
          )}

          {knownClassNames.size === 0 && (
            <div className="flex items-start gap-3 rounded-xl border border-amber-200 bg-amber-50 p-4 text-sm text-amber-700 dark:border-amber-900/50 dark:bg-amber-950/30 dark:text-amber-300">
              <AlertTriangle className="mt-0.5 h-4 w-4 shrink-0" strokeWidth={2} />
              <span>No classes are set up yet, so class names in your file won't resolve. Set up classes first if you want students enrolled automatically.</span>
            </div>
          )}

          <div className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm dark:border-slate-800 dark:bg-slate-900">
            <div className="flex items-center justify-between border-b border-slate-200 px-4 py-3 dark:border-slate-800">
              <span className="text-sm font-medium text-slate-700 dark:text-slate-200">Row-by-row report</span>
              <label className="flex items-center gap-2 text-sm text-slate-500 dark:text-slate-400">
                <input
                  type="checkbox"
                  checked={showOnlyErrors}
                  onChange={(e) => setShowOnlyErrors(e.target.checked)}
                  className="rounded border-slate-300 text-brand-600 focus:ring-brand-500"
                />
                Show only rows with errors
              </label>
            </div>
            <div className="max-h-[28rem] overflow-y-auto">
              <table className="w-full text-left text-sm">
                <thead className="sticky top-0 bg-white dark:bg-slate-900">
                  <tr className="border-b border-slate-200 text-xs uppercase tracking-wider text-slate-400 dark:border-slate-800">
                    <th className="px-4 py-2.5 font-medium">Row</th>
                    <th className="px-4 py-2.5 font-medium">Student</th>
                    <th className="px-4 py-2.5 font-medium">Class</th>
                    <th className="px-4 py-2.5 font-medium">Guardian</th>
                    <th className="px-4 py-2.5 font-medium">Notes</th>
                  </tr>
                </thead>
                <tbody>
                  {visibleRows.map((r) => (
                    <tr key={r.rowNumber} className="border-b border-slate-100 last:border-0 dark:border-slate-800/60">
                      <td className="px-4 py-2.5 text-slate-400">{r.rowNumber}</td>
                      <td className="px-4 py-2.5">
                        <div className="flex items-center gap-2">
                          {r.isValid ? (
                            <CheckCircle2 className="h-4 w-4 shrink-0 text-emerald-500" strokeWidth={2} />
                          ) : (
                            <XCircle className="h-4 w-4 shrink-0 text-red-500" strokeWidth={2} />
                          )}
                          <span className="text-slate-700 dark:text-slate-200">{r.studentName || '—'}</span>
                        </div>
                      </td>
                      <td className="px-4 py-2.5 text-slate-500 dark:text-slate-400">{r.resolvedClass ?? '—'}</td>
                      <td className="px-4 py-2.5 text-slate-500 dark:text-slate-400">
                        {r.guardianAction === 'create' ? 'New' : r.guardianAction === 'link-existing' ? 'Linked' : '—'}
                      </td>
                      <td className="px-4 py-2.5">
                        {r.errors.map((e, i) => (
                          <p key={i} className="text-red-600 dark:text-red-400">
                            {e}
                          </p>
                        ))}
                        {r.warnings.map((w, i) => (
                          <p key={i} className="text-amber-600 dark:text-amber-400">
                            {w}
                          </p>
                        ))}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>

          <div className="flex items-center justify-between">
            <Button variant="ghost" onClick={() => setStep('map')}>
              Back to mapping
            </Button>
            <div className="flex items-center gap-2">
              <Button variant="secondary" loading={importMutation.isPending} onClick={() => importMutation.mutate(false)}>
                Re-validate
              </Button>
              <Button
                disabled={result.errorRows > 0}
                loading={importMutation.isPending}
                onClick={() => importMutation.mutate(true)}
              >
                Commit import
              </Button>
            </div>
          </div>
        </div>
      )}

      {step === 'done' && result && (
        <div className="rounded-2xl border border-slate-200 bg-white p-8 text-center shadow-sm dark:border-slate-800 dark:bg-slate-900">
          <div className="mx-auto flex h-14 w-14 items-center justify-center rounded-2xl bg-emerald-50 dark:bg-emerald-900/30">
            <CheckCircle2 className="h-7 w-7 text-emerald-600 dark:text-emerald-300" strokeWidth={1.75} />
          </div>
          <h3 className="mt-4 text-lg font-semibold text-slate-900 dark:text-white">Import complete</h3>
          <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">
            {result.studentsCreated} student{result.studentsCreated === 1 ? '' : 's'} added.
          </p>

          <div className="mx-auto mt-6 grid max-w-md grid-cols-2 gap-3 text-left">
            <SummaryTile label="Students" value={result.studentsCreated} tone="brand" />
            <SummaryTile label="Enrollments" value={result.enrollmentsCreated} tone="brand" />
            <SummaryTile label="New guardians" value={result.guardiansCreated} tone="slate" />
            <SummaryTile label="Linked guardians" value={result.guardiansLinked} tone="slate" />
            {result.openingBalanceInvoices > 0 && (
              <SummaryTile label="Opening invoices" value={result.openingBalanceInvoices} tone="amber" />
            )}
          </div>

          <Button className="mt-8" onClick={() => navigate('/students')}>
            Go to students
          </Button>
        </div>
      )}

      {importMutation.isPending && step !== 'map' && (
        <div className="mt-4 flex items-center justify-center gap-2 text-sm text-slate-400">
          <Loader2 className="h-4 w-4 animate-spin" strokeWidth={2.5} />
          Working…
        </div>
      )}
    </div>
  )
}

function SummaryTile({
  label,
  value,
  tone,
}: {
  label: string
  value: number
  tone: 'slate' | 'emerald' | 'red' | 'amber' | 'brand'
}) {
  const toneClasses: Record<typeof tone, string> = {
    slate: 'bg-slate-50 text-slate-700 dark:bg-slate-800/60 dark:text-slate-200',
    emerald: 'bg-emerald-50 text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-300',
    red: 'bg-red-50 text-red-700 dark:bg-red-900/30 dark:text-red-300',
    amber: 'bg-amber-50 text-amber-700 dark:bg-amber-900/30 dark:text-amber-300',
    brand: 'bg-brand-50 text-brand-700 dark:bg-brand-900/30 dark:text-brand-300',
  }
  return (
    <div className={`rounded-xl px-4 py-3 ${toneClasses[tone]}`}>
      <p className="text-2xl font-semibold tracking-tight">{value}</p>
      <p className="text-xs font-medium opacity-80">{label}</p>
    </div>
  )
}
