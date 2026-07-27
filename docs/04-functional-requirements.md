# 04 — Functional Requirements

**Status:** Draft for review · Requirements are numbered `FR-<area>-<n>`, testable, and traceable to features (`02`) and stories (`03`).

---

## FR-CUR — Curriculum & Knowledge Graph

- **FR-CUR-1** The system SHALL store the complete A1–C1 curriculum in relational tables: grammar concepts, vocabulary topics, conversation scenarios, reading/listening objectives, writing prompts, culture notes, assessments.
- **FR-CUR-2** Each concept SHALL carry: id, CEFR level, difficulty (1–10), dependencies, learning objective, curated examples, common mistakes, estimated learning time, mastery threshold, required vocabulary, required grammar.
- **FR-CUR-3** The knowledge graph SHALL be a directed acyclic graph; the system SHALL reject any edit that introduces a cycle.
- **FR-CUR-4** The system SHALL compute a learner's *eligible frontier*: concepts whose prerequisites all meet their mastery thresholds.
- **FR-CUR-5** Curriculum content SHALL be versioned; learner mastery state SHALL survive curriculum upgrades via migration mapping.
- **FR-CUR-6** The LLM SHALL NOT create, modify, or reorder curriculum entities under any circumstances.

## FR-STU — Student Model

- **FR-STU-1** The system SHALL maintain a persistent learner profile: current CEFR (overall + per skill), goals, preferences, motivation type, study history.
- **FR-STU-2** The system SHALL track per-concept state: mastery probability [0..1], confidence, attempts, correct/incorrect counts, last review, next review, review priority.
- **FR-STU-3** The system SHALL track per-word state: status (new/learning/known/mastered/forgotten), FSRS memory state (stability, difficulty), review history.
- **FR-STU-4** The system SHALL maintain a mistake history with error category, source skill, mapped concept, and timestamp.
- **FR-STU-5** The system SHALL estimate learning speed and retention rate per learner from observed data (rolling windows).
- **FR-STU-6** All student-model updates SHALL be event-sourced at the attempt level (append-only attempt log) so estimates can be recomputed.

## FR-ASM — Assessment Engine

- **FR-ASM-1** Every scoreable interaction SHALL emit an observation `(skill, concept?, difficulty, outcome, confidence)` into the assessment stream.
- **FR-ASM-2** Skill estimates (reading, writing, listening, speaking, grammar, vocabulary, pronunciation) SHALL be updated incrementally (Elo-style/IRT-lite update; see ADR-006) with a confidence value.
- **FR-ASM-3** Overall CEFR SHALL be derived from per-skill estimates using level descriptors; a level change SHALL require ≥ N (configurable, default 30) observations since the last change and confidence ≥ 0.7.
- **FR-ASM-4** The placement test SHALL be adaptive: item difficulty follows current ability estimate; termination when the standard error < threshold or item cap reached.
- **FR-ASM-5** Level-up exams SHALL verify all exit requirements of the current CEFR level (mastery coverage %, per-skill minimums).
- **FR-ASM-6** AI-scored assessments (writing/speaking) SHALL store both the AI rubric output and deterministic checks; disagreements above a threshold flag the item for down-weighting.

## FR-PLN — Lesson Planner & Learning Engine

- **FR-PLN-1** The planner SHALL compose sessions deterministically from: due reviews, weak concepts, eligible new concepts, difficulty targets, and time budget. Identical inputs SHALL produce identical plans (seeded randomness only).
- **FR-PLN-2** Session composition defaults: 40 % review, 40 % new/weak material, 20 % free production — adjustable by planner policy, never by the LLM.
- **FR-PLN-3** The difficulty governor SHALL target a 70–85 % rolling success rate; outside the band it SHALL adjust exercise difficulty/scaffolding one step per session.
- **FR-PLN-4** Three consecutive failures on one concept SHALL trigger remediation: prerequisite re-check, scaffolded re-teach, difficulty decrease.
- **FR-PLN-5** The planner SHALL interleave concept families (e.g., not 10 Dativ items in a row once past guided practice).
- **FR-PLN-6** The planner SHALL decide: what to teach, what to skip, review vs. new ratio, difficulty, lesson length, practice types, conversation topics, homework. The LLM SHALL receive these as *inputs*.

## FR-LSN — Lesson Player & Exercises

- **FR-LSN-1** Lessons SHALL follow: teach → guided practice → free practice → wrap-up (skippable phases only by planner policy).
- **FR-LSN-2** Supported exercise types: multiple-choice, cloze, sentence ordering, matching, translation (DE↔EN), transformation, error spotting, dictation, free writing, speaking prompt.
- **FR-LSN-3** Deterministic exercise types (MC, cloze, ordering, matching) SHALL be validated locally without an AI call.
- **FR-LSN-4** AI-generated exercises SHALL be schema-validated and constraint-checked (vocabulary whitelist, grammar whitelist) before display; invalid generations are retried then replaced by curated fallbacks.
- **FR-LSN-5** Every attempt SHALL be persisted with response, latency, hint usage, and outcome.
- **FR-LSN-6** A hint ladder SHALL provide up to 3 escalating hints; hint use reduces the mastery credit of the attempt.

## FR-VOC — Vocabulary Engine

