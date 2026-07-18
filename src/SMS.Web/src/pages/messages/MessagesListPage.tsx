import { useQuery } from '@tanstack/react-query'
import { MessageSquare, PenSquare, Wallet } from 'lucide-react'
import { useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { fetchMessages } from '../../api/messages'
import { useAuthStore } from '../../auth/authStore'
import { Badge } from '../../components/ui/Badge'
import { Button } from '../../components/ui/Button'
import { EmptyState } from '../../components/ui/EmptyState'
import { PageHeader } from '../../components/ui/PageHeader'
import { Pagination } from '../../components/ui/Pagination'
import { SendFeeRemindersDrawer } from './SendFeeRemindersDrawer'
import { SendMessageDrawer } from './SendMessageDrawer'

const PAGE_SIZE = 15

const STATUS_TONE: Record<string, 'emerald' | 'brand' | 'amber' | 'slate'> = {
  Sent: 'emerald',
  Sending: 'brand',
  Queued: 'amber',
  Draft: 'slate',
  Failed: 'amber',
}

export function MessagesListPage() {
  const navigate = useNavigate()
  const canSend = useAuthStore((s) => s.hasPermission('messages.send'))
  const canBulk = useAuthStore((s) => s.hasPermission('messages.bulk'))
  const [params, setParams] = useSearchParams()
  const [composeOpen, setComposeOpen] = useState(false)
  const [remindersOpen, setRemindersOpen] = useState(false)
  const page = Number(params.get('page') ?? '1')

  function updateParam(key: string, value: string) {
    const next = new URLSearchParams(params)
    if (value) next.set(key, value)
    else next.delete(key)
    if (key !== 'page') next.delete('page')
    setParams(next, { replace: true })
  }

  const { data, isLoading } = useQuery({
    queryKey: ['messages', { page }],
    queryFn: () => fetchMessages({ pageNumber: page, pageSize: PAGE_SIZE }),
    placeholderData: (prev) => prev,
  })

  return (
    <div>
      <PageHeader
        title="Messages"
        description={data ? `${data.totalCount} messages` : 'Announcements and reminders sent to guardians'}
        actions={
          <>
            {canBulk && (
              <Button variant="secondary" onClick={() => setRemindersOpen(true)}>
                <Wallet className="h-4 w-4" strokeWidth={2} />
                Fee reminders
              </Button>
            )}
            {canSend && (
              <Button onClick={() => setComposeOpen(true)}>
                <PenSquare className="h-4 w-4" strokeWidth={2} />
                Compose
              </Button>
            )}
          </>
        }
      />

      <div className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm dark:border-slate-800 dark:bg-slate-900">
        {isLoading ? (
          <div className="space-y-3 p-4">
            {Array.from({ length: 6 }).map((_, i) => (
              <div key={i} className="h-12 animate-pulse rounded-lg bg-slate-100 dark:bg-slate-800/60" />
            ))}
          </div>
        ) : data && data.items.length > 0 ? (
          <>
            <table className="w-full text-left text-sm">
              <thead>
                <tr className="border-b border-slate-200 text-xs uppercase tracking-wider text-slate-400 dark:border-slate-800">
                  <th className="px-4 py-3 font-medium">Subject</th>
                  <th className="px-4 py-3 font-medium">Type</th>
                  <th className="px-4 py-3 font-medium">Channel</th>
                  <th className="px-4 py-3 font-medium">Status</th>
                  <th className="px-4 py-3 font-medium text-right">Delivered</th>
                  <th className="px-4 py-3 font-medium">Sent</th>
                </tr>
              </thead>
              <tbody>
                {data.items.map((m) => (
                  <tr
                    key={m.id}
                    onClick={() => navigate(`/messages/${m.id}`)}
                    className="cursor-pointer border-b border-slate-100 last:border-0 hover:bg-slate-50 dark:border-slate-800/60 dark:hover:bg-slate-800/40"
                  >
                    <td className="px-4 py-3 font-medium text-slate-900 dark:text-white">{m.subject}</td>
                    <td className="px-4 py-3 text-slate-600 dark:text-slate-300">{m.messageType}</td>
                    <td className="px-4 py-3 text-slate-600 dark:text-slate-300">{m.channel}</td>
                    <td className="px-4 py-3">
                      <Badge tone={STATUS_TONE[m.status] ?? 'slate'}>{m.status}</Badge>
                    </td>
                    <td className="px-4 py-3 text-right text-slate-600 dark:text-slate-300">
                      {m.deliveredCount}/{m.totalRecipients}
                      {m.failedCount > 0 && <span className="ml-1 text-red-500">({m.failedCount} failed)</span>}
                    </td>
                    <td className="px-4 py-3 text-slate-500 dark:text-slate-400">
                      {m.sentAt ? new Date(m.sentAt).toLocaleString() : new Date(m.createdAt).toLocaleString()}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
            <Pagination
              pageNumber={data.pageNumber}
              totalPages={data.totalPages}
              totalCount={data.totalCount}
              pageSize={data.pageSize}
              hasPreviousPage={data.hasPreviousPage}
              hasNextPage={data.hasNextPage}
              onPageChange={(p) => updateParam('page', String(p))}
            />
          </>
        ) : (
          <EmptyState
            icon={MessageSquare}
            title="No messages sent yet"
            description="Compose an announcement or send fee reminders to get started."
            action={
              canSend && (
                <Button onClick={() => setComposeOpen(true)}>
                  <PenSquare className="h-4 w-4" strokeWidth={2} />
                  Compose
                </Button>
              )
            }
          />
        )}
      </div>

      <SendMessageDrawer open={composeOpen} onClose={() => setComposeOpen(false)} />
      <SendFeeRemindersDrawer open={remindersOpen} onClose={() => setRemindersOpen(false)} />
    </div>
  )
}
