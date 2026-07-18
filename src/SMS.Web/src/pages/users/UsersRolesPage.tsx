import { useQuery } from '@tanstack/react-query'
import { Plus, Shield, UsersRound } from 'lucide-react'
import { useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { fetchRoles } from '../../api/roles'
import { fetchUsers } from '../../api/users'
import { useAuthStore } from '../../auth/authStore'
import { Avatar } from '../../components/ui/Avatar'
import { Badge } from '../../components/ui/Badge'
import { Button } from '../../components/ui/Button'
import { EmptyState } from '../../components/ui/EmptyState'
import { PageHeader } from '../../components/ui/PageHeader'
import { Pagination } from '../../components/ui/Pagination'
import { SearchInput } from '../../components/ui/SearchInput'
import { useDebouncedValue } from '../../hooks/useDebouncedValue'
import { RoleFormDrawer } from './RoleFormDrawer'
import { RolePermissionsDrawer } from './RolePermissionsDrawer'
import { UserFormDrawer } from './UserFormDrawer'
import type { RoleDetailDto } from '../../api/types'

const PAGE_SIZE = 10

export function UsersRolesPage() {
  const navigate = useNavigate()
  const canCreateUsers = useAuthStore((s) => s.hasPermission('users.create'))
  const canManageRoles = useAuthStore((s) => s.hasPermission('roles.manage'))
  const [params, setParams] = useSearchParams()
  const tab = params.get('tab') === 'roles' && canManageRoles ? 'roles' : 'users'
  const [userFormOpen, setUserFormOpen] = useState(false)
  const [roleFormOpen, setRoleFormOpen] = useState(false)
  const [editingRole, setEditingRole] = useState<RoleDetailDto | null>(null)

  const page = Number(params.get('page') ?? '1')
  const search = params.get('q') ?? ''
  const status = params.get('status') ?? ''
  const debouncedSearch = useDebouncedValue(search, 350)

  function setTab(next: 'users' | 'roles') {
    setParams(next === 'users' ? {} : { tab: next })
  }

  function updateParam(key: string, value: string) {
    const next = new URLSearchParams(params)
    if (value) next.set(key, value)
    else next.delete(key)
    if (key !== 'page') next.delete('page')
    setParams(next, { replace: true })
  }

  const { data: users, isLoading: loadingUsers } = useQuery({
    queryKey: ['users', { page, debouncedSearch, status }],
    queryFn: () =>
      fetchUsers({
        pageNumber: page,
        pageSize: PAGE_SIZE,
        searchTerm: debouncedSearch || undefined,
        isActive: status ? status === 'active' : undefined,
      }),
    placeholderData: (prev) => prev,
    enabled: tab === 'users',
  })

  const { data: roles, isLoading: loadingRoles } = useQuery({
    queryKey: ['roles'],
    queryFn: fetchRoles,
    enabled: tab === 'roles',
  })

  return (
    <div>
      <PageHeader
        title="Users & roles"
        description={
          tab === 'users' ? (users ? `${users.totalCount} users` : undefined) : roles ? `${roles.length} roles` : undefined
        }
        actions={
          tab === 'users' ? (
            canCreateUsers && (
              <Button onClick={() => setUserFormOpen(true)}>
                <Plus className="h-4 w-4" strokeWidth={2.5} />
                Add user
              </Button>
            )
          ) : (
            canManageRoles && (
              <Button onClick={() => setRoleFormOpen(true)}>
                <Plus className="h-4 w-4" strokeWidth={2.5} />
                Add role
              </Button>
            )
          )
        }
      />

      <div className="mb-4 flex gap-1 border-b border-slate-200 dark:border-slate-800">
        <button
          onClick={() => setTab('users')}
          className={`flex items-center gap-1.5 border-b-2 px-3 py-2 text-sm font-medium transition-colors ${
            tab === 'users'
              ? 'border-brand-600 text-brand-700 dark:text-brand-400'
              : 'border-transparent text-slate-500 hover:text-slate-700 dark:hover:text-slate-300'
          }`}
        >
          <UsersRound className="h-4 w-4" strokeWidth={2} />
          Users
        </button>
        {canManageRoles && (
          <button
            onClick={() => setTab('roles')}
            className={`flex items-center gap-1.5 border-b-2 px-3 py-2 text-sm font-medium transition-colors ${
              tab === 'roles'
                ? 'border-brand-600 text-brand-700 dark:text-brand-400'
                : 'border-transparent text-slate-500 hover:text-slate-700 dark:hover:text-slate-300'
            }`}
          >
            <Shield className="h-4 w-4" strokeWidth={2} />
            Roles
          </button>
        )}
      </div>

      {tab === 'users' ? (
        <>
          <div className="flex flex-col gap-3 sm:flex-row sm:items-center">
            <SearchInput
              placeholder="Search by name or email…"
              value={search}
              onChange={(e) => updateParam('q', e.target.value)}
              className="sm:max-w-xs"
            />
            <select
              value={status}
              onChange={(e) => updateParam('status', e.target.value)}
              className="rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-700 focus:border-brand-500 focus:outline-none focus:ring-4 focus:ring-brand-500/10 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200"
            >
              <option value="">All statuses</option>
              <option value="active">Active</option>
              <option value="inactive">Inactive</option>
            </select>
          </div>

          <div className="mt-4 overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm dark:border-slate-800 dark:bg-slate-900">
            {loadingUsers ? (
              <div className="space-y-3 p-4">
                {Array.from({ length: 6 }).map((_, i) => (
                  <div key={i} className="h-12 animate-pulse rounded-lg bg-slate-100 dark:bg-slate-800/60" />
                ))}
              </div>
            ) : users && users.items.length > 0 ? (
              <>
                <table className="w-full text-left text-sm">
                  <thead>
                    <tr className="border-b border-slate-200 text-xs uppercase tracking-wider text-slate-400 dark:border-slate-800">
                      <th className="px-4 py-3 font-medium">User</th>
                      <th className="px-4 py-3 font-medium">Roles</th>
                      <th className="px-4 py-3 font-medium">Last login</th>
                      <th className="px-4 py-3 font-medium">Status</th>
                    </tr>
                  </thead>
                  <tbody>
                    {users.items.map((u) => (
                      <tr
                        key={u.id}
                        onClick={() => navigate(`/users/${u.id}`)}
                        className="cursor-pointer border-b border-slate-100 last:border-0 hover:bg-slate-50 dark:border-slate-800/60 dark:hover:bg-slate-800/40"
                      >
                        <td className="px-4 py-3">
                          <div className="flex items-center gap-3">
                            <Avatar name={u.fullName} size="sm" />
                            <div>
                              <p className="font-medium text-slate-900 dark:text-white">{u.fullName}</p>
                              <p className="text-xs text-slate-400">{u.email}</p>
                            </div>
                          </div>
                        </td>
                        <td className="px-4 py-3">
                          <div className="flex flex-wrap gap-1">
                            {u.roles.length > 0 ? (
                              u.roles.map((r) => (
                                <Badge key={r} tone="brand">
                                  {r}
                                </Badge>
                              ))
                            ) : (
                              <span className="text-slate-400">—</span>
                            )}
                          </div>
                        </td>
                        <td className="px-4 py-3 text-slate-600 dark:text-slate-300">
                          {u.lastLoginAt ? new Date(u.lastLoginAt).toLocaleDateString() : 'Never'}
                        </td>
                        <td className="px-4 py-3">
                          <Badge tone={u.isActive ? 'emerald' : 'red'}>{u.isActive ? 'Active' : 'Inactive'}</Badge>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
                <Pagination
                  pageNumber={users.pageNumber}
                  totalPages={users.totalPages}
                  totalCount={users.totalCount}
                  pageSize={users.pageSize}
                  hasPreviousPage={users.hasPreviousPage}
                  hasNextPage={users.hasNextPage}
                  onPageChange={(p) => updateParam('page', String(p))}
                />
              </>
            ) : (
              <EmptyState
                icon={UsersRound}
                title="No users found"
                description={search || status ? 'Try adjusting your filters.' : 'Add your first user to get started.'}
                action={
                  canCreateUsers &&
                  !search &&
                  !status && (
                    <Button onClick={() => setUserFormOpen(true)}>
                      <Plus className="h-4 w-4" strokeWidth={2.5} />
                      Add user
                    </Button>
                  )
                }
              />
            )}
          </div>
        </>
      ) : (
        <div className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm dark:border-slate-800 dark:bg-slate-900">
          {loadingRoles ? (
            <div className="space-y-3 p-4">
              {Array.from({ length: 4 }).map((_, i) => (
                <div key={i} className="h-12 animate-pulse rounded-lg bg-slate-100 dark:bg-slate-800/60" />
              ))}
            </div>
          ) : roles && roles.length > 0 ? (
            <table className="w-full text-left text-sm">
              <thead>
                <tr className="border-b border-slate-200 text-xs uppercase tracking-wider text-slate-400 dark:border-slate-800">
                  <th className="px-4 py-3 font-medium">Role</th>
                  <th className="px-4 py-3 font-medium">Permissions</th>
                  <th className="px-4 py-3 font-medium">Users</th>
                  <th className="px-4 py-3 font-medium">Type</th>
                </tr>
              </thead>
              <tbody>
                {roles.map((r) => (
                  <tr
                    key={r.id}
                    onClick={() => setEditingRole(r)}
                    className="cursor-pointer border-b border-slate-100 last:border-0 hover:bg-slate-50 dark:border-slate-800/60 dark:hover:bg-slate-800/40"
                  >
                    <td className="px-4 py-3">
                      <p className="font-medium text-slate-900 dark:text-white">{r.name}</p>
                      {r.description && <p className="text-xs text-slate-400">{r.description}</p>}
                    </td>
                    <td className="px-4 py-3 text-slate-600 dark:text-slate-300">{r.permissions.length}</td>
                    <td className="px-4 py-3 text-slate-600 dark:text-slate-300">{r.userCount}</td>
                    <td className="px-4 py-3">
                      <Badge tone={r.isSystemRole ? 'slate' : 'brand'}>{r.isSystemRole ? 'Built-in' : 'Custom'}</Badge>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          ) : (
            <EmptyState icon={Shield} title="No roles found" />
          )}
        </div>
      )}

      <UserFormDrawer open={userFormOpen} onClose={() => setUserFormOpen(false)} />
      <RoleFormDrawer open={roleFormOpen} onClose={() => setRoleFormOpen(false)} />
      <RolePermissionsDrawer role={editingRole} onClose={() => setEditingRole(null)} />
    </div>
  )
}
