import { fireEvent, screen } from '@testing-library/react'
import { vi } from 'vitest'
import { LessonPlayer } from './LessonPlayer'
import { useAppStore } from '@/app/store'
import { renderWithQuery, resetAppState, stubApi } from '@/test/testUtils'

const session = {
  sessionId: 's-1',
  learnerId: 'learner-1',
  status: 'InProgress',
  timeBudgetMinutes: 15,
  reviewShare: 0.5,
  newShare: 0.5,
  rationale: [],
  plannerVersion: 'planner-v1',
  activities: [
    {
      activityId: 'a-1',
      orderIndex: 0,
      kind: 'Lesson',
      status: 'InProgress',
      estimatedMinutes: 10,
      exercises: [
        {
          exerciseInstanceId: 'ex-1',
          orderIndex: 0,
          type: 'MultipleChoice',
          stem: 'Wie ___ du?',
          options: ['heißt', 'heiße', 'heißen'],
          source: 'Curated',
        },
      ],
    },
  ],
}

describe('LessonPlayer', () => {
  beforeEach(resetAppState)
  afterEach(() => vi.unstubAllGlobals())

  it('submits an answer and shows the backend feedback', async () => {
    useAppStore.setState({ learnerId: 'learner-1', screen: 'lesson' })
    const { calls } = stubApi({
      'GET /api/v1/sessions/current': session,
      'POST /api/v1/exercises/ex-1/attempts': {
        attemptId: 'at-1',
        outcome: 1,
        evaluatedBy: 'AnswerKey',
        correctAnswer: 'heißt',
        explanation: 'Second person singular of heißen.',
        masteryDeltas: [],
      },
    })

    renderWithQuery(<LessonPlayer />)

    expect(await screen.findByText('Wie ___ du?')).toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: 'heißt' }))
    fireEvent.click(screen.getByRole('button', { name: 'Check' }))

    expect(await screen.findByText(/Richtig/)).toBeInTheDocument()
    expect(screen.getByText('Second person singular of heißen.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Continue →' })).toBeInTheDocument()

    const post = calls.find((c) => c.method === 'POST')
    expect(post?.url).toBe('/api/v1/exercises/ex-1/attempts')
    expect(post?.body).toEqual({ response: 'heißt' })
  })

  it('offers the way back when no session is open', async () => {
    useAppStore.setState({ learnerId: 'learner-1', screen: 'lesson' })
    stubApi({}) // /sessions/current falls through to the 404 stub

    renderWithQuery(<LessonPlayer />)

    expect(
      await screen.findByText('No open session. Plan one from the Today screen.'),
    ).toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: '← Back to Today' }))
    expect(useAppStore.getState().screen).toBe('today')
  })
})
