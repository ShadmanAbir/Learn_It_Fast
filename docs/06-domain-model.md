# 06 — Domain Model

**Status:** Draft for review · DDD-style: bounded contexts, aggregates, entities, value objects, domain events, domain services.

---

## 1. Bounded Contexts

```
┌────────────────────────────────────────────────────────────────────────┐
│                         LIF Modular Monolith                          │
│                                                                        │
│ ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌─────────────┐ │
│ │  Curriculum  │  │   Learner    │  │  Assessment  │  │  Scheduling │ │
│ │  (content,   │  │  (profile,   │  │  (skill      │  │  (FSRS,     │ │
│ │  knowledge   │  │  mastery,    │  │  estimates,  │  │  review     │ │
│ │  graph)      │  │  vocab state)│  │  exams)      │  │  queue)     │ │
│ └──────┬───────┘  └──────┬───────┘  └──────┬───────┘  └──────┬──────┘ │
│        │                 │                 │                 │        │
│ ┌──────┴─────────────────┴─────────────────┴─────────────────┴──────┐ │
│ │                    Learning (planner, lesson, session)            │ │
│ └──────┬───────────────────────────────────────────────────┬───────┘ │
│        │                                                   │         │
│ ┌──────┴───────┐ ┌──────────────┐ ┌──────────────┐ ┌───────┴───────┐ │
│ │ Conversation │ │  Production  │ │ Gamification │ │  AI Gateway   │ │
│ │ (roleplay)   │ │ (writing/    │ │ (XP, badges, │ │ (providers,   │ │
│ │              │ │  speaking)   │ │  streaks)    │ │  prompts) ACL │ │
│ └──────────────┘ └──────────────┘ └──────────────┘ └───────────────┘ │
└────────────────────────────────────────────────────────────────────────┘
```

| Context | Responsibility | Key invariants |
|---|---|---|
| **Curriculum** | Static teaching content + knowledge graph | DAG acyclicity; CEFR consistency; versioning |
| **Learner** | Per-student state: profile, concept mastery, word state, mistakes | Mastery ∈ [0,1]; state transitions legal |
| **Assessment** | Skill ability estimates, placement, exams | Estimate changes need min. observations |
| **Scheduling** | FSRS memory states, review queue | Next-review monotonicity; budget respected |
| **Learning** | Session planning, lesson orchestration, attempts | Planner determinism; lesson phase order |
| **Production** | Writing/speaking evaluation results | Every error maps to a concept or `unmapped` |
| **Conversation** | Roleplay sessions, turns, scenario progress | AI output level-constraint enforced |
| **Gamification** | XP, streaks, badges, goals | XP rules deterministic |
| **AI Gateway (ACL)** | Anti-corruption layer to LLM/speech providers | No domain type leaks provider details |

**Design decision:** bounded contexts are *modules within one deployable* (modular monolith, ADR-002). Contexts communicate via in-process domain events + explicit application services, never by touching each other's tables.

## 2. Ubiquitous Language (excerpt)

| Term | Meaning |
|---|---|
| **Concept** | Atomic teachable unit (grammar point, function, skill objective) — node in the knowledge graph |
| **Frontier** | Set of concepts whose prerequisites are mastered but which aren't mastered themselves |
| **Mastery** | P(learner knows concept) estimated by the mastery model |
| **Observation** | One scoreable interaction outcome feeding assessment |
| **Session** | One planned study unit (10–45 min) composed of activities |
| **Activity** | Review batch, lesson step, conversation, reading, etc. inside a session |
| **Item** | A reviewable memory unit (word sense, grammar micro-skill) with FSRS state |
| **Context Pack** | The bundle of constraints the Prompt Engine gives the LLM |

## 3. Aggregates & Entities

### 3.1 Curriculum context

