import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { fetchClasses } from '../../api/classes'
import { getErrorMessage } from '../../api/errors'
import { createStudent, updateStudent } from '../../api/students'
import { GENDERS, type StudentDetailDto } from '../../api/types'
import { Button } from '../../components/ui/Button'
import { Drawer } from '../../components/ui/Drawer'
import { SelectField, TextField, TextareaField } from '../../components/ui/Field'

interface StudentFormDrawerProps {
  open: boolean
  onClose: () => void
  /** Present for edit mode; omitted for create. */
  student?: StudentDetailDto
}

interface FormState {
  firstName: string
  middleName: string
  lastName: string
  dateOfBirth: string
  gender: string
  nationalId: string
  birthCertificateNumber: string
  address: string
  city: string
  nationality: string
  religion: string
  previousSchool: string
  medicalNotes: string
  specialNeeds: string
  classId: string
  admissionDate: string
}

function emptyForm(): FormState {
  return {
    firstName: '',
    middleName: '',
    lastName: '',
    dateOfBirth: '',
    gender: 'Male',
    nationalId: '',
    birthCertificateNumber: '',
    address: '',
    city: '',
    nationality: 'Zimbabwean',
    religion: '',
    previousSchool: '',
    medicalNotes: '',
    specialNeeds: '',
    classId: '',
    admissionDate: new Date().toISOString().slice(0, 10),
  }
}

function formFromStudent(s: StudentDetailDto): FormState {
  return {
    firstName: s.firstName,
    middleName: s.middleName,
    lastName: s.lastName,
    dateOfBirth: s.dateOfBirth.slice(0, 10),
    gender: s.gender,
    nationalId: s.nationalId ?? '',
    birthCertificateNumber: s.birthCertificateNumber ?? '',
    address: s.address ?? '',
    city: s.city ?? '',
    nationality: s.nationality ?? '',
    religion: s.religion ?? '',
    previousSchool: '',
    medicalNotes: s.medicalNotes ?? '',
    specialNeeds: s.specialNeeds ?? '',
    classId: s.currentClassId ?? '',
    admissionDate: s.admissionDate.slice(0, 10),
  }
}

