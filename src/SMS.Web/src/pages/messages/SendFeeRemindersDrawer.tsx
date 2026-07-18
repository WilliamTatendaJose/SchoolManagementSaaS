import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { sendFeeReminders } from '../../api/messages'
import { getErrorMessage } from '../../api/errors'
import { fetchClasses } from '../../api/classes'
import { MESSAGE_CHANNELS } from '../../api/types'
import { Button } from '../../components/ui/Button'
import { Drawer } from '../../components/ui/Drawer'
import { SelectField } from '../../components/ui/Field'

export function SendFeeRemindersDrawer({ open, onClose }: { open: boolean; onClose: () => void }) {
  const queryClient = useQueryClient()
  const [channel, setChannel] = useState<(typeof MESSAGE_CHANNELS)[number]>('SMS')
  const [classId, setClassId] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [result, setResult] = useState<{ totalRecipients: number; delivered: number; failed: number } | null>(null)
  const [submitting, setSubmitting] = useState(false)

  const { data: classes } = useQuery({ queryKey: ['classes'], queryFn: fetchClasses, enabled: open })

  useEffect(() => {
    if (open) {
      setChannel('SMS')
      setClassId('')
      setError(null)
      setResult(null)
    }
  }, [open])

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    setSubmitting(true)
    try {
      const res = await sendFeeReminders({ channel, classId: classId || undefined })
      setResult(res)
      await queryClient.invalidateQueries({ queryKey: ['messages'] })
    } catch (err) {
      setError(getErrorMessage(err, 'Could not send fee reminders'))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Drawer
      open={open}
      onClose={onClose}
      title="Send fee reminders"
      description="Notifies guardians of every student with an outstanding balance"
      footer={
        <>
          <Button variant="secondary" type="button" onClick={onClose}>
            {result ? 'Close' : 'Cancel'}
          </Button>
          {!result && (
            <Button type="submit" form="fee-reminders-form" loading={submitting}>
              Send reminders
            </Button>
          )}
        </>
      }
    >
      <form id="fee-reminders-form" onSubmit={handleSubmit} className="space-y-4">
        <SelectField label="Channel" value={channel} onChange={(e) => setChannel(e.target.value as typeof channel)}>
          {MESSAGE_CHANNELS.map((c) => (
            <option key={c} value={c}>
              {c}
            </option>
          ))}
        </SelectField>
        <SelectField
          label="Class"
          value={classId}
          onChange={(e) => setClassId(e.target.value)}
          hint="Leave blank to include defaulters across all classes"
        >
          <option value="">All classes</option>
          {classes?.map((c) => (
            <option key={c.id} value={c.id}>
              {c.name}
            </option>
          ))}
        </SelectField>

        {result && (
          <div className="rounded-lg bg-emerald-50 px-3 py-2.5 text-sm text-emerald-800 dark:bg-emerald-950/40 dark:text-emerald-300">
            Sent to {result.delivered} of {result.totalRecipients} guardians
            {result.failed > 0 ? ` (${result.failed} failed)` : ''}.
          </div>
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
