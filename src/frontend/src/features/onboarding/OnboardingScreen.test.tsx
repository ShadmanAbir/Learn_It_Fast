import { fireEvent, screen, waitFor } from '@testing-library/react'
import { vi } from 'vitest'
import { OnboardingScreen } from './OnboardingScreen'
import { useAppStore } from '@/app/store'
import { renderWithQuery, resetAppState, stubApi } from '@/test/testUtils'

describe('OnboardingScreen', () => {
  beforeEach(resetAppState)
  afterEach(() => vi.unstubAllGlobals())

  it('creates the learner and stores the identity', async () => {
    const { calls } = stubApi({
      'POST /api/v1/onboarding': {
        learnerId: 'learner-9',
        currentLevel: 'A1.1',
        targetLevel: 'B1.1',
        minutesPerDay: 20,
        curriculumVersionId: 'cv-1',
      },
    })

    renderWithQuery(<OnboardingScreen />)

    fireEvent.change(screen.getByPlaceholderText('Your name'), { target: { value: '  Anna ' } })
    fireEvent.click(screen.getByRole('button', { name: '20 min' }))
    fireEvent.click(screen.getByRole('button', { name: 'Start learning' }))

    await waitFor(() => expect(useAppStore.getState().learnerId).toBe('learner-9'))
    expect(localStorage.getItem('lif.learnerId')).toBe('learner-9')

    const post = calls.find((c) => c.method === 'POST')
    expect(post?.url).toBe('/api/v1/onboarding')
    expect(post?.body).toEqual({ displayName: 'Anna', targetLevel: 'B1.1', minutesPerDay: 20 })
  })

  it('keeps submit disabled until a name is entered', () => {
    stubApi({})

    renderWithQuery(<OnboardingScreen />)

    expect(screen.getByRole('button', { name: 'Start learning' })).toBeDisabled()
  })
})
