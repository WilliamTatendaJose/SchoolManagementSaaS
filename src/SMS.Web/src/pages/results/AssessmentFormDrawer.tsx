import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { createAssessment } from '../../api/assessments'
import { fetchClasses } from '../../api/classes'
import { getErrorMessage } from '../../api/errors'
import { fetchAcademicTerms } from '../../api/academicTerms'
import { fetchSubjects } from '../../api/subjects'
import { Button } from '../../components/ui/Button'
import { Drawer } from '../../components/ui/Drawer'
import { SelectField, TextareaField, TextField } from '../../components/ui/Field'

const ASSESSMENT_TYPES = ['Test', 'Quiz', 'Assignment', 'Exam', 'Project', 'Practical']

export function AssessmentFormDrawer({ open, onClose }: { open: boolean; onClose: () => void }) {
  const queryClient = useQueryClient()
  const navigate = useNavigate()
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [classId, setClassId] = useState('')
  const [subjectId, setSubjectId] = useState('')
  const [termId, setTermId] = useState('')
  const [assessmentType, setAssessmentType] = useState('Test')
  const [maxScore, setMaxScore] = useState('100')
  const [weightPercentage, setWeightPercentage] = useState('10')
  const [date, setDate] = useState(new Date().toISOString().slice(0, 10))
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  const { data: classes } = useQuery({ queryKey: ['classes'], queryFn: fetchClasses, enabled: open })
  const { data: subjects } = useQuery({ queryKey: ['subjects'], queryFn: () => fetchSubjects(true), enabled: open })
  const { data: terms } = useQuery({ queryKey: ['academic-terms'], queryFn: () => fetchAcademicTerms(), enabled: open })

  useEffect(() => {
    if (open) {
      setName('')
      setDescription('')
      setClassId('')
      setSubjectId('')
      setTermId('')
      setAssessmentType('Test')
      setMaxScore('100')
      setWeightPercentage('10')
      setDate(new Date().toISOString().slice(0, 10))
      setError(null)
    }
  }, [open])

  useEffect(() => {
    if (terms && terms.length > 0 && !termId) {
      setTermId(terms.find((t) => t.isCurrent)?.id ?? terms[0].id)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [terms])

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    setSubmitting(true)
    try {
      const created = await createAssessment({
        name,
        description: description || undefined,
        classId,
        subjectId,
        academicTermId: termId,
        assessmentType,
        maxScore: Number(maxScore),
        weightPercentage: Number(weightPercentage),
        date: date || undefined,
      })
      await queryClient.invalidateQueries({ queryKey: ['assessments'] })
      onClose()
      navigate(`/results/${created.id}`)
    } catch (err) {
      setError(getErrorMessage(err, 'Could not create assessment'))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Drawer
      open={open}
      onClose={onClose}
      title="Add assessment"
      description="Create a test, exam or assignment to record scores against"
      footer={
        <>
          <Button variant="secondary" type="button" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" form="assessment-form" loading={submitting}>
            Create assessment
          </Button>
        </>
      }
    >
      <form id="assessment-form" onSubmit={handleSubmit} className="space-y-4">
        <TextField label="Name" required value={name} onChange={(e) => setName(e.target.value)} placeholder="Mid-term test" />

        <div className="grid grid-cols-2 gap-4">
          <SelectField label="Class" required value={classId} onChange={(e) => setClassId(e.target.value)}>
            <option value="">Select a class…</option>
            {classes?.map((c) => (
              <option key={c.id} value={c.id}>
                {c.name}
              </option>
            ))}
          </SelectField>
          <SelectField label="Subject" required value={subjectId} onChange={(e) => setSubjectId(e.target.value)}>
            <option value="">Select a subject…</option>
            {subjects?.map((s) => (
              <option key={s.id} value={s.id}>
                {s.name}
              </option>
            ))}
          </SelectField>
        </div>

        <SelectField label="Academic term" required value={termId} onChange={(e) => setTermId(e.target.value)}>
          <option value="">Select a term…</option>
          {terms?.map((t) => (
            <option key={t.id} value={t.id}>
              {t.academicYearName} · {t.name}
              {t.isCurrent ? ' (current)' : ''}
            </option>
          ))}
        </SelectField>

        <div className="grid grid-cols-2 gap-4">
          <SelectField
            label="Type"
            required
            value={assessmentType}
            onChange={(e) => setAssessmentType(e.target.value)}
          >
            {ASSESSMENT_TYPES.map((t) => (
              <option key={t} value={t}>
                {t}
              </option>
            ))}
          </SelectField>
          <TextField label="Date" type="date" value={date} onChange={(e) => setDate(e.target.value)} />
        </div>

        <div className="grid grid-cols-2 gap-4">
          <TextField
            label="Max score"
            type="number"
            min={1}
            step="0.01"
            required
            value={maxScore}
            onChange={(e) => setMaxScore(e.target.value)}
          />
          <TextField
            label="Weight %"
            type="number"
            min={0}
            max={100}
            required
            value={weightPercentage}
            onChange={(e) => setWeightPercentage(e.target.value)}
            hint="Contribution to the subject average"
          />
        </div>

        <TextareaField
          label="Description"
          rows={2}
          value={description}
          onChange={(e) => setDescription(e.target.value)}
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
