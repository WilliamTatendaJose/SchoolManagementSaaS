import { useQueryClient } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { getErrorMessage } from '../../api/errors'
import { reviewWeekendLeave } from '../../api/hostel'
import type { WeekendLeaveDto } from '../../api/types'
import { Button } from '../../components/ui/Button'
import { Drawer } from '../../components/ui/Drawer'
import { TextareaField } from '../../components/ui/Field'

export function ReviewLeaveDrawer({ leave, onClose }: { leave: WeekendLeaveDto | null; onClose: () => void }) {
  const queryClient = useQueryClient()
  const [approve, setApprove] = useState(true)
  const [reviewNote, setReviewNote] = useState('')
  const [notifyGuardian, setNotifyGuardian] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  useEffect(() => {
    if (leave) {
      setApprove(true)
      setReviewNote('')
      setNotifyGuardian(true)
      setError(null)
    }
  }, [leave])

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!leave) return
    setError(null)
    setSubmitting(true)
    try {
      await reviewWeekendLeave({ leaveId: leave.id, approve, reviewNote: reviewNote || undefined, notifyGuardian })
      await queryClient.invalidateQueries({ queryKey: ['weekend-leaves'] })
      onClose()
    } catch (err) {
      setError(getErrorMessage(err, 'Could not review leave request'))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Drawer
      open={!!leave}
      onClose={onClose}
      title="Review leave request"
      description={leave ? `${leave.studentName} · ${leave.destination}` : undefined}
      footer={
        <>
          <Button variant="secondary" type="button" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" form="review-leave-form" loading={submitting} variant={approve ? 'primary' : 'danger'}>
            {approve ? 'Approve' : 'Reject'}
          </Button>
        </>
      }
    >
      <form id="review-leave-form" onSubmit={handleSubmit} className="space-y-4">
        <div className="flex gap-2">
          <button
            type="button"
            onClick={() => setApprove(true)}
            className={`flex-1 rounded-lg border px-3 py-2 text-sm font-medium transition-colors ${
              approve
                ? 'border-brand-500 bg-brand-50 text-brand-700 dark:border-brand-600 dark:bg-brand-950/40 dark:text-brand-300'
                : 'border-slate-300 text-slate-600 hover:bg-slate-50 dark:border-slate-700 dark:text-slate-300 dark:hover:bg-slate-800'
            }`}
          >
            Approve
          </button>
          <button
            type="button"
            onClick={() => setApprove(false)}
            className={`flex-1 rounded-lg border px-3 py-2 text-sm font-medium transition-colors ${
              !approve
                ? 'border-red-500 bg-red-50 text-red-700 dark:border-red-600 dark:bg-red-950/40 dark:text-red-300'
                : 'border-slate-300 text-slate-600 hover:bg-slate-50 dark:border-slate-700 dark:text-slate-300 dark:hover:bg-slate-800'
            }`}
          >
            Reject
          </button>
        </div>

        <TextareaField
          label="Review note"
          rows={2}
          value={reviewNote}
          onChange={(e) => setReviewNote(e.target.value)}
        />

        {approve && (
          <label className="flex items-center gap-2.5 text-sm text-slate-700 dark:text-slate-300">
            <input
              type="checkbox"
              checked={notifyGuardian}
              onChange={(e) => setNotifyGuardian(e.target.checked)}
              className="h-4 w-4 rounded border-slate-300 text-brand-600 focus:ring-brand-500"
            />
            Notify guardian by message
          </label>
        )}

        {error && (
          <p className="rounded-lg bg-red-50 px-3 py-2.5 text-sm text-red-700 dark:bg-red-950/40 dark:text-red-300">
            {error}
          </p>
        )}
      </form>
    </Drawer>
  )
}
