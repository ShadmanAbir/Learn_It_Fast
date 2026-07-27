import { useEffect, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { answerReview, fetchReviewQueue } from '@/shared/api/lif'
import type { ReviewQueueItemDto } from '@/shared/api/types'
import { useAppStore } from '@/app/store'

const RATINGS = [
  { value: 1, label: 'Again' },
  { value: 2, label: 'Hard' },
  { value: 3, label: 'Good' },
  { value: 4, label: 'Easy' },
] as const

/**
 * Review queue (doc 13 §5): keyboard-first (1–4), one item at a time. Phase 1 shows the
 * item kind + reference; card content rendering arrives with the vocabulary endpoints.
 */
export function ReviewScreen() {
  const navigate = useAppStore((s) => s.navigate)
  const queryClient = useQueryClient()

  const queue = useQuery({ queryKey: ['reviews', 'queue'], queryFn: () => fetchReviewQueue() })
  const [position, setPosition] = useState(0)

  const answer = useMutation({
    mutationFn: (input: { reviewItemId: string; rating: 1 | 2 | 3 | 4 }) =>
      answerReview(input.reviewItemId, input.rating),
    onSuccess: () => {
      setPosition((p) => p + 1)
      void queryClient.invalidateQueries({ queryKey: ['today'] })
    },
  })

  const items: ReviewQueueItemDto[] = queue.data?.items ?? []
  const current = items[position]

  // Anki-grade keyboard flow: 1–4 rate the current card.
  useEffect(() => {
    if (!current) {
      return
    }
    const onKey = (event: KeyboardEvent) => {
      const rating = Number(event.key)
      if (rating >= 1 && rating <= 4 && !answer.isPending) {
        answer.mutate({ reviewItemId: current.reviewItemId, rating: rating as 1 | 2 | 3 | 4 })
      }
    }
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [current, answer])

  if (queue.isLoading) {
    return <p className="muted">Loading your queue…</p>
  }
  if (queue.isError) {
    return <p role="alert">Could not load reviews: {queue.error.message}</p>
  }

  if (!current) {
    return (
      <section className="card">
        <h2>Queue clear! 🌱</h2>
        <p className="muted">
          {items.length > 0 ? `You rated ${items.length} items.` : 'Nothing was due.'}
        </p>
        <button
          className="primary"
          autoFocus
          onClick={() => {
            void queryClient.invalidateQueries({ queryKey: ['reviews', 'queue'] })
            navigate('today')
          }}
        >
          Back to Today
        </button>
      </section>
    )
  }

  return (
    <section className="review">
      <header className="lesson-header">
        <button onClick={() => navigate('today')}>← Exit</button>
        <span>
          {position + 1} / {items.length}
        </span>
      </header>

      <div className="card review-card">
        <p className="muted">{current.kind} review</p>
        <p className="stem">How well do you remember this {current.kind}?</p>
        <p className="muted">Rate yourself — keys 1–4 work too.</p>

        <div className="option-row">
          {RATINGS.map((rating) => (
            <button
              key={rating.value}
              className={rating.value === 1 ? 'chip danger' : 'chip'}
              disabled={answer.isPending}
              onClick={() =>
                answer.mutate({ reviewItemId: current.reviewItemId, rating: rating.value })
              }
            >
              {rating.value} {rating.label}
            </button>
          ))}
        </div>
        {answer.isError && <p role="alert">Could not save the rating: {answer.error.message}</p>}
      </div>
    </section>
  )
}
