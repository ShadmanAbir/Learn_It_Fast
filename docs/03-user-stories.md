# 03 — User Stories

**Status:** Draft for review · Format: `As a <role>, I want <capability>, so that <benefit>` + acceptance criteria (AC).
Stories map to features in `02-feature-list.md`.

---

## Epic E1 — Onboarding & Placement

**US-1.1** As a new learner, I want to state my goal (target level, deadline, daily minutes), so that the planner paces my curriculum realistically. *(F1.2)*
- AC: Goal stored; planner surfaces "on track / behind / ahead" from day 1; goal editable anytime.

**US-1.2** As a returning German learner, I want an adaptive placement test, so that I skip what I already know.
- AC: Test adapts item difficulty to my answers; finishes in ≤ 20 min; produces per-skill CEFR estimate with confidence; seeds mastery priors for concepts below my estimated level (marked *provisional*, verified during study).

**US-1.3** As a total beginner, I want to skip the placement test, so that I can start learning immediately.
- AC: One click; all mastery = 0; first lesson starts within 30 s.

**US-1.4** As a privacy-conscious user, I want a setup wizard that finds my local LM Studio/Ollama, so that I never send data to the cloud.
- AC: Auto-detects local endpoints; runs a capability probe (JSON compliance, German quality smoke test); shows red/yellow/green fitness verdict per task type.

## Epic E2 — Daily Learning Loop

**US-2.1** As a learner, I want the app to decide what I study today, so that I never waste time choosing.
- AC: "Start today's session" composes reviews + new material deterministically; session plan visible before start; no LLM involved in the selection.

**US-2.2** As a learner, I want each new concept taught before I'm tested on it, so that I'm never guessing blindly.
- AC: Lesson flow = teach → guided practice → free practice → wrap-up; teach step uses curated content, AI generates *additional* examples only within allowed vocabulary/grammar.

**US-2.3** As a learner, I want immediate, specific feedback on every answer, so that I learn from mistakes instantly.
- AC: Feedback ≤ 2 s for deterministic checks, ≤ 10 s for AI evaluation; every wrong answer shows *why* and links the violated concept.

**US-2.4** As a struggling learner, I want the app to notice repeated failures and re-teach prerequisites, so that I don't stay stuck.
- AC: 3 consecutive failed attempts on a concept triggers prerequisite mastery re-check; remediation lesson auto-inserted; difficulty governor lowers exercise difficulty one band.

**US-2.5** As a busy learner, I want a 10-minute session option, so that a short day still counts.
- AC: Planner produces a valid session for 10/20/30/45 min budgets; streak preserved by any completed session.

## Epic E3 — Vocabulary

**US-3.1** As a learner, I want daily vocabulary reviews scheduled scientifically, so that I stop forgetting words.
- AC: FSRS scheduling; due queue capped by daily budget; overflow postponed by priority; retention target configurable (default 90 %).

**US-3.2** As a learner, I want to save any word I meet in a text or conversation, so that my vocabulary grows from real usage.
- AC: One-click save from reading/conversation views; word enters review queue with lesson-context example sentence.

**US-3.3** As a learner, I want stubborn words detected and practiced differently, so that leeches don't eat my review time.
- AC: Word failed ≥ 5 times flagged as leech; switched to production drills + mnemonic prompt; surfaced in weakness report.

## Epic E4 — Reading & Listening

**US-4.1** As a learner, I want reading passages matched to my level and interests, so that reading is challenging but doable.
- AC: Passage vocabulary ≥ 95 % from my known + current-lesson words; unknown-word taps recorded; comprehension quiz after every passage.

**US-4.2** As a learner, I want dictation and shadowing modes, so that I train listening precision and speaking rhythm.
- AC: Dictation diffs my transcript vs. reference with per-token marking; shadowing plays segment-by-segment with speed control 0.5×–1.25×.

## Epic E5 — Writing