```
CurriculumVersion (AR)
 ├─ Unit (entity)                      # thematic unit inside a CEFR level, ordered
 │   └─ LessonTemplate (entity)        # planned teach flow for 1–3 concepts
 ├─ Concept (AR)                       # ~500 nodes
 │   ├─ ConceptExample (VO list)
 │   ├─ CommonMistake (VO list)
 │   ├─ MasteryThreshold (VO, default .80)
 │   ├─ EstimatedMinutes (VO)
 │   └─ ConceptDependency (VO: prereqId, type: Requires|Supports)
 ├─ VocabEntry (AR)                    # lexicon: lemma, gender, plural, IPA, freq, CEFR…
 │   └─ WordSense (entity: meaning, example, topic)
 ├─ Scenario (AR)                      # roleplay script: stages, goals, req. vocab/concepts
 ├─ ReadingText / ListeningItem / WritingPrompt (ARs, curated content)
 └─ AssessmentBlueprint (AR)           # checkpoint & level-up exam definitions
```

*Invariants:* dependency graph is a DAG; a concept's `RequiredVocabulary` must be at/below its CEFR; `MasteryThreshold ∈ [0.5, 0.95]`.

### 3.2 Learner context

```
LearnerProfile (AR)
 ├─ Identity, Goals (target CEFR, deadline, minutesPerDay)
 ├─ Preferences (practice types, voice, UI options)
 ├─ CefrState (VO: overall + per-skill level, provisional flags)
 └─ Pace (VO: learningSpeed, retentionRate — recomputed)

ConceptMastery (AR)  [learnerId + conceptId]
 ├─ mastery: double, confidence: double
 ├─ attempts, correct, streakWrong
 └─ lastPracticed, provisional (from placement)

WordState (AR)  [learnerId + vocabEntryId]
 ├─ status: New|Learning|Known|Mastered|Forgotten|Relearning
 ├─ FsrsState (VO: stability, difficulty, lastReview, due)
 └─ leechCount, source (lesson|reading|conversation|manual)

MistakeRecord (entity, append-only)
 └─ category, conceptId?, skill, sourceRef, timestamp
```

*Invariants:* `WordState` transitions follow the FSRS state machine; `ConceptMastery.mastery` only changes through recorded observations.

### 3.3 Assessment context

```
SkillEstimate (AR) [learnerId + skill]        # 7 skills + overall derived
 ├─ theta (ability, Elo/IRT-lite scale)
 ├─ standardError, observationCount
 └─ history (snapshots)

PlacementSession (AR)   # adaptive test lifecycle
ExamSession (AR)        # checkpoint / level-up exam with sections & results
Observation (entity, append-only stream)
 └─ skill, conceptId?, itemDifficulty, outcome(0..1), weight, source
```

*Invariant:* CEFR level derived, never set directly (except placement seed marked provisional).

### 3.4 Scheduling context

```
ReviewItem (AR) [learnerId + itemRef(word|conceptDrill)]
 ├─ FsrsState, priorityScore (computed)
 └─ postponedCount
ReviewLog (entity, append-only): rating, elapsed, scheduledInterval
```

### 3.5 Learning context

```
StudySession (AR)
 ├─ plan: SessionPlan (VO: ordered ActivityPlan list, seeds, budgets)
 ├─ state: Planned|Active|Completed|Abandoned
 └─ activities: Activity (entity)
      ├─ ReviewActivity | LessonActivity | ReadingActivity
      │   | ListeningActivity | ConversationActivity | WritingActivity
      └─ ExerciseInstance (entity)
           ├─ spec (VO: type, stem, options, answerKey, conceptIds, difficulty)
           ├─ source: Curated|AiGenerated(templateVersion, contextHash)
           └─ Attempt (entity, append-only): response, outcome, hintsUsed, ms
```

*Invariants:* activity order fixed at planning; attempts immutable; an `ExerciseInstance` from AI must reference the validation report that admitted it.

### 3.6 Production & Conversation contexts

```
WritingSubmission (AR): promptRef, text, EvaluationReport (VO)
 └─ ErrorAnnotation (VO): span, category, conceptId?, correction, explanation
SpeakingAttempt (AR): audioRef, transcript, pronunciation scores, EvaluationReport
ConversationSession (AR): scenarioRef, stage, turns[], constraints snapshot, FeedbackReport
 └─ Turn (entity): speaker, text, audioRef?, violations[]
```

