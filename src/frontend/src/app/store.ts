import { create } from 'zustand'
import { getLearnerId, setLearnerId } from '@/shared/api/identity'

export type Screen = 'today' | 'lesson' | 'review'

interface AppState {
  learnerId: string | null
  screen: Screen
  onboarded: (learnerId: string) => void
  navigate: (screen: Screen) => void
  signOut: () => void
}

// Session/UI state in Zustand, server state in TanStack Query (ADR-012).
export const useAppStore = create<AppState>()((set) => ({
  learnerId: getLearnerId(),
  screen: 'today',
  onboarded: (learnerId) => {
    setLearnerId(learnerId)
    set({ learnerId, screen: 'today' })
  },
  navigate: (screen) => set({ screen }),
  signOut: () => {
    setLearnerId(null)
    set({ learnerId: null, screen: 'today' })
  },
}))
