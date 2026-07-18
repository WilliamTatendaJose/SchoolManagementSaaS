import type { PermissionGroupDto } from '../../api/types'

export function PermissionChecklist({
  groups,
  selected,
  onToggle,
  onToggleModule,
}: {
  groups: PermissionGroupDto[] | undefined
  selected: Set<string>
  onToggle: (code: string) => void
  onToggleModule: (codes: string[]) => void
}) {
  return (
    <div className="max-h-72 space-y-3 overflow-y-auto rounded-lg border border-slate-200 p-3 dark:border-slate-800">
      {groups?.map((group) => {
        const codes = group.permissions.map((p) => p.code)
        const allSelected = codes.every((c) => selected.has(c))
        return (
          <div key={group.module}>
            <div className="mb-1 flex items-center justify-between">
              <span className="text-xs font-semibold uppercase tracking-wide text-slate-500">{group.module}</span>
              <button
                type="button"
                onClick={() => onToggleModule(codes)}
                className="text-xs font-medium text-brand-600 hover:underline dark:text-brand-300"
              >
                {allSelected ? 'Clear' : 'Select all'}
              </button>
            </div>
            <div className="grid grid-cols-2 gap-x-3 gap-y-1">
              {group.permissions.map((p) => (
                <label
                  key={p.code}
                  className="flex cursor-pointer items-center gap-2 py-0.5 text-sm text-slate-700 dark:text-slate-300"
                >
                  <input
                    type="checkbox"
                    checked={selected.has(p.code)}
                    onChange={() => onToggle(p.code)}
                    className="h-3.5 w-3.5 rounded border-slate-300 text-brand-600 focus:ring-brand-500"
                  />
                  {p.name}
                </label>
              ))}
            </div>
          </div>
        )
      })}
    </div>
  )
}
