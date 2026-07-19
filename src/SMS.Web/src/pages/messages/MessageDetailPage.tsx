import { useQuery } from '@tanstack/react-query'
import { CheckCheck, CheckCircle2, Clock, Send, XCircle } from 'lucide-react'
import { useParams } from 'react-router-dom'
import { fetchMessage } from '../../api/messages'
import type { MessageRecipientDto } from '../../api/types'
import { Badge } from '../../components/ui/Badge'
import { PageHeader } from '../../components/ui/PageHeader'

const STATUS_TONE: Record<string, 'emerald' | 'brand' | 'amber' | 'slate'> = {
  Sent: 'emerald',
  Sending: 'brand',
  Queued: 'amber',
  Draft: 'slate',
  Failed: 'amber',
}

/** Renders a per-recipient delivery status with an icon that matches WhatsApp's own
 * vocabulary (Sent → Delivered → Read), so the head can read the sheet at a glance. */
function RecipientStatus({ r }: { r: MessageRecipientDto }) {
  switch (r.status) {
    case 'Read':
      return (
        <span className="flex items-center gap-1 text-brand-600 dark:text-brand-300">
          <CheckCheck className="h-3.5 w-3.5" strokeWidth={2} />
          Read
        </span>
      )
    case 'Delivered':
      return (
        <span className="flex items-center gap-1 text-emerald-600 dark:text-emerald-400">
          <CheckCircle2 className="h-3.5 w-3.5" strokeWidth={2} />
          Delivered
        </span>
      )
    case 'Sent':
      return (
        <span className="flex items-center gap-1 text-slate-500 dark:text-slate-400" title="Accepted by the provider; awaiting a delivery receipt">
          <Send className="h-3.5 w-3.5" strokeWidth={2} />
          Sent
        </span>
      )
    case 'Failed':
      return (
        <span className="flex items-center gap-1 text-red-600 dark:text-red-400" title={r.failureReason ?? undefined}>
          <XCircle className="h-3.5 w-3.5" strokeWidth={2} />
          Failed
        </span>
      )
    case 'Pending':
      return (
        <span className="flex items-center gap-1 text-amber-600 dark:text-amber-400">
          <Clock className="h-3.5 w-3.5" strokeWidth={2} />
          Pending
        </span>
      )
    default:
      return <Badge tone="slate">{r.status}</Badge>
  }
}

