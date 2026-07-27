import { useState } from 'react'
import type { FormEvent } from 'react'
import { useMutation } from '@tanstack/react-query'
import { onboard } from '@/shared/api/lif'
import { useAppStore } from '@/app/store'

const TARGET_LEVELS = ['A1.2', 'A2.1', 'A2.2', 'B1.1', 'B1.2', 'B2.1'] as const
const PACE_OPTIONS = [10, 15, 20, 30] as const

/** Beginner fast path (doc 09): three questions, no placement test, straight to A1.1. */
export function OnboardingScreen() {
  const onboarded = useAppStore((s) => s.onboarded)
  const [displayName, setDisplayName] = useState('')
  const [targetLevel, setTargetLevel] = useState<string>('B1.1')
  const [minutesPerDay, setMinutesPerDay] = useState<number>(15)

  const mutation = useMutation({
    mutationFn: onboard,
    onSuccess: (result) => onboarded(result.learnerId),
  })

  const submit = (event: FormEvent) => {
    event.preventDefault()
    if (displayName.trim().length === 0 || mutation.isPending) {
      return
    }
    mutation.mutate({ displayName: displayName.trim(), targetLevel, minutesPerDay })
  }

  return (
    <section className="card onboarding">
      <h2>Willkommen! Let's set up your German journey.</h2>
      <p className="muted">You start at A1.1 — no placement test, just three questions.</p>

      <form onSubmit={submit}>
        <label>
          What should we call you?
          <input
            value={displayName}
            onChange={(e) => setDisplayName(e.target.value)}
            placeholder="Your name"
            autoFocus
          />
        </label>

        <label>
          Target level
          <select value={targetLevel} onChange={(e) => setTargetLevel(e.target.value)}>
            {TARGET_LEVELS.map((level) => (
              <option key={level} value={level}>
                {level}
              </option>
            ))}
          </select>
        </label>

        <fieldset>
          <legend>Minutes per day</legend>
          <div className="option-row">
            {PACE_OPTIONS.map((minutes) => (
              <button
                key={minutes}
                type="button"
                className={minutes === minutesPerDay ? 'chip selected' : 'chip'}
                aria-pressed={minutes === minutesPerDay}
                onClick={() => setMinutesPerDay(minutes)}
              >
                {minutes} min
              </button>
            ))}
          </div>
        </fieldset>

        <button type="submit" className="primary" disabled={displayName.trim().length === 0 || mutation.isPending}>
          {mutation.isPending ? 'Creating…' : 'Start learning'}
        </button>
        {mutation.isError && <p role="alert">Could not create your profile: {mutation.error.message}</p>}
      </form>
    </section>
  )
}
