# LIF — Learn It Fast · Development Roadmap & Status

**Project:** Adaptive AI German Tutor — *the application is the teacher; the AI model is only one tool used by the teacher.*
**Stack:** ASP.NET Core 9 (Clean Architecture) · React 19 + Vite · PostgreSQL 16 · local-first AI (LM Studio / Ollama) with cloud fallback.
**Full design set:** [`docs/`](docs/) (01–16). Phase definitions: [`docs/16-development-roadmap.md`](docs/16-development-roadmap.md).

Legend: ✅ built & verified · 🔨 in progress · ⬜ not started

---

## Status at a glance

| Phase | Scope | Status |
|---|---|---|
| 0 | Foundations (scaffold, CI, ADRs) | ✅ Complete |
| 1 | Walking skeleton of the learning loop | ✅ Complete — live-verified end-to-end |
| 2 | Assessment & adaptivity depth | 🔨 In progress |
| 3 | Production skills: writing, reading, listening | ⬜ Not started |
| 4 | Speaking & conversation | ⬜ Not started |
| 5 | Level-up, polish & hardening | ⬜ Not started |

---

## ✅ Phase 0 — Foundations (COMPLETE)

- Full design documentation set (`docs/01`–`16`): requirements, domain model, DB schema, knowledge graph, CEFR curriculum, AI gateway, API design, frontend architecture, project structure, ADRs, roadmap.
- Solution scaffold per `docs/14`: `SharedKernel ← Domain ← Application ← {Infrastructure, Persistence} ← Api`; warnings-as-errors + analyzers; architecture tests green.
- CI (GitHub Actions): build, unit tests, architecture tests, seed validation, frontend lint/test.
- Docker compose (app + Postgres) and native local dev path; EF Core migrations pipeline; Serilog + OpenTelemetry wiring; health endpoints (`/health/live`, `/health/ready`).
- Frontend scaffold: Vite + React 19, design tokens, router, typed API client.

## ✅ Phase 1 — Walking Skeleton of the Learning Loop (COMPLETE)

The complete adaptive loop works end-to-end for a thin A1.1 slice — **live-verified**: onboard → session plan → exercises → review queue, running against native PostgreSQL 16 with seeds imported.

