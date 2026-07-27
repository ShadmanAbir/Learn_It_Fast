import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { fetchToday, planSession } from '@/shared/api/lif'
import { useAppStore } from '@/app/store'

/** Session hub (doc 13 §2): one dominant CTA, time-budget switcher, review shortcut. */
export function TodayScreen() {
  const navigate = useAppStore((s) => s.navigate)
  const queryClient = useQueryClient()

  const today = useQuery({ queryKey: ['today'], queryFn: fetchToday })

  const plan = useMutation({
    mutationFn: (timeBudgetMinutes?: number) => planSession(timeBudgetMinutes),
    onSuccess: (session) => {
      queryClient.setQueryData(['session', 'current'], session)
      navigate('lesson')
    },
  })

  if (today.isLoading) {
    return <p className="muted">Loading your day…</p>
  }
  if (today.isError) {
    return <p role="alert">Could not load your day: {today.error.message}</p>
  }

  const { learner, dueReviewCount, openSession } = today.data!

  return (
    <section className="today">
      <h2>Guten Morgen, {learner.displayName}!</h2>
      <p className="muted">
        Level {learner.currentLevel} · goal {learner.targetLevel} · {learner.minutesPerDay} min/day
      </p>

      <div className="card">
        <h3>Today's session</h3>
        {openSession ? (
          <>
            <p>You have a session in progress ({openSession.activities.length} activities).</p>
            <button className="primary" onClick={() => navigate('lesson')}>
              Resume session
            </button>
          </>
        ) : (
          <>
            <p>The planner builds your session from due reviews and your learning frontier.</p>
            <button className="primary" onClick={() => plan.mutate(undefined)} disabled={plan.isPending}>
              {plan.isPending ? 'Planning…' : '▶ Start session'}
            </button>
            <div className="option-row">
              <span className="muted">Short on time?</span>
              <button className="chip" onClick={() => plan.mutate(10)} disabled={plan.isPending}>
                10 min
              </button>
              <button className="chip" onClick={() => plan.mutate(20)} disabled={plan.isPending}>
                20 min
              </button>
            </div>
            {plan.isError && <p role="alert">Could not plan a session: {plan.error.message}</p>}
          </>
        )}
      </div>

      <div className="card">
        <h3>Reviews</h3>
        <p>
          {dueReviewCount === 0
            ? 'Nothing due right now — the queue is clear.'
            : `${dueReviewCount} item${dueReviewCount === 1 ? '' : 's'} due.`}
        </p>
        <button onClick={() => navigate('review')} disabled={dueReviewCount === 0}>
          Review now
        </button>
      </div>
    </section>
  )
}
