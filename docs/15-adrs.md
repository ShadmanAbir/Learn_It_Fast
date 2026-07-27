# 15 — Architecture Decision Records

**Status:** Accepted (review gate passed, 2026-07-26) · Format: Context → Decision → Alternatives → Consequences. All 14 ADRs are *Accepted*.

---

## ADR-001: The application owns pedagogy; the LLM is a constrained executor

**Context.** AI tutors typically delegate teaching decisions to the model, causing level drift, no memory of mastery, and unreproducible learning paths. Our core philosophy forbids this.
**Decision.** All curriculum, progression, difficulty, scheduling, and session-composition decisions are made by deterministic application code over database state. LLMs perform only bounded tasks (explain/generate/evaluate/converse) with explicit constraints, and their outputs are validated before use.
**Alternatives.** (a) Agentic LLM tutor with tool access — rejected: untestable pedagogy, small-model infeasibility. (b) Hybrid where LLM proposes and app approves — rejected: approval logic would need the same machinery anyway; adds latency and ambiguity about authority.
**Consequences.** + Reproducible, testable, works offline with 3B models; + pedagogy improvable without touching AI. − We must build real assessment/planning algorithms (BKT, FSRS, IRT-lite) instead of outsourcing to a model; − curated fallback content is mandatory.

## ADR-002: Modular monolith (Clean Architecture), not microservices

**Context.** Deployment target is a single machine (offline-first), but the product must be commercial-grade and future-scalable.
**Decision.** One deployable ASP.NET Core app with 9 bounded contexts as modules; Clean Architecture layers (SharedKernel/Domain/Application/Infrastructure/Persistence/Api); boundaries enforced by architecture tests; transactional outbox so contexts can later be split without event-semantics changes.
**Alternatives.** Microservices — rejected: absurd ops burden for local installs. Layered monolith without context boundaries — rejected: becomes unsplittable and review-hostile.
**Consequences.** + Single-command deploy; simple transactions across mastery updates. − Discipline needed to prevent cross-context coupling (mitigated by tests + code review checklist).

## ADR-003: PostgreSQL for everything, including the knowledge graph

**Context.** Need relational data, some graph traversal (~500 nodes, depth ≤ 12), full-text search, offline installs.
**Decision.** PostgreSQL 16 as the only database. Graph = adjacency list + recursive CTEs; per-learner frontier cached in Redis. FTS via built-in tsvector; pg_trgm for lexicon lookup. Vector DB deferred; if semantic retrieval is needed, pgvector first.
**Alternatives.** Neo4j (ops cost, no offline benefit), SQLite (weaker concurrency/FTS/jsonb; painful server-mode divergence), separate search engine (unjustified).
**Consequences.** + One backup/restore story; jsonb flexibility. − Recursive CTE queries need care (encapsulated in repository + benchmarked).

## ADR-004: FSRS as the spaced-repetition scheduler

**Context.** Reviews are the retention backbone; SM-2 is the incumbent standard.
**Decision.** Implement FSRS (v4+ parameterization) in `Lif.Domain.Scheduling` as a pure function; per-learner parameter optimization as a later background job; ratings Again/Hard/Good/Easy; desired retention configurable.
**Alternatives.** SM-2 — simpler but measurably worse interval efficiency; Leitner boxes — too coarse; custom half-life regression — reinventing FSRS with less evidence.
**Consequences.** + State-of-the-art retention/workload; review logs already shaped for the FSRS optimizer. − More complex math to test (mitigated: property tests + reference-vector tests against the published algorithm).

## ADR-005: Concept mastery via Bayesian Knowledge Tracing (BKT-style)

**Context.** Need per-concept mastery probability with confidence, robust to guessing/slipping, updateable per observation.
**Decision.** BKT-style Bayesian update with per-concept guess/slip priors; hint-usage discounts credit; AI-evaluated outcomes carry reduced observation weight; effective mastery modulated by FSRS retrievability (see `08-knowledge-graph.md` §6).
**Alternatives.** Plain accuracy windows — no confidence semantics; Deep Knowledge Tracing — needs training data we won't have at launch and is unexplainable; Elo-per-concept — conflates ability and knowledge state.
**Consequences.** + Explainable ("87 % likely you've mastered Dativ articles"); cheap to compute. − Priors need tuning; the simulation harness (NFR-TST-2) is the tuning instrument.

## ADR-006: Skill ability estimation via Elo/IRT-lite, CEFR by derivation

**Context.** Seven skill estimates must update continuously from heterogeneous observations, without psychometric calibration studies.
**Decision.** Per-skill scalar ability (theta) updated Elo-style against item difficulty, with uncertainty (K-factor decays with observation count); CEFR derived from theta bands; level changes gated by min-observation and confidence rules (FR-ASM-3). Full IRT calibration is a later upgrade once attempt data accumulates.
**Alternatives.** Full IRT from day 1 — needs calibrated item banks we don't have; CAT engines (proprietary) — cost + closed.
**Consequences.** + Simple, incremental, explainable; placement test reuses the same updater with aggressive K. − Item difficulties initially author-assigned (calibrated later from data — the observation stream makes this retroactive).