- **Curriculum:** seed package format + importer (idempotent, versioned); A1.1 slice seeded — 15 concepts, 127 lexemes, 2 units, 41 exercises, 1 checkpoint blueprint.
- **Knowledge graph:** prerequisite edges, cycle validation, frontier computation (what's learnable next).
- **Learner model:** BKT mastery updates (`MasteryUpdater`), per-concept mastery aggregates, append-only mistake records, 3-strike remediation flag.
- **Session planner v1:** review/new/production shares, difficulty governor (1-band steps), rationale output.
- **Lesson player:** teach step + 4 deterministic exercise types (multiple choice, cloze, matching, ordering) with deterministic evaluation.
- **Scheduling:** FSRS scheduler, unified review queue, review UI.
- **AI Gateway v1:** LM Studio + Ollama + OpenAI providers; PromptEngine; `gen_examples`, `gen_exercise`, `explain` tasks; JSON-schema validation + curated-bank fallback pipeline (LM Studio quirk handled: `json_schema` response format).
- **Onboarding:** beginner fast path (skip placement) + goals.
- **UI:** Today screen, lesson/review flows, minimal progress page (mastery list, streak, XP).
- **Simulation harness v1:** simulated students run the loop in CI.
- **Persistence:** full EF Core model + Initial migration (incl. fix for Npgsql identity-on-owned-PK pitfall); 39/39 persistence tests green.

## 🔨 Phase 2 — Assessment & Adaptivity Depth (IN PROGRESS)

Goal: continuous assessment + placement + remediation prove *adaptive*, not just *sequenced*.

| Item | Status |
|---|---|
| **Skill estimates (Elo/IRT-lite) + CEFR derivation** — `Observation` stream, `AbilityUpdater` (logistic Elo update, decaying K, information/SE), `SkillEstimate` aggregate with FR-ASM-3 confidence gates (≥30 obs + confidence ≥ 0.7), `CefrDeriver` | 🔨 Domain layer built; tests, persistence (`assessment` schema), application handlers & `GET /assessment/skills` pending |
| Adaptive placement test (grammar/vocab/reading) seeding provisional mastery; SE-threshold termination | ⬜ |
| Remediation flow: 3-strike → prerequisite re-check → re-teach insertion into plan | ⬜ (domain flag exists from Phase 1; orchestration pending) |
| Checkpoint exams from blueprints + gap reports | ⬜ |
| Difficulty governor tuning + interleaving policy via simulation harness | ⬜ |
| Remaining AI providers (Claude, Gemini, Azure, OpenRouter) + routing table + fallback chains + capability probe wizard | ⬜ |
| Analytics rollup job + dashboard v1 (vocab growth, mastery heatmap, XP, review completion, CEFR estimate) | ⬜ |

**Exit demo:** a returning learner places into A2, fails Dativ drills → system diagnoses the prerequisite and re-teaches it; dashboards reflect it.

## ⬜ Phase 3 — Production Skills: Writing, Reading, Listening

- Writing engine: prompts, `eval_writing` (full + reduced-claim), error taxonomy → mastery mapping, rewrite diff UI, history.
- Reading engine: curated + constrained generation, lookups → vocab, WPM & comprehension tracking.
- Listening engine: TTS adapter (local Piper-class + cloud), comprehension/dictation/shadowing, playback speeds.
- Grammar micro-drills join the unified review queue.
- Content wave: **complete A1**; gamification v1 complete (badges, adaptive goals, freeze tokens).

## ⬜ Phase 4 — Speaking & Conversation

- STT adapter (local whisper.cpp + cloud); audio capture UX; word-level pronunciation scores.
- Conversation engine: scenario scripts, stage machine, constraint-checked streamed turns (SSE), simplification trigger, feedback reports.
- 6 launch scenarios × A1/A2 (restaurant, shopping, doctor, hotel, introductions, directions).
- Speaking observations → skill estimates; conversation errors → mistake records.
- Content wave: **A2 complete**.

## ⬜ Phase 5 — Level-Up, Polish & Hardening

- Level-up exams (multi-skill), exit-requirement gates, promotion ceremony UX.
- Data export/import; GDPR endpoints; key-encryption audit.
- Accessibility to WCAG 2.2 AA; i18n (EN/DE UI); reduced-motion & dyslexia options.
- Performance pass vs NFR targets; load testing; partitioning switches.
- Curriculum map (graph view); weakness report + "practice now" routing.
- Content wave: **B1 complete**; B2/C1 authoring pipeline running.
- Beta program instrumentation → **public beta**.

## ⬜ Post-v1 tracks (prioritized by beta data)

1. FSRS per-learner parameter optimization; IRT item calibration from accumulated attempts.
2. Mock exam mode (Goethe/telc structures); phoneme-level pronunciation scoring.
3. React Native app (reusing feature model layers + API).
4. Authoring UI for external linguists; marketplace-style content packages.
5. pgvector semantic retrieval for smarter example/context selection.

---

## Immediate next steps (current working set)

1. Finish **p2-skills**: unit tests for `AbilityUpdater`/`CefrDeriver`/`SkillEstimate`, EF config + migration for the `assessment` schema, observation emission from attempt/review handlers, `GET /api/v1/assessment/skills`.
2. Adaptive placement sessions (`POST /placement`, next-item, answers) seeding provisional mastery.
3. Remediation orchestration (prereq re-check exercise → re-teach insertion).
4. Checkpoint exams + gap reports.
5. Governor/interleaving tuning through the simulation harness.
6. Provider expansion + routing table + capability probe.
7. Analytics rollup + dashboard v1.