export function MessageDetailPage() {
  const { id } = useParams<{ id: string }>()

  const { data: message, isLoading } = useQuery({
    queryKey: ['message', id],
    queryFn: () => fetchMessage(id!),
    enabled: !!id,
  })

  if (isLoading) {
    return <div className="h-64 animate-pulse rounded-2xl bg-slate-100 dark:bg-slate-800/50" />
  }

  if (!message) {
    return <p className="text-sm text-slate-500">Message not found.</p>
  }

  const counts = message.recipients.reduce<Record<string, number>>((acc, r) => {
    acc[r.status] = (acc[r.status] ?? 0) + 1
    return acc
  }, {})
  const reached = (counts.Delivered ?? 0) + (counts.Read ?? 0)
  const reachedPct = message.totalRecipients > 0 ? Math.round((reached / message.totalRecipients) * 100) : 0

  return (
    <div>
      <PageHeader title={message.subject} backTo="/messages" />

      <div className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm dark:border-slate-800 dark:bg-slate-900">
        <div className="flex flex-wrap items-center gap-2">
          <Badge tone={STATUS_TONE[message.status] ?? 'slate'}>{message.status}</Badge>
          <Badge tone="slate">{message.channel}</Badge>
          <Badge tone="slate">{message.messageType}</Badge>
        </div>
        <p className="mt-3 whitespace-pre-wrap text-sm text-slate-700 dark:text-slate-200">{message.content}</p>
        <p className="mt-3 text-xs text-slate-400">
          {message.sentAt ? `Sent ${new Date(message.sentAt).toLocaleString()}` : `Created ${new Date(message.createdAt).toLocaleString()}`}
        </p>
      </div>

      <div className="mt-4 rounded-2xl border border-slate-200 bg-white p-6 shadow-sm dark:border-slate-800 dark:bg-slate-900">
        <div className="flex items-baseline justify-between">
          <h3 className="text-sm font-semibold text-slate-900 dark:text-white">Delivery</h3>
          <span className="text-sm font-semibold text-emerald-600 dark:text-emerald-400">
            {reachedPct}% reached
          </span>
        </div>
        <div className="mt-2 h-2 overflow-hidden rounded-full bg-slate-100 dark:bg-slate-800">
          <div className="h-full rounded-full bg-emerald-500 transition-all" style={{ width: `${reachedPct}%` }} />
        </div>
        <div className="mt-4 grid grid-cols-2 gap-3 sm:grid-cols-5">
          <DeliveryStat label="Recipients" value={message.totalRecipients} tone="slate" />
          <DeliveryStat label="Sent" value={counts.Sent ?? 0} tone="slate" />
          <DeliveryStat label="Delivered" value={counts.Delivered ?? 0} tone="emerald" />
          <DeliveryStat label="Read" value={counts.Read ?? 0} tone="brand" />
          <DeliveryStat label="Failed" value={counts.Failed ?? 0} tone={(counts.Failed ?? 0) > 0 ? 'red' : 'slate'} />
        </div>
        {message.channel === 'WhatsApp' && (counts.Sent ?? 0) > 0 && (
          <p className="mt-3 text-xs text-slate-400">
            "Sent" messages are awaiting a delivery receipt from WhatsApp; this updates automatically as receipts arrive.
          </p>
        )}
      </div>

      <div className="mt-4 overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm dark:border-slate-800 dark:bg-slate-900">
        <h3 className="border-b border-slate-200 px-4 py-3 text-sm font-semibold text-slate-900 dark:border-slate-800 dark:text-white">
          Recipients
        </h3>
        {message.recipients.length === 0 ? (
          <p className="px-4 py-6 text-sm text-slate-400">No recipients recorded.</p>
        ) : (
          <table className="w-full text-left text-sm">
            <thead>
              <tr className="border-b border-slate-200 text-xs uppercase tracking-wider text-slate-400 dark:border-slate-800">
                <th className="px-4 py-3 font-medium">Name</th>
                <th className="px-4 py-3 font-medium">Phone</th>
                <th className="px-4 py-3 font-medium">Status</th>
                <th className="px-4 py-3 font-medium">Delivered</th>
                <th className="px-4 py-3 font-medium">Read</th>
              </tr>
            </thead>
            <tbody>
              {message.recipients.map((r, i) => (
                <tr key={i} className="border-b border-slate-100 last:border-0 dark:border-slate-800/60">
                  <td className="px-4 py-2.5 text-slate-700 dark:text-slate-200">{r.recipientName || '—'}</td>
                  <td className="px-4 py-2.5 text-slate-600 dark:text-slate-300">{r.recipientPhone}</td>
                  <td className="px-4 py-2.5">
                    <RecipientStatus r={r} />
                  </td>
                  <td className="px-4 py-2.5 text-slate-500 dark:text-slate-400">
                    {r.deliveredAt ? new Date(r.deliveredAt).toLocaleString() : '—'}
                  </td>
                  <td className="px-4 py-2.5 text-slate-500 dark:text-slate-400">
                    {r.readAt ? new Date(r.readAt).toLocaleString() : '—'}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  )
}

function DeliveryStat({
  label,
  value,
  tone,
}: {
  label: string
  value: number
  tone: 'slate' | 'emerald' | 'brand' | 'red'
}) {
  const toneClasses: Record<typeof tone, string> = {
    slate: 'bg-slate-50 text-slate-700 dark:bg-slate-800/60 dark:text-slate-200',
    emerald: 'bg-emerald-50 text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-300',
    brand: 'bg-brand-50 text-brand-700 dark:bg-brand-900/30 dark:text-brand-300',
    red: 'bg-red-50 text-red-700 dark:bg-red-900/30 dark:text-red-300',
  }
  return (
    <div className={`rounded-xl px-3 py-2.5 ${toneClasses[tone]}`}>
      <p className="text-xl font-semibold tracking-tight">{value}</p>
      <p className="text-xs font-medium opacity-80">{label}</p>
    </div>
  )
}
