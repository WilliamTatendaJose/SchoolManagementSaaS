import { useQuery } from '@tanstack/react-query'
import { CheckCircle2, XCircle } from 'lucide-react'
import { useParams } from 'react-router-dom'
import { fetchMessage } from '../../api/messages'
import { Badge } from '../../components/ui/Badge'
import { PageHeader } from '../../components/ui/PageHeader'

const STATUS_TONE: Record<string, 'emerald' | 'brand' | 'amber' | 'slate'> = {
  Sent: 'emerald',
  Sending: 'brand',
  Queued: 'amber',
  Draft: 'slate',
  Failed: 'amber',
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
          {' · '}
          {message.deliveredCount}/{message.totalRecipients} delivered
          {message.failedCount > 0 ? `, ${message.failedCount} failed` : ''}
        </p>
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
              </tr>
            </thead>
            <tbody>
              {message.recipients.map((r, i) => (
                <tr key={i} className="border-b border-slate-100 last:border-0 dark:border-slate-800/60">
                  <td className="px-4 py-2.5 text-slate-700 dark:text-slate-200">{r.recipientName || '—'}</td>
                  <td className="px-4 py-2.5 text-slate-600 dark:text-slate-300">{r.recipientPhone}</td>
                  <td className="px-4 py-2.5">
                    {r.status === 'Delivered' ? (
                      <span className="flex items-center gap-1 text-emerald-600 dark:text-emerald-400">
                        <CheckCircle2 className="h-3.5 w-3.5" strokeWidth={2} />
                        Delivered
                      </span>
                    ) : r.status === 'Failed' ? (
                      <span className="flex items-center gap-1 text-red-600 dark:text-red-400" title={r.failureReason ?? undefined}>
                        <XCircle className="h-3.5 w-3.5" strokeWidth={2} />
                        Failed
                      </span>
                    ) : (
                      <Badge tone="slate">{r.status}</Badge>
                    )}
                  </td>
                  <td className="px-4 py-2.5 text-slate-500 dark:text-slate-400">
                    {r.deliveredAt ? new Date(r.deliveredAt).toLocaleString() : '—'}
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
