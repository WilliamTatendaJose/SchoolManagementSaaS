import { useQuery } from '@tanstack/react-query'
import { Package, Pencil, Plus } from 'lucide-react'
import { useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { fetchAssets } from '../../api/assets'
import type { AssetDto } from '../../api/types'
import { useAuthStore } from '../../auth/authStore'
import { Badge } from '../../components/ui/Badge'
import { Button } from '../../components/ui/Button'
import { EmptyState } from '../../components/ui/EmptyState'
import { PageHeader } from '../../components/ui/PageHeader'
import { Pagination } from '../../components/ui/Pagination'
import { SearchInput } from '../../components/ui/SearchInput'
import { useDebouncedValue } from '../../hooks/useDebouncedValue'
import { AssetFormDrawer } from './AssetFormDrawer'

const PAGE_SIZE = 15

const CONDITION_TONE: Record<string, 'emerald' | 'brand' | 'amber' | 'slate'> = {
  New: 'emerald',
  Good: 'brand',
  Fair: 'amber',
  Poor: 'amber',
  Damaged: 'slate',
}

const currency = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' })

export function AssetsListPage() {
  const canManage = useAuthStore((s) => s.hasPermission('assets.manage'))
  const [params, setParams] = useSearchParams()
  const [formOpen, setFormOpen] = useState(false)
  const [editingAsset, setEditingAsset] = useState<AssetDto | undefined>()

  const page = Number(params.get('page') ?? '1')
  const search = params.get('q') ?? ''
  const debouncedSearch = useDebouncedValue(search, 350)

  function updateParam(key: string, value: string) {
    const next = new URLSearchParams(params)
    if (value) next.set(key, value)
    else next.delete(key)
    if (key !== 'page') next.delete('page')
    setParams(next, { replace: true })
  }

  const { data, isLoading, isFetching } = useQuery({
    queryKey: ['assets', { page, debouncedSearch }],
    queryFn: () => fetchAssets({ pageNumber: page, pageSize: PAGE_SIZE, searchTerm: debouncedSearch || undefined }),
    placeholderData: (prev) => prev,
  })

  return (
    <div>
      <PageHeader
        title="Assets"
        description={data ? `${data.totalCount} assets` : 'Equipment, furniture and other school property'}
        actions={
          canManage && (
            <Button
              onClick={() => {
                setEditingAsset(undefined)
                setFormOpen(true)
              }}
            >
              <Plus className="h-4 w-4" strokeWidth={2.5} />
              Add asset
            </Button>
          )
        }
      />

      <SearchInput
        placeholder="Search by name or asset number…"
        value={search}
        onChange={(e) => updateParam('q', e.target.value)}
        className="mb-4 sm:max-w-xs"
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
            <table className={`w-full text-left text-sm transition-opacity ${isFetching ? 'opacity-60' : ''}`}>
              <thead>
                <tr className="border-b border-slate-200 text-xs uppercase tracking-wider text-slate-400 dark:border-slate-800">
                  <th className="px-4 py-3 font-medium">Asset</th>
                  <th className="px-4 py-3 font-medium">Category</th>
                  <th className="px-4 py-3 font-medium">Location</th>
                  <th className="px-4 py-3 font-medium">Assigned to</th>
                  <th className="px-4 py-3 font-medium">Condition</th>
                  <th className="px-4 py-3 font-medium text-right">Value</th>
                  <th className="px-4 py-3 font-medium">Status</th>
                  {canManage && <th className="w-12 px-2 py-3" />}
                </tr>
              </thead>
              <tbody>
                {data.items.map((a) => (
                  <tr key={a.id} className="border-b border-slate-100 last:border-0 dark:border-slate-800/60">
                    <td className="px-4 py-3">
                      <p className="font-medium text-slate-900 dark:text-white">{a.name}</p>
                      <p className="text-xs text-slate-400">{a.assetNumber}</p>
                    </td>
                    <td className="px-4 py-3 text-slate-600 dark:text-slate-300">{a.category}</td>
                    <td className="px-4 py-3 text-slate-600 dark:text-slate-300">{a.location || '—'}</td>
                    <td className="px-4 py-3 text-slate-600 dark:text-slate-300">{a.assignedToName || '—'}</td>
                    <td className="px-4 py-3">
                      <Badge tone={CONDITION_TONE[a.condition] ?? 'slate'}>{a.condition}</Badge>
                    </td>
                    <td className="px-4 py-3 text-right text-slate-600 dark:text-slate-300">
                      {a.purchasePrice != null ? currency.format(a.purchasePrice) : '—'}
                    </td>
                    <td className="px-4 py-3">
                      <Badge tone={a.isActive ? 'emerald' : 'slate'}>{a.isActive ? 'In service' : 'Retired'}</Badge>
                    </td>
                    {canManage && (
                      <td className="px-2 py-3">
                        <button
                          onClick={() => {
                            setEditingAsset(a)
                            setFormOpen(true)
                          }}
                          className="rounded-md p-1.5 text-slate-400 hover:bg-slate-100 hover:text-slate-600 dark:hover:bg-slate-800 dark:hover:text-slate-300"
                          aria-label="Edit"
                        >
                          <Pencil className="h-3.5 w-3.5" strokeWidth={2} />
                        </button>
                      </td>
                    )}
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
            icon={Package}
            title="No assets found"
            description={search ? 'Try a different search.' : 'Register your first asset to start tracking it.'}
            action={
              canManage &&
              !search && (
                <Button
                  onClick={() => {
                    setEditingAsset(undefined)
                    setFormOpen(true)
                  }}
                >
                  <Plus className="h-4 w-4" strokeWidth={2.5} />
                  Add asset
                </Button>
              )
            }
          />
        )}
      </div>

      <AssetFormDrawer open={formOpen} onClose={() => setFormOpen(false)} asset={editingAsset} />
    </div>
  )
}