export function StudentFormDrawer({ open, onClose, student }: StudentFormDrawerProps) {
  const isEdit = !!student
  const queryClient = useQueryClient()
  const navigate = useNavigate()
  const [form, setForm] = useState<FormState>(student ? formFromStudent(student) : emptyForm())
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  const { data: classes } = useQuery({ queryKey: ['classes'], queryFn: fetchClasses, enabled: open })

  useEffect(() => {
    if (open) {
      setForm(student ? formFromStudent(student) : emptyForm())
      setError(null)
    }
  }, [open, student])

  function set<K extends keyof FormState>(key: K, value: FormState[K]) {
    setForm((f) => ({ ...f, [key]: value }))
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    setSubmitting(true)

    try {
      if (isEdit && student) {
        await updateStudent({
          id: student.id,
          firstName: form.firstName,
          middleName: form.middleName,
          lastName: form.lastName,
          dateOfBirth: form.dateOfBirth,
          gender: form.gender,
          nationalId: form.nationalId || undefined,
          birthCertificateNumber: form.birthCertificateNumber || undefined,
          address: form.address || undefined,
          city: form.city || undefined,
          nationality: form.nationality || undefined,
          religion: form.religion || undefined,
          medicalNotes: form.medicalNotes || undefined,
          specialNeeds: form.specialNeeds || undefined,
          classId: form.classId || null,
        })
        await queryClient.invalidateQueries({ queryKey: ['student', student.id] })
        await queryClient.invalidateQueries({ queryKey: ['students'] })
        onClose()
      } else {
        const created = await createStudent({
          firstName: form.firstName,
          middleName: form.middleName || undefined,
          lastName: form.lastName,
          dateOfBirth: form.dateOfBirth,
          gender: form.gender,
          nationalId: form.nationalId || undefined,
          birthCertificateNumber: form.birthCertificateNumber || undefined,
          address: form.address || undefined,
          city: form.city || undefined,
          nationality: form.nationality || undefined,
          religion: form.religion || undefined,
          previousSchool: form.previousSchool || undefined,
          medicalNotes: form.medicalNotes || undefined,
          specialNeeds: form.specialNeeds || undefined,
          classId: form.classId || null,
          admissionDate: form.admissionDate,
        })
        await queryClient.invalidateQueries({ queryKey: ['students'] })
        onClose()
        navigate(`/students/${created.id}`)
      }
    } catch (err) {
      setError(getErrorMessage(err, isEdit ? 'Could not update student' : 'Could not create student'))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Drawer
      open={open}
      onClose={onClose}
      title={isEdit ? 'Edit student' : 'Add student'}
      description={isEdit ? student?.fullName : 'Create a new student record'}
      footer={
        <>
          <Button variant="secondary" onClick={onClose} type="button">
            Cancel
          </Button>
          <Button type="submit" form="student-form" loading={submitting}>
            {isEdit ? 'Save changes' : 'Create student'}
          </Button>
        </>
      }
    >
      <form id="student-form" onSubmit={handleSubmit} className="space-y-4">
        <div className="grid grid-cols-2 gap-4">
          <TextField
            label="First name"
            required
            value={form.firstName}
            onChange={(e) => set('firstName', e.target.value)}
          />
          <TextField
            label="Last name"
            required
            value={form.lastName}
            onChange={(e) => set('lastName', e.target.value)}
          />
        </div>

        <TextField
          label="Middle name"
          value={form.middleName}
          onChange={(e) => set('middleName', e.target.value)}
        />

        <div className="grid grid-cols-2 gap-4">
          <TextField
            label="Date of birth"
            type="date"
            required
            value={form.dateOfBirth}
            onChange={(e) => set('dateOfBirth', e.target.value)}
          />
          <SelectField label="Gender" required value={form.gender} onChange={(e) => set('gender', e.target.value)}>
            {GENDERS.map((g) => (
              <option key={g} value={g}>
                {g}
              </option>
            ))}
          </SelectField>
        </div>

        <div className="grid grid-cols-2 gap-4">
          <SelectField label="Class" value={form.classId} onChange={(e) => set('classId', e.target.value)}>
            <option value="">Not assigned</option>
            {classes?.map((c) => (
              <option key={c.id} value={c.id}>
                {c.name}
              </option>
            ))}
          </SelectField>
          {!isEdit && (
            <TextField
              label="Admission date"
              type="date"
              required
              value={form.admissionDate}
              onChange={(e) => set('admissionDate', e.target.value)}
            />
          )}
        </div>

        <div className="grid grid-cols-2 gap-4">
          <TextField
            label="National ID"
            value={form.nationalId}
            onChange={(e) => set('nationalId', e.target.value)}
          />
          <TextField
            label="Birth certificate #"
            value={form.birthCertificateNumber}
            onChange={(e) => set('birthCertificateNumber', e.target.value)}
          />
        </div>

        <TextField label="Address" value={form.address} onChange={(e) => set('address', e.target.value)} />

        <div className="grid grid-cols-2 gap-4">
          <TextField label="City" value={form.city} onChange={(e) => set('city', e.target.value)} />
          <TextField
            label="Nationality"
            value={form.nationality}
            onChange={(e) => set('nationality', e.target.value)}
          />
        </div>

        {!isEdit && (
          <TextField
            label="Previous school"
            value={form.previousSchool}
            onChange={(e) => set('previousSchool', e.target.value)}
          />
        )}

        <TextareaField
          label="Medical notes"
          rows={2}
          value={form.medicalNotes}
          onChange={(e) => set('medicalNotes', e.target.value)}
        />

        <TextareaField
          label="Special needs"
          rows={2}
          value={form.specialNeeds}
          onChange={(e) => set('specialNeeds', e.target.value)}
        />

        {error && (
          <p className="rounded-lg bg-red-50 px-3 py-2.5 text-sm text-red-700 dark:bg-red-950/40 dark:text-red-300">
            {error}
          </p>
        )}
      </form>
    </Drawer>
  )
}
