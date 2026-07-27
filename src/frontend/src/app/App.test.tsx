import { screen } from '@testing-library/react'
import { vi } from 'vitest'
import { App } from './App'
import { useAppStore } from './store'
import { renderWithQuery, resetAppState, stubApi } from '@/test/testUtils'

const learner = {
  learnerId: 'learner-1',
  displayName: 'Anna',
  currentLevel: 'A1.1',
  targetLevel: 'B1.1',
  minutesPerDay: 15,
  curriculumVersionId: 'cv-1',
  motivationType: null,
}

describe('App', () => {
  beforeEach(resetAppState)
  afterEach(() => vi.unstubAllGlobals())

  it('shows onboarding and backend status when no learner exists yet', async () => {
    stubApi({ 'GET /health/ready': true })

    renderWithQuery(<App />)

    expect(screen.getByRole('heading', { name: 'LIF' })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: /Willkommen/ })).toBeInTheDocument()
    expect(await screen.findByText('ready')).toBeInTheDocument()
  })

  it('shows the Today screen for a returning learner', async () => {
    useAppStore.setState({ learnerId: 'learner-1' })
    stubApi({
      'GET /health/ready': true,
      'GET /api/v1/today': { learner, dueReviewCount: 3, openSession: null },
    })

    renderWithQuery(<App />)

    expect(await screen.findByText(/Guten Morgen, Anna!/)).toBeInTheDocument()
    expect(screen.getByText('3 items due.')).toBeInTheDocument()
  })
})