- **FR-VOC-1** The lexicon SHALL store: lemma, meaning(s), part of speech, gender, plural, frequency rank, CEFR, topic, IPA, example sentence, audio reference, difficulty.
- **FR-VOC-2** Reviews SHALL be scheduled by FSRS; rating scale Again/Hard/Good/Easy; desired retention configurable (default 0.90).
- **FR-VOC-3** Words failed ≥ 5 times SHALL be flagged leeches and routed to alternative practice.
- **FR-VOC-4** Learners SHALL add arbitrary encountered words; system enriches them (gender, plural, example) via AI + dictionary validation.
- **FR-VOC-5** Word status transitions: new → learning → known → mastered; any lapse → forgotten → relearning (FSRS state machine).

## FR-REV — Review Engine

- **FR-REV-1** A unified review queue SHALL cover vocabulary and grammar micro-drills.
- **FR-REV-2** Queue priority = f(overdue-ness, past mistakes, item difficulty, retention estimate, concept importance); formula documented and unit-tested.
- **FR-REV-3** Daily review load SHALL respect the learner's time budget; overflow postpones lowest-priority items and logs the debt.
- **FR-REV-4** Review outcomes SHALL update both FSRS state and concept mastery.

## FR-RDG / FR-LIS — Reading & Listening

- **FR-RDG-1** Generated passages SHALL use ≥ 95 % known+current vocabulary; the generator receives the allowed list; a post-check enforces it.
- **FR-RDG-2** Word-lookup taps SHALL be recorded as vocabulary signals.
- **FR-RDG-3** Reading speed (WPM) and comprehension score SHALL be stored per session.
- **FR-LIS-1** Listening items SHALL provide TTS audio + hidden transcript + questions; dictation and shadowing modes SHALL be supported.
- **FR-LIS-2** Playback speed 0.5×–1.25×; replays counted as difficulty signal.

## FR-WRT — Writing

- **FR-WRT-1** Writing submissions SHALL be evaluated for: grammar, vocabulary use, naturalness, sentence structure, cases, articles, word order — each with rubric score 0–5.
- **FR-WRT-2** Errors SHALL be returned as structured annotations `{span, category, conceptId, explanation, correction}`.
- **FR-WRT-3** A corrected/natural rewrite SHALL be provided at the learner's level.
- **FR-WRT-4** Error annotations SHALL update concept mastery and mistake history.

## FR-SPK — Speaking & Conversation

- **FR-SPK-1** Speech pipeline: audio → STT → grammar analysis → pronunciation analysis → conversation turn → feedback.
- **FR-SPK-2** Roleplay scenarios SHALL be driven by scenario scripts (goals, stages, required vocabulary) stored in the curriculum; the AI receives the current stage + constraints per turn.
- **FR-SPK-3** The conversation engine SHALL enforce level constraints on AI output (post-check vocabulary/complexity; regenerate on violation).
- **FR-SPK-4** Post-conversation feedback SHALL include grammar issues, vocabulary suggestions, task-completion assessment.
- **FR-SPK-5** Pronunciation scoring SHALL work at word level from STT confidence in v1; phoneme-level when provider supports it.

## FR-AI — AI Orchestration

- **FR-AI-1** All model access SHALL go through `IAiProvider` (chat-completion abstraction); business logic SHALL depend only on task-level interfaces (e.g., `IWritingEvaluator`).
- **FR-AI-2** Providers: LM Studio, Ollama, OpenAI, Anthropic Claude, Google Gemini, Azure OpenAI, OpenRouter.
- **FR-AI-3** A routing table SHALL map AI task types → provider profile with an ordered fallback chain.
- **FR-AI-4** Every AI request SHALL be built by the Prompt Engine from a versioned template + a context pack (level, allowed grammar/vocab, objective, constraints). No raw chat history concatenation.
- **FR-AI-5** All AI outputs SHALL be JSON-schema validated; invalid output → bounded retries with repair prompt → fallback content; every failure logged.
- **FR-AI-6** Token budgets per task type SHALL be enforced (small-model targets: ≤ 1.5 k prompt tokens for generation tasks, ≤ 2.5 k for evaluation).
- **FR-AI-7** AI calls SHALL be cacheable by (template version, context hash) for reusable generations (examples, passages).

## FR-GAM — Gamification

- **FR-GAM-1** XP SHALL be awarded by deterministic rules (per exercise type, difficulty, first-time bonus).
- **FR-GAM-2** Streaks SHALL count any completed session; freeze tokens (earned, max 2 banked) auto-apply.
- **FR-GAM-3** Badges SHALL be evaluated by a rule engine over domain events.
- **FR-GAM-4** Daily/weekly goals SHALL adapt to trailing 14-day activity.

## FR-ANA — Analytics

- **FR-ANA-1** Daily rollups (background job) SHALL aggregate: XP, minutes, reviews done/due, new words, mastery deltas, skill estimates.
- **FR-ANA-2** Dashboards per `F12.1` SHALL read from rollup tables (not raw event scans).
- **FR-ANA-3** A weakness report SHALL rank concepts by (low mastery × high importance × recent errors) with recommended actions.

## FR-SET — Settings, Data, Admin

- **FR-SET-1** Full profile export/import as versioned JSON archive.
- **FR-SET-2** Provider credentials SHALL be stored encrypted at rest (DPAPI/OS keychain in local mode).
- **FR-SET-3** Content authoring SHALL validate: DAG integrity, CEFR consistency of required vocabulary, threshold ranges.
- **FR-SET-4** Feature flags SHALL gate: new exercise types, model routing changes, experimental scoring.

---

# 05 — Non-Functional Requirements (summary — full doc in `05-nfr.md`)

See `05-nfr.md`.
