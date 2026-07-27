import { api } from './client'
import type {
  AnswerReviewResult,
  AttemptResultDto,
  OnboardLearnerResult,
  ReviewQueueDto,
  SessionDto,
  TodayDto,
} from './types'

// One function per endpoint (doc 12 §2); screens compose these via TanStack Query.

export function onboard(command: {
  displayName: string
  targetLevel: string
  minutesPerDay: number
  motivationType?: string
}): Promise<OnboardLearnerResult> {
  return api('/onboarding', { method: 'POST', body: command })
}

export function fetchToday(): Promise<TodayDto> {
  return api('/today')
}

export function planSession(timeBudgetMinutes?: number): Promise<SessionDto> {
  return api('/sessions', { method: 'POST', body: { timeBudgetMinutes: timeBudgetMinutes ?? null } })
}

export function fetchCurrentSession(): Promise<SessionDto> {
  return api('/sessions/current')
}

export function startSession(sessionId: string): Promise<SessionDto> {
  return api(`/sessions/${sessionId}/start`, { method: 'POST' })
}

export function completeActivity(sessionId: string, activityId: string): Promise<SessionDto> {
  return api(`/sessions/${sessionId}/activities/${activityId}/complete`, { method: 'POST' })
}

export function completeSession(sessionId: string): Promise<SessionDto> {
  return api(`/sessions/${sessionId}/complete`, { method: 'POST' })
}

export function abandonSession(sessionId: string): Promise<void> {
  return api(`/sessions/${sessionId}/abandon`, { method: 'POST' })
}

export function recordAttempt(
  exerciseInstanceId: string,
  attempt: { response: string; hintsUsed?: number; latencyMs?: number },
): Promise<AttemptResultDto> {
  return api(`/exercises/${exerciseInstanceId}/attempts`, { method: 'POST', body: attempt })
}

export function fetchReviewQueue(limit = 50): Promise<ReviewQueueDto> {
  return api(`/reviews/queue?limit=${limit}`)
}

export function answerReview(
  reviewItemId: string,
  rating: 1 | 2 | 3 | 4,
  elapsedMs?: number,
): Promise<AnswerReviewResult> {
  return api(`/reviews/${reviewItemId}/answers`, { method: 'POST', body: { rating, elapsedMs } })
}