**US-5.1** As a learner, I want my free writing corrected with categorized errors and a natural rewrite, so that I understand my mistakes.
- AC: Errors annotated inline with category (case, article, word order, …); each error links to its grammar concept; rubric scores (grammar, vocabulary, structure, task) stored; model rewrite shown side-by-side.

**US-5.2** As a learner, I want my writing mistakes to influence what I review, so that practice targets my real weaknesses.
- AC: Each categorized error updates mastery of the mapped concept; concepts with error spikes rise in review priority.

## Epic E6 — Speaking & Conversation

**US-6.1** As a learner, I want to roleplay real scenarios (restaurant, doctor, interview…), so that I build practical speaking confidence.
- AC: AI partner stays in role, uses only level-appropriate language, pursues the scenario's task goals; conversation ends with a feedback report (grammar, vocab, task completion).

**US-6.2** As a learner, I want to speak my answers and get pronunciation feedback, so that I'm understood in real life.
- AC: STT transcribes locally if configured; mispronounced/low-confidence words highlighted; per-word score history tracked.

**US-6.3** As an anxious speaker, I want the AI partner to slow down and simplify when I struggle, so that I'm not overwhelmed.
- AC: Two consecutive non-understandings → partner simplifies (app instructs the model via constraint update, not model's own decision).

## Epic E7 — Assessment & Progression

**US-7.1** As a learner, I want to see my estimated CEFR per skill with confidence, so that I know where I truly stand.
- AC: Dashboard shows 7 skill estimates + overall; confidence shown; estimate changes only after sufficient observations (no single-exam jumps).

**US-7.2** As a learner, I want a level-up exam when the system thinks I'm ready, so that promotion feels earned and valid.
- AC: Exam offered only when exit requirements met (mastery coverage + skill estimates); covers all skills; pass → level promoted, fail → gap report + targeted plan.

**US-7.3** As a learner, I want checkpoint quizzes at unit boundaries, so that weak spots are caught early.
- AC: Failing a checkpoint blocks dependent units and schedules remediation.

## Epic E8 — Analytics & Motivation

**US-8.1** As a learner, I want dashboards of my growth (vocab size, mastery heatmap, XP, streak, skill trends), so that I stay motivated by visible progress.
- AC: All F12.1 charts render from real data; empty states designed; date-range filters.

**US-8.2** As a learner, I want streaks, XP, badges, and adaptive daily goals, so that studying becomes a habit.
- AC: XP awarded by planner-defined rules (not AI); streak freeze tokens earnable; goals auto-adjust to my recent capacity.

**US-8.3** As a learner falling behind my goal, I want the app to propose a realistic recovery plan, so that I don't quit.
- AC: "Behind schedule" triggers plan options: extend deadline, increase daily minutes, or narrow scope; chosen plan updates the planner.

## Epic E9 — Platform & AI Management

**US-9.1** As a power user, I want to configure multiple AI providers and route tasks (e.g., conversation → local 8B, writing evaluation → cloud), so that I balance cost, privacy and quality.
- AC: Per-task-type routing table; fallback chain on provider failure; secrets stored encrypted; no business behavior change when routing changes.

**US-9.2** As a user, I want full export/import of my learning profile, so that my data is mine.
- AC: Single JSON archive (profile, mastery, review states, history); import restores byte-equivalent learning state.

**US-9.3** As a content author (linguist), I want to edit curriculum with dependency validation, so that content stays pedagogically sound.
- AC: Authoring UI rejects cycles, missing prerequisites, vocabulary above declared CEFR; changes are versioned; learner state migrates.

**US-9.4** As an operator, I want health checks, structured logs, and feature flags, so that I can run this like a commercial product.
- AC: `/health` covers DB, cache, AI provider reachability; all AI calls logged with latency/tokens/task-type; flags toggle risky features at runtime.

---

## Story-point sizing note

Sizing happens at sprint planning. Epics E2, E7 (planner + assessment) are the *architecturally critical path* — they get walking-skeleton treatment in Phase 1 of the roadmap (`16-development-roadmap.md`).
