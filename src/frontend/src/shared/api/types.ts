// DTO mirrors of the backend Application layer (camelCase per ASP.NET JSON defaults).
// The answer key never appears here — evaluation happens on the backend (doc 12 §3).

export interface LearnerDto {
  learnerId: string
  displayName: string
  currentLevel: string
  targetLevel: string
  minutesPerDay: number
  curriculumVersionId: string
  motivationType: string | null
}

export interface OnboardLearnerResult {
  learnerId: string
  currentLevel: string
  targetLevel: string
  minutesPerDay: number
  curriculumVersionId: string
}

export interface ExerciseDto {
  exerciseInstanceId: string
  orderIndex: number
  type: 'MultipleChoice' | 'Cloze' | 'Matching' | 'Ordering'
  stem: string
  options: string[]
  source: string
}

export interface ActivityDto {
  activityId: string
  orderIndex: number
  kind: string
  status: string
  estimatedMinutes: number
  exercises: ExerciseDto[]
}

export interface SessionDto {
  sessionId: string
  learnerId: string
  status: string
  timeBudgetMinutes: number
  reviewShare: number
  newShare: number
  rationale: string[]
  plannerVersion: string
  activities: ActivityDto[]
}

export interface TodayDto {
  learner: LearnerDto
  dueReviewCount: number
  openSession: SessionDto | null
}

export interface MasteryDeltaDto {
  conceptId: string
  from: number
  to: number
  needsRemediation: boolean
}

export interface AttemptResultDto {
  attemptId: string
  outcome: number
  evaluatedBy: string
  correctAnswer: string
  explanation: string | null
  masteryDeltas: MasteryDeltaDto[]
}

export interface ReviewQueueItemDto {
  reviewItemId: string
  kind: string
  itemRef: string
  dueAt: string
  priority: number
}

export interface ReviewQueueDto {
  dueCount: number
  items: ReviewQueueItemDto[]
}

export interface AnswerReviewResult {
  reviewItemId: string
  nextDueAt: string
  stability: number
  wordStatus: string | null
}
