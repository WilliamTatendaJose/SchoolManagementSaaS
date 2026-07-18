import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { getErrorMessage } from '../../api/errors'
import { fetchRoles } from '../../api/roles'
import type { UserDetailDto } from '../../api/types'
import { assignRoles } from '../../api/users'
import { Button } from '../../components/ui/Button'
import { Drawer } from '../../components/ui/Drawer'

export function AssignRolesDrawer({
  open,
  onClose,
  user,
}: {
  open: boolean
  onClose: () => void
  user: UserDetailDto
}) {
  const queryClient = useQueryClient()
  const [selected, setSelected] = useState<Set<string>>(new Set())
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  const { data: roles } = useQuery({ queryKey: ['roles'], queryFn: fetchRoles, enabled: open })

  useEffect(() => {
    if (open) {
      setSelected(new Set(user.roles.map((r) => r.name)))
      setError(null)
    }
  }, [open, user])

  function toggle(name: string) {
    setSelected((prev) => {
      const next = new Set(prev)
      if (next.has(name)) next.delete(name)
      else next.add(name)
      return next
    })
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    setSubmitting(true)
    try {
      await assignRoles({ userId: user.id, roles: Array.from(selected) })
      await queryClient.invalidateQueries({ queryKey: ['user', user.id] })
      await queryClient.invalidateQueries({ queryKey: ['users'] })
      await queryClient.invalidateQueries({ queryKey: ['roles'] })
      onClose()
    } catch (err) {
      setError(getErrorMessage(err, 'Could not update roles'))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Drawer
      open={open}
      onClose={onClose}
      title="Manage roles"
      description={`Replaces all roles currently assigned to ${user.fullName}`}
      footer={
        <>
          <Button variant="secondary" type="button" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" form="assign-roles-form" loading={submitting}>
            Save roles
          </Button>
        </>
      }
    >
      <form id="assign-roles-form" onSubmit={handleSubmit} className="space-y-4">
        <div className="rounded-lg border border-slate-200 dark:border-slate-800">
          {roles?.map((r) => (
            <label
              key={r.id}
              className="flex cursor-pointer items-center gap-2.5 border-b border-slate-100 px-3 py-2.5 text-sm last:border-0 hover:bg-slate-50 dark:border-slate-800 dark:hover:bg-slate-800"
            >
              <input
                type="checkbox"
                checked={selected.has(r.name)}
                onChange={() => toggle(r.name)}
                className="h-4 w-4 rounded border-slate-300 text-brand-600 focus:ring-brand-500"
              />
              <div>
                <p className="text-slate-800 dark:text-slate-100">{r.name}</p>
                {r.description && <p className="text-xs text-slate-400">{r.description}</p>}
              </div>
            </label>
          ))}
        </div>

        {error && (
          <p className="rounded-lg bg-red-50 px-3 py-2.5 text-sm text-red-700 dark:bg-red-950/40 dark:text-red-300">
            {error}
          </p>
        )}
      </form>
    </Drawer>
  )
}
