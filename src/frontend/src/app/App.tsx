import { useQuery } from '@tanstack/react-query'
import { fetchReadiness } from '@/shared/api/health'
import { useAppStore } from '@/app/store'
import { OnboardingScreen } from '@/features/onboarding/OnboardingScreen'
import { TodayScreen } from '@/features/today/TodayScreen'
import { LessonPlayer } from '@/features/lesson/LessonPlayer'
import { ReviewScreen } from '@/features/review/ReviewScreen'

/** App shell: onboarding gate, then Zustand screen switch (no router in Phase 1). */
export function App() {
  const learnerId = useAppStore((s) => s.learnerId)
  const screen = useAppStore((s) => s.screen)

  const health = useQuery({
    queryKey: ['health', 'ready'],
    queryFn: fetchReadiness,
  })

  const status = health.isLoading ? 'checking…' : health.data ? 'ready' : 'unreachable'

  return (
    <main className="shell">
      <header className="topbar">
        <h1>LIF</h1>
        <p role="status" className="muted">
          Backend: <strong>{status}</strong>
        </p>
      </header>

      {learnerId === null ? (
        <OnboardingScreen />
      ) : screen === 'lesson' ? (
        <LessonPlayer />
      ) : screen === 'review' ? (
        <ReviewScreen />
      ) : (
        <TodayScreen />
      )}
    </main>
  )
}
