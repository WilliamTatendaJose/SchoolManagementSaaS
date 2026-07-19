import { Monitor, Moon, Sun } from 'lucide-react'
import type { ThemeMode } from '../../theme/themeStore'
import { useThemeStore } from '../../theme/themeStore'

const OPTIONS: { mode: ThemeMode; icon: typeof Sun; label: string }[] = [
  { mode: 'light', icon: Sun, label: 'Light theme' },
  { mode: 'system', icon: Monitor, label: 'Match system theme' },
  { mode: 'dark', icon: Moon, label: 'Dark theme' },
]

export function ThemeToggle() {
  const mode = useThemeStore((s) => s.mode)
  const setMode = useThemeStore((s) => s.setMode)

  return (
    <div className="flex items-center rounded-lg bg-slate-100 p-0.5 dark:bg-slate-800">
      {OPTIONS.map(({ mode: optionMode, icon: Icon, label }) => {
        const active = mode === optionMode
        return (
          <button
            key={optionMode}
            type="button"
            onClick={() => setMode(optionMode)}
            aria-label={label}
            aria-pressed={active}
            title={label}
            className={[
              'flex h-7 w-7 items-center justify-center rounded-md transition-colors',
              active
                ? 'bg-white text-brand-600 shadow-sm dark:bg-slate-700 dark:text-brand-400'
                : 'text-slate-400 hover:text-slate-600 dark:text-slate-500 dark:hover:text-slate-300',
            ].join(' ')}
          >
            <Icon className="h-3.5 w-3.5" strokeWidth={2.25} />
          </button>
        )
      })}
    </div>
  )
}
