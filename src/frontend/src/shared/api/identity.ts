const LEARNER_KEY = 'lif.learnerId'

/** Phase-1 identity: the learner id doubles as the device identity (X-Learner-Id header). */
export function getLearnerId(): string | null {
  return localStorage.getItem(LEARNER_KEY)
}

export function setLearnerId(learnerId: string | null): void {
  if (learnerId === null) {
    localStorage.removeItem(LEARNER_KEY)
  } else {
    localStorage.setItem(LEARNER_KEY, learnerId)
  }
}
