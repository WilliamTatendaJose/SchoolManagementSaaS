import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { getErrorMessage } from '../../api/errors'
import { createRole, fetchPermissions } from '../../api/roles'
import { Button } from '../../components/ui/Button'
import { Drawer } from '../../components/ui/Drawer'
import { TextField } from '../../components/ui/Field'
import { PermissionChecklist } from './PermissionChecklist'

export function RoleFormDrawer({ open, onClose }: { open: boolean; onClose: () => void }) {
  const queryClient = useQueryClient()
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [selected, setSelected] = useState<Set<string>>(new Set())
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  const { data: groups } = useQuery({ queryKey: ['permissions'], queryFn: () => fetchPermissions(), enabled: open })

  useEffect(() => {
    if (open) {
      setName('')
      setDescription('')
      setSelected(new Set())
      setError(null)
    }
  }, [open])

  function toggle(code: string) {
    setSelected((prev) => {
      const next = new Set(prev)
      if (next.has(code)) next.delete(code)
      else next.add(code)
      return next
    })
  }

  function toggleModule(codes: string[]) {
    setSelected((prev) => {
      const next = new Set(prev)
      const allSelected = codes.every((c) => next.has(c))
      codes.forEach((c) => (allSelected ? next.delete(c) : next.add(c)))
      return next
    })
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    setSubmitting(true)
    try {
      await createRole({ name, description: description || undefined, permissionCodes: Array.from(selected) })
      await queryClient.invalidateQueries({ queryKey: ['roles'] })
      onClose()
    } catch (err) {
      setError(getErrorMessage(err, 'Could not create role'))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Drawer
      open={open}
      onClose={onClose}
      title="Add role"
      description="Custom roles can be assigned to users alongside built-in ones"
      footer={
        <>
          <Button variant="secondary" type="button" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" form="role-form" loading={submitting}>
            Create role
          </Button>
        </>
      }
    >
      <form id="role-form" onSubmit={handleSubmit} className="space-y-4">
        <TextField label="Name" required value={name} onChange={(e) => setName(e.target.value)} />
        <TextField label="Description" value={description} onChange={(e) => setDescription(e.target.value)} />

        <div>
          <span className="mb-1.5 block text-sm font-medium text-slate-700 dark:text-slate-300">Permissions</span>
          <PermissionChecklist groups={groups} selected={selected} onToggle={toggle} onToggleModule={toggleModule} />
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
