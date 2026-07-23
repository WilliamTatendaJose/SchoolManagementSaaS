import { useQuery } from '@tanstack/react-query'
import { useState } from 'react'
import { fetchGuardians } from '../../api/guardians'
import { Avatar } from '../../components/ui/Avatar'
import { SearchInput } from '../../components/ui/SearchInput'
import { useDebouncedValue } from '../../hooks/useDebouncedValue'

/**
 * Search-and-select for the guardian a parent login belongs to. Setting this is what
 * actually lets a parent see their children in the portal (it sets Guardian.UserId, which
 * ParentChildAccess resolves through) - creating a guardian and a login separately does
 * NOT connect them on its own.
 */
export function GuardianPicker({
  selectedId,
  selectedLabel,
  onSelect,
}: {
  selectedId: string | null
  selectedLabel: string
  onSelect: (id: string | null, label: string) => void
}) {
  const [search, setSearch] = useState('')
  const debounced = useDebouncedValue(search, 300)

  const { data, isFetching } = useQuery({
    queryKey: ['guardian-search', debounced],
    queryFn: () => fetchGuardians({ pageNumber: 1, pageSize: 6, searchTerm: debounced }),
    enabled: debounced.length >= 2 && !selectedId,
  })

  return (
    <div>
      <span className="mb-1.5 block text-sm font-medium text-slate-700 dark:text-slate-300">
        Linked guardian <span className="font-normal text-slate-400">(for parent logins)</span>
      </span>

      {selectedId ? (
        <div className="flex items-center justify-between rounded-lg border border-brand-200 bg-brand-50 px-3 py-2 dark:border-brand-900 dark:bg-brand-950/40">
          <span className="text-sm font-medium text-brand-800 dark:text-brand-200">{selectedLabel || 'Linked guardian'}</span>
          <button
            type="button"
            onClick={() => {
              onSelect(null, '')
              setSearch('')
            }}
            className="text-xs font-medium text-brand-600 hover:underline dark:text-brand-300"
          >
            Clear
          </button>
        </div>
      ) : (
        <>
          <SearchInput
            placeholder="Search guardians by name…"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
          {debounced.length >= 2 && (
            <div className="mt-2 max-h-44 overflow-y-auto rounded-lg border border-slate-200 dark:border-slate-800">
              {isFetching ? (
                <p className="px-3 py-3 text-sm text-slate-400">Searching…</p>
              ) : data && data.items.length > 0 ? (
                data.items.map((g) => (
                  <button
                    type="button"
                    key={g.id}
                    onClick={() => onSelect(g.id, g.phone ? `${g.fullName} · ${g.phone}` : g.fullName)}
                    className="flex w-full items-center gap-3 border-b border-slate-100 px-3 py-2 text-left last:border-0 hover:bg-slate-50 dark:border-slate-800 dark:hover:bg-slate-800"
                  >
                    <Avatar name={g.fullName} size="sm" />
                    <div>
                      <p className="text-sm font-medium text-slate-800 dark:text-slate-100">{g.fullName}</p>
                      <p className="text-xs text-slate-400">{g.phone ?? g.email ?? '—'}</p>
                    </div>
                  </button>
                ))
              ) : (
                <p className="px-3 py-3 text-sm text-slate-400">No guardians found.</p>
              )}
            </div>
          )}
        </>
      )}
      <p className="mt-1 text-xs text-slate-400">
        Connects this login to a guardian record so they can see their children's results and fees in the parent portal.
      </p>
    </div>
  )
}
