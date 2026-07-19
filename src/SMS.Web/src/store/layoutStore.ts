import { create } from 'zustand'
import { persist } from 'zustand/middleware'

interface LayoutState {
  sidebarCollapsed: boolean
  toggleSidebar: () => void
}

/** Persisted sidebar collapse state, independent of theme - a user's layout preference
 *  should survive reloads the same way their theme choice does. */
export const useLayoutStore = create<LayoutState>()(
  persist(
    (set) => ({
      sidebarCollapsed: false,
      toggleSidebar: () => set((state) => ({ sidebarCollapsed: !state.sidebarCollapsed })),
    }),
    { name: 'sms-layout' },
  ),
)
