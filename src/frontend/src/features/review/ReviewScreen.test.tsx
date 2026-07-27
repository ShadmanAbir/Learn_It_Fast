import { fireEvent, screen } from '@testing-library/react'
import { vi } from 'vitest'
import { ReviewScreen } from './ReviewScreen'
import { useAppStore } from '@/app/store'
import { renderWithQuery, resetAppState, stubApi } from '@/test/testUtils'

describe('ReviewScreen', () => {
  beforeEach(resetAppState)
  afterEach(() => vi.unstubAllGlobals())

  it('rates a card and reaches the empty state', async () => {
    useAppStore.setState({ learnerId: 'learner-1', screen: 'review' })
    const { calls } = stubApi({
      'GET /api/v1/reviews/queue?limit=50': {
        dueCount: 1,
        items: [
          {
            reviewItemId: 'ri-1',
            kind: 'Word',
            itemRef: 'w-1',
            dueAt: '2026-07-26T08:00:00Z',
            priority: 1,
          },
        ],
      },
      'POST /api/v1/reviews/ri-1/answers': {
        reviewItemId: 'ri-1',
        nextDueAt: '2026-07-27T08:00:00Z',
        stability: 1.4,
        wordStatus: 'Learning',
      },
    })

    renderWithQuery(<ReviewScreen />)

    expect(await screen.findByText('Word review')).toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: '3 Good' }))

    expect(await screen.findByRole('heading', { name: /Queue clear/ })).toBeInTheDocument()
    const post = calls.find((c) => c.method === 'POST')
    expect(post?.url).toBe('/api/v1/reviews/ri-1/answers')
    expect(post?.body).toEqual({ rating: 3 })
  })

  it('shows the empty state when nothing is due', async () => {
    useAppStore.setState({ learnerId: 'learner-1', screen: 'review' })
    stubApi({ 'GET /api/v1/reviews/queue?limit=50': { dueCount: 0, items: [] } })

    renderWithQuery(<ReviewScreen />)

    expect(await screen.findByRole('heading', { name: /Queue clear/ })).toBeInTheDocument()
    expect(screen.getByText('Nothing was due.')).toBeInTheDocument()
  })
})
