import { useEffect } from 'react'
import { resolveTheme, useThemeStore } from './themeStore'

function applyTheme(resolved: 'light' | 'dark') {
  const root = document.documentElement
  root.classList.toggle('dark', resolved === 'dark')
  root.style.colorScheme = resolved
}

/** Keeps <html class="dark"> (and color-scheme, for native controls) in sync with the
 *  theme store - including live updates when mode is 'system' and the OS preference
 *  changes underneath us. The initial pre-paint class is set synchronously by the
 *  inline script in index.html; this takes over from there. */
export function ThemeProvider({ children }: { children: React.ReactNode }) {
  const mode = useThemeStore((s) => s.mode)

  useEffect(() => {
    applyTheme(resolveTheme(mode))

    if (mode !== 'system') return

    const media = window.matchMedia('(prefers-color-scheme: dark)')
    const onChange = () => applyTheme(resolveTheme('system'))
    media.addEventListener('change', onChange)
    return () => media.removeEventListener('change', onChange)
  }, [mode])

  return children
}