## ADR-007: Two-tier AI abstraction (task ports over provider port)

**Context.** Business logic must never depend on providers; small local models need different prompting than cloud models.
**Decision.** Application defines task-level ports (`IWritingEvaluator`, …); Infrastructure implements them via PromptEngine + Router + `IAiProvider` implementations (7 providers). Templates versioned per task × model-size class.
**Alternatives.** Single provider interface used by engines — prompt logic leaks into business code; LangChain-style framework dependency — uncontrolled abstractions, poor .NET fit, obscures token budgets.
**Consequences.** + Swap providers with config; test engines with fake ports. − We own prompt tooling (acceptable: it's core IP).

## ADR-008: All AI output is schema-validated and constraint-checked; curated fallbacks are mandatory

**Context.** Small models fail formats and violate level constraints; learners must never see garbage.
**Decision.** JSON Schema validation + deterministic ConstraintChecker (lemma whitelist scan, forbidden-structure detectors) + bounded repair retries + provider fallback + curated content fallback. Failures are first-class metrics.
**Alternatives.** Trust the model + spot checks — unacceptable quality floor; LLM-as-judge validation — compounds unreliability, doubles cost/latency locally.
**Consequences.** + Hard quality floor independent of model quality. − Curated fallback bank must be authored per exercise type/level (scoped into content waves).

## ADR-009: Speech via ports; v1 pronunciation scoring = word-level from STT confidence

**Context.** Offline speech support varies; phoneme-level scoring locally is immature.
**Decision.** `ISpeechToText` / `ITextToSpeech` ports with local (whisper.cpp, Piper-class) and cloud adapters. Pronunciation v1: word-level scores from STT confidences + known-confusion heuristics; `IPronunciationScorer` port reserved for phoneme-grade providers.
**Alternatives.** Azure Pronunciation Assessment only — breaks offline-first; forced-alignment DIY (MFA) in v1 — high effort, deferred.
**Consequences.** + Honest, working offline feedback now; clean upgrade path. − v1 can't diagnose specific phonemes (UI copy must not overclaim).

## ADR-010: Event-sourced attempt/observation streams; projected state elsewhere

**Context.** Assessment algorithms will improve; recomputability is required (NFR-REL-1); full event sourcing is heavy.
**Decision.** Append-only streams for attempts, observations, review logs, mistakes; current-state tables (mastery, estimates, FSRS states) are projections; a rebuild tool recomputes projections from streams. Other aggregates are plain CRUD. Outbox pattern for async consumers.
**Alternatives.** Full ES + CQRS everywhere — cost without benefit for profiles/settings; no streams — algorithm improvements would never benefit historical data.
**Consequences.** + Retroactive algorithm upgrades, audit trail, simulation food. − Dual-write discipline (projection update + stream append) wrapped in one transaction — covered by integration tests.

## ADR-011: Redis for cache/queues, with in-memory fallback for single-user installs

**Context.** Frontier caches, generation cache index, rate limiting, job coordination need fast shared state; but a solo desktop install shouldn't require Redis.
**Decision.** `ICache`/`IQueue` abstractions; Redis implementation for server mode; in-process implementation for single-user mode (compose profile chooses). Hangfire storage: PostgreSQL (avoids Redis dependency for jobs).
**Consequences.** + `docker compose` minimal profile = app + Postgres only. − Two cache impls to test (small surface, shared contract tests).

## ADR-012: React 19 + Vite + TypeScript; TanStack Query; Zustand for local state; no Next.js

**Context.** SPA over a local API; offline-first; future React Native reuse.
**Decision.** Vite SPA + TanStack Query (server state) + Zustand (session/UI state) + generated OpenAPI client + Radix/Tailwind design system + Recharts for dashboards. SSR unnecessary (no SEO, local deploy), so no Next.js.
**Alternatives.** Next.js — SSR/routing machinery irrelevant locally, complicates packaging; Redux — boilerplate without benefit at this state shape.
**Consequences.** + Simple packaging (static assets served by ASP.NET Core); portable feature `model/` layers for React Native. − No SSR if a marketing site is later needed (separate site solves that).

## ADR-013: Gamification is deterministic and event-driven

**Decision.** XP/streak/badge rules run in the Gamification context off domain events; the LLM only phrases encouragement text, never computes rewards. Prevents reward inconsistency and keeps motivation mechanics tunable/testable.

## ADR-014: Curriculum content as versioned seed packages with CI validation

**Decision.** Curriculum lives in `seeds/curriculum/de/*` JSON, validated in CI (DAG, CEFR consistency, error-code references, pacing bands), loaded by the seeding pipeline into versioned `curriculum_versions`. Published versions are immutable; learner state migrates by stable concept `code`.
**Consequences.** + Content PRs reviewable by linguists; rollback = version pin. − Migration tooling needed when codes change (rename map file).
