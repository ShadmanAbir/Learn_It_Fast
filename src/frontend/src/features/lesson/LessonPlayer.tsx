import { useEffect, useRef, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  completeActivity,
  completeSession,
  fetchCurrentSession,
  recordAttempt,
  startSession,
} from '@/shared/api/lif'
import type { AttemptResultDto, ExerciseDto } from '@/shared/api/types'
import { useAppStore } from '@/app/store'

/**
 * Lesson player (doc 13 §3): one exercise per screen, feedback always names the correct
 * answer, Continue drives the plan forward. All evaluation happens on the backend.
 */
export function LessonPlayer() {
  const navigate = useAppStore((s) => s.navigate)
  const queryClient = useQueryClient()

  const session = useQuery({
    queryKey: ['session', 'current'],
    queryFn: fetchCurrentSession,
    retry: false,
  })

  const [activityIdx, setActivityIdx] = useState<number | null>(null)
  const [exerciseIdx, setExerciseIdx] = useState(0)
  const [feedback, setFeedback] = useState<AttemptResultDto | null>(null)
  const [finished, setFinished] = useState(false)
  const startRequested = useRef(false)

  const start = useMutation({
    mutationFn: startSession,
    onSuccess: (updated) => queryClient.setQueryData(['session', 'current'], updated),
  })

  const attempt = useMutation({
    mutationFn: (input: { exerciseInstanceId: string; response: string }) =>
      recordAttempt(input.exerciseInstanceId, { response: input.response }),
    onSuccess: setFeedback,
  })

  const finishActivity = useMutation({
    mutationFn: (input: { sessionId: string; activityId: string }) =>
      completeActivity(input.sessionId, input.activityId),
    onSuccess: (updated) => queryClient.setQueryData(['session', 'current'], updated),
  })

  const finishSession = useMutation({
    mutationFn: completeSession,
    onSuccess: () => {
      setFinished(true)
      void queryClient.invalidateQueries({ queryKey: ['today'] })
      void queryClient.removeQueries({ queryKey: ['session', 'current'] })
    },
  })

  // A freshly planned session must be started before attempts are accepted.
  useEffect(() => {
    if (session.data && session.data.status === 'Planned' && !startRequested.current) {
      startRequested.current = true
      start.mutate(session.data.sessionId)
    }
  }, [session.data, start])

  if (session.isLoading) {
    return <p className="muted">Loading your session…</p>
  }
  if (session.isError || !session.data) {
    return (
      <section className="card">
        <p>No open session. Plan one from the Today screen.</p>
        <button onClick={() => navigate('today')}>← Back to Today</button>
      </section>
    )
  }

  if (finished) {
    return (
      <section className="card">
        <h2>Session complete! 🎉</h2>
        <p className="muted">The planner logged your progress; reviews are scheduled.</p>
        <button className="primary" autoFocus onClick={() => navigate('today')}>
          Back to Today
        </button>
      </section>
    )
  }

  const activities = session.data.activities
  const openIdx = activities.findIndex((a) => a.status !== 'Completed' && a.status !== 'Skipped')
  const idx = activityIdx ?? (openIdx === -1 ? activities.length - 1 : openIdx)
  const activity = activities[idx]
  const exercise: ExerciseDto | undefined = activity?.exercises[exerciseIdx]

  const advance = () => {
    setFeedback(null)
    if (activity && exerciseIdx + 1 < activity.exercises.length) {
      setExerciseIdx(exerciseIdx + 1)
      return
    }
    finishActivity.mutate(
      { sessionId: session.data.sessionId, activityId: activity.activityId },
      {
        onSuccess: () => {
          if (idx + 1 < activities.length) {
            setActivityIdx(idx + 1)
            setExerciseIdx(0)
          } else {
            finishSession.mutate(session.data.sessionId)
          }
        },
      },
    )
  }

  const totalInActivity = activity?.exercises.length ?? 0

  return (
    <section className="lesson">
      <header className="lesson-header">
        <button onClick={() => navigate('today')}>← Exit</button>
        <span>
          {activity?.kind} · block {idx + 1}/{activities.length}
          {totalInActivity > 0 && ` · step ${exerciseIdx + 1}/${totalInActivity}`}
        </span>
      </header>

      {exercise ? (
        <ExerciseCard
          key={exercise.exerciseInstanceId}
          exercise={exercise}
          feedback={feedback}
          submitting={attempt.isPending}
          onSubmit={(response) =>
            attempt.mutate({ exerciseInstanceId: exercise.exerciseInstanceId, response })
          }
          onContinue={advance}
        />
      ) : (
        <div className="card">
          <p>
            {activity?.kind === 'Review'
              ? 'This block is your review queue — you can also do it from the Review screen.'
              : 'Nothing to answer in this block.'}
          </p>
          <button className="primary" onClick={advance} disabled={finishActivity.isPending}>
            Continue →
          </button>
        </div>
      )}
      {attempt.isError && <p role="alert">Could not record your answer: {attempt.error.message}</p>}
    </section>
  )
}

function ExerciseCard({
  exercise,
  feedback,
  submitting,
  onSubmit,
  onContinue,
}: {
  exercise: ExerciseDto
  feedback: AttemptResultDto | null
  submitting: boolean
  onSubmit: (response: string) => void
  onContinue: () => void
}) {
  const [response, setResponse] = useState('')
  const answered = feedback !== null
  const correct = answered && feedback.outcome >= 1

  return (
    <div className="card exercise">
      <p className="stem" lang="de">
        {exercise.stem}
      </p>

      {exercise.type === 'MultipleChoice' ? (
        <div className="option-grid" role="group" aria-label="answer options">
          {exercise.options.map((option) => (
            <button
              key={option}
              lang="de"
              className={option === response ? 'option selected' : 'option'}
              aria-pressed={option === response}
              disabled={answered}
              onClick={() => setResponse(option)}
            >
              {option}
            </button>
          ))}
        </div>
      ) : (
        <input
          lang="de"
          value={response}
          placeholder="Your answer"
          disabled={answered}
          onChange={(e) => setResponse(e.target.value)}
          onKeyDown={(e) => {
            if (e.key === 'Enter' && response.trim() && !answered) {
              onSubmit(response)
            }
          }}
        />
      )}

      {!answered && (
        <button
          className="primary"
          disabled={response.trim().length === 0 || submitting}
          onClick={() => onSubmit(response)}
        >
          {submitting ? 'Checking…' : 'Check'}
        </button>
      )}

      {answered && (
        <div className={correct ? 'feedback correct' : 'feedback wrong'} role="status">
          <p>
            {correct ? '✅ Richtig!' : '❌ Not quite.'} Correct answer:{' '}
            <strong lang="de">{feedback.correctAnswer}</strong>
          </p>
          {feedback.explanation && <p className="muted">{feedback.explanation}</p>}
          <button className="primary" autoFocus onClick={onContinue}>
            Continue →
          </button>
        </div>
      )}
    </div>
  )
}
