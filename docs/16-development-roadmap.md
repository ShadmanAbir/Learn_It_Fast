# 16 — Development Roadmap

**Status:** Draft for review · Phases gate on demonstrable outcomes, not dates. Team assumption: 2–4 engineers + 1 linguist (content) part-time. Indicative durations for that team size.

---

## Phase 0 — Foundations (2–3 weeks)

**Goal:** repo, CI, architecture skeleton, decisions locked.

- Solution scaffold per `14-project-structure.md`; architecture tests green from day 1.
- CI: build, unit tests, arch tests, seed validation, frontend lint/test.
- Docker compose (app+Postgres+Redis); EF migrations pipeline; Serilog + OTel wiring.
- ADR review gate: **this documentation set approved** → ADRs flipped to *Accepted*.
- Frontend scaffold: Vite, design tokens, router, OpenAPI client generation.

**Exit:** `docker compose up` serves a hello-world SPA + healthy `/health/ready`.

## Phase 1 — Walking Skeleton of the Learning Loop (4–6 weeks) ⭐ critical path

**Goal:** the *complete* adaptive loop works end-to-end for a thin A1.1 slice — proving the architecture before content scale-up.

- Curriculum: seed **A1.1 slice** (~15 concepts, 120 words, 2 units, 1 checkpoint blueprint).
- Knowledge graph + frontier computation + mastery model (BKT) + observation stream.
- Session planner v1 (review/new/production shares, difficulty governor, rationale output).
- Lesson player: teach step + 4 deterministic exercise types (MC, cloze, matching, ordering).
- FSRS scheduler + unified review queue + review UI.
- AI Gateway v1: LM Studio + Ollama + OpenAI providers; PromptEngine; `gen_examples`, `gen_exercise` (cloze/MC), `explain`; validation & fallback pipeline.
- Onboarding: beginner fast path (skip placement); goals.
- Today screen; minimal progress page (mastery list, streak, XP basic).
- **Simulation harness v1** — simulated students run the loop in CI.

**Exit demo:** a beginner completes 14 simulated days: lessons adapt, reviews schedule correctly, weak concepts resurface, mastery visibly updates. All without cloud access.

## Phase 2 — Assessment & Adaptivity Depth (4–5 weeks)

**Goal:** continuous assessment + placement + remediation prove "adaptive," not just "sequenced."

- Skill estimates (Elo/IRT-lite) for grammar/vocab/reading; CEFR derivation + confidence gates.
- Adaptive placement test (grammar/vocab/reading sections) seeding provisional mastery.
- Remediation flow (3-strike → prerequisite re-check → re-teach insertion).
- Checkpoint exams from blueprints; gap reports.
- Difficulty governor tuning via simulation harness; interleaving policy.
- Remaining providers (Claude, Gemini, Azure, OpenRouter); routing table + fallback chains + capability probe wizard.
- Analytics rollup job + dashboard v1 (vocab growth, mastery heatmap, XP, review completion, CEFR estimate).

**Exit demo:** a "returning learner" places into A2, gets a coherent plan, fails Dativ drills → system diagnoses and re-teaches the prerequisite; dashboards reflect it.

## Phase 3 — Production Skills: Writing, Reading, Listening (5–6 weeks)

**Goal:** free production evaluated; input skills measured.

- Writing engine: prompts, `eval_writing` (full + reduced-claim), error taxonomy → mastery mapping, rewrite diff UI, history.
- Reading engine: curated + constrained generation, lookups → vocab, WPM & comprehension tracking.
- Listening engine: TTS adapter (local Piper-class + cloud), comprehension/dictation/shadowing, playback speeds.
- Grammar micro-drills join the review queue (unified priorities).
- Content wave: **complete A1** (all units, scenarios, blueprints, fallback exercise bank).
- Gamification v1 complete: badges, adaptive goals, freeze tokens.

**Exit demo:** learner writes an A1 email, receives annotated categorized feedback that changes next session's plan; dictation and reading sessions feed analytics.

## Phase 4 — Speaking & Conversation (4–6 weeks)

**Goal:** the roleplay differentiator, offline-capable.

- STT adapter (local whisper.cpp + cloud); audio capture UX; word-level pronunciation scores.
- Conversation engine: scenario scripts, stage machine, constraint-checked streamed turns (SSE), simplification trigger, feedback reports.
- 6 launch scenarios ×A1/A2 (restaurant, shopping, doctor, hotel, introductions, directions).
- Speaking observations → skill estimates; conversation errors → mistake records.
- Content wave: **A2 complete**.

**Exit demo:** 5-minute "Beim Arzt" roleplay with a local 8B model: partner stays level-bound and in role; feedback report maps errors to concepts; works with WiFi off.

## Phase 5 — Level-Up, Polish & Hardening (4–5 weeks)

**Goal:** promotion machinery + commercial-grade finish.

- Level-up exams (multi-skill), exit-requirement gates, promotion ceremony UX.
- Data export/import; GDPR endpoints; key encryption audit.
- Accessibility pass to WCAG 2.2 AA; i18n (EN/DE UI); reduced-motion & dyslexia options.
- Performance pass against NFR-PERF targets; load test server mode; partitioning switches.
- Curriculum map (graph view); weakness report + "practice now" routing.
- Content wave: **B1 complete**; B2/C1 authoring pipeline running (waves continue post-launch).
- Beta program instrumentation (opt-in telemetry, feedback capture).

**Exit:** public beta. B2/C1 content ships as updates via seed packages (no redeploy).

## Post-v1 tracks (parallel, prioritized by beta data)

1. FSRS per-learner parameter optimization job; IRT item calibration from accumulated attempts.
2. Mock exam mode (Goethe/telc structures); phoneme-level pronunciation via provider port.
3. React Native app (reusing feature `model/` layers + API).
4. Authoring UI hardening for external linguists; marketplace-style content packages.
5. pgvector semantic retrieval for smarter example/context selection.

## Risk register (top 5)

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Small-model output quality below validity target | High | High | Validation+fallback architecture (ADR-008); capability probe steers routing; curated banks absorb worst case |
| Pedagogy math (BKT/FSRS/Elo) mis-tuned → frustration or boredom | Medium | High | Simulation harness in CI from Phase 1; difficulty governor bands; beta telemetry |
| Content authoring becomes the schedule bottleneck | High | Medium | Thin-slice strategy; AI-assisted drafting pipeline with human review; fallback banks prioritized |
| Local STT/TTS setup friction kills onboarding | Medium | Medium | Optional speech (text paths everywhere); guided wizard; prepackaged compose profile with whisper/piper |
| Scope creep from "one more engine" | High | Medium | Phase exit demos are the only gate; MoSCoW enforced by PM |
