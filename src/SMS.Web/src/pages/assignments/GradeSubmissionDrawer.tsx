import { useQueryClient } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { gradeSubmission } from '../../api/assignments'
import { getErrorMessage } from '../../api/errors'
import type { AssignmentSubmissionDto } from '../../api/types'
import { Button } from '../../components/ui/Button'
import { Drawer } from '../../components/ui/Drawer'
import { TextareaField, TextField } from '../../components/ui/Field'

export function GradeSubmissionDrawer({
  submission,
  assignmentId,
  onClose,
}: {
  submission: AssignmentSubmissionDto | null
  assignmentId: string
  onClose: () => void
}) {
  const queryClient = useQueryClient()
  const [grade, setGrade] = useState('0')
  const [feedback, setFeedback] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  useEffect(() => {
    if (submission) {
      setGrade(submission.grade != null ? String(submission.grade) : '0')
      setFeedback(submission.feedback ?? '')
      setError(null)
    }
  }, [submission])

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!submission) return
    setError(null)
    setSubmitting(true)
    try {
      await gradeSubmission({ submissionId: submission.id, grade: Number(grade), feedback: feedback || undefined })
      await queryClient.invalidateQueries({ queryKey: ['assignment-submissions', assignmentId] })
      onClose()
    } catch (err) {
      setError(getErrorMessage(err, 'Could not grade submission'))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Drawer
      open={!!submission}
      onClose={onClose}
      title="Grade submission"
      description={submission?.studentName}
      footer={
        <>
          <Button variant="secondary" type="button" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" form="grade-submission-form" loading={submitting}>
            Save grade
          </Button>
        </>
      }
    >
      <form id="grade-submission-form" onSubmit={handleSubmit} className="space-y-4">
        <TextField label="Grade" type="number" min={0} step="0.01" required value={grade} onChange={(e) => setGrade(e.target.value)} />
        <TextareaField label="Feedback" rows={3} value={feedback} onChange={(e) => setFeedback(e.target.value)} />

        {error && (
          <p className="rounded-lg bg-red-50 px-3 py-2.5 text-sm text-red-700 dark:bg-red-950/40 dark:text-red-300">
            {error}
          </p>
        )}
      </form>
    </Drawer>
  )
}