### 3.7 Gamification context

```
GamificationState (AR): xp, level, streak, freezeTokens, dailyGoal
BadgeAward (entity), GoalPeriod (entity: target, progress, adaptive basis)
```

## 4. Value Objects (cross-cutting)

`CefrLevel` (A1..C1 + sublevels A1.1/A1.2…), `Skill` (enum of 7), `Difficulty` (1–10 with band arithmetic), `MasteryThreshold`, `TokenBudget`, `LanguageCode`, `ContextPack` (AI constraint bundle), `RubricScore` (0–5 per criterion).

## 5. Domain Events (selection)

| Event | Producer | Consumers |
|---|---|---|
| `AttemptRecorded` | Learning | Assessment (observation), Learner (mastery), Gamification (XP), Scheduling (FSRS update if review) |
| `ConceptMasteryChanged` | Learner | Learning (frontier recompute), Analytics |
| `WordLapsed` / `WordMastered` | Scheduling | Learner, Analytics, Gamification |
| `SkillEstimateUpdated` | Assessment | Learner (CEFR state), Analytics |
| `LevelUpEligible` | Assessment | Learning (offer exam) |
| `ExamPassed` | Assessment | Learner (promote), Gamification (badge) |
| `WritingEvaluated` | Production | Learner (mistakes→mastery), Assessment |
| `ConversationCompleted` | Conversation | Production (feedback), Assessment |
| `SessionCompleted` | Learning | Gamification (streak), Analytics |
| `RemediationTriggered` | Learning | Learning planner (insert re-teach) |

Events are dispatched in-process (MediatR-style) within the same transaction where consistency requires it (mastery updates), and via the outbox for async consumers (analytics rollups).

## 6. Domain Services

| Service | Context | Pure? | Responsibility |
|---|---|---|---|
| `MasteryUpdater` | Learner | ✔ pure | BKT-style Bayesian update from an observation |
| `FrontierCalculator` | Curriculum+Learner | ✔ pure | Eligible concepts from graph + mastery |
| `SessionPlanner` | Learning | ✔ pure (seeded) | Compose SessionPlan from inputs |
| `DifficultyGovernor` | Learning | ✔ pure | Adjust difficulty band from rolling success |
| `FsrsScheduler` | Scheduling | ✔ pure | Next interval from rating + memory state |
| `ReviewPrioritizer` | Scheduling | ✔ pure | Priority score formula |
| `AbilityUpdater` | Assessment | ✔ pure | Elo/IRT-lite theta update |
| `CefrDeriver` | Assessment | ✔ pure | Map skill thetas → CEFR + confidence |
| `ConstraintChecker` | AI Gateway edge | ✔ pure | Validate AI output against ContextPack |

**Design decision — pure domain services:** every pedagogical algorithm is a pure function over immutable inputs. This is what makes the "application is the teacher" principle *testable*: we can property-test and simulate the entire pedagogy without I/O or AI (NFR-TST-2).

## 7. What the domain explicitly does NOT contain

- No `IAiProvider` or prompt types — the AI Gateway is infrastructure behind application-level ports (`IExerciseGenerator`, `IWritingEvaluator`, `IConversationPartner`, `IExplainer`).
- No EF Core attributes/base classes — persistence-ignorant POCOs.
- No decision points where an LLM output selects curriculum, difficulty, or scheduling (enforced by design review + arch tests: Domain and Application planner namespaces may not reference AI ports' *generation* interfaces).

## 8. Alternatives considered

| Choice | Alternative | Why rejected |
|---|---|---|
| Modular monolith with 9 contexts | Fine-grained microservices | Offline single-machine target; transactional mastery updates simpler; contexts keep future split possible |
| Event-sourced *attempts only* | Full event sourcing everywhere | Full ES adds heavy infra for little value outside learning telemetry; attempts stream gives recomputability where it matters |
| Per-learner aggregates (`ConceptMastery` as AR) | One giant `Student` aggregate | Giant aggregate = contention + memory bloat with 5 000+ items; small ARs align with access patterns |
