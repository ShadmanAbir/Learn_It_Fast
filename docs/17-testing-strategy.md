# 17 — Testing Strategy

**Status:** Draft for review · Test pyramid + two LIF-specific layers: **pedagogy simulation** and **AI contract testing**.

---

## 1. Test taxonomy & tooling

| Layer | Project | Tools | Scope |
|---|---|---|---|
| Unit (domain) | `Lif.Domain.Tests` | xUnit, FluentAssertions, FsCheck | Pure services: FSRS, BKT, Elo, planner, prioritizer, governor, VOs |
| Unit (application) | `Lif.Application.Tests` | xUnit, NSubstitute fakes for ports | Command/query handlers, ContextPackBuilder, policies |
| Architecture | `Lif.Architecture.Tests` | NetArchTest | Layer rules, context boundaries, "planner never touches generators" |
| Persistence | `Lif.Persistence.Tests` | Testcontainers (Postgres 16) | Mappings, repositories, query filters, migrations, outbox |
| AI contract | `Lif.Ai.ContractTests` | xUnit + recorded fixtures | Prompt rendering, schema validation, constraint checker, repair loop |
| API integration | `Lif.Api.IntegrationTests` | WebApplicationFactory + Testcontainers | Endpoint contracts, auth, SSE, problem-details, isolation |
| Simulation | `Lif.Simulation.Tests` | custom harness | 90-day virtual learner journeys (pedagogy regression) |
| Frontend unit | `frontend/test` | Vitest + Testing Library + MSW | Components, hooks, stores |
| E2E | Playwright | | Core loop happy paths + a11y assertions (axe) |
| Mutation | Stryker.NET | | Scheduler & mastery math only (NFR-MNT-5) |

Coverage gates: Domain ≥ 90 %, Application ≥ 80 %; CI fails below.

## 2. Pedagogy algorithm testing (the crown jewels)

**Reference-vector tests:** FSRS implementation validated against published algorithm test vectors; BKT and Elo against hand-computed spreadsheets checked by the Ed-Tech role.

**Property-based tests (FsCheck):**
- FSRS: intervals monotonic in rating; stability never negative; `Again` always shortens next interval.
- BKT: mastery stays in [0,1]; observation order-insensitivity within tolerance for equal-weight batches; hint discount strictly reduces credit.
- Planner: plan fits time budget ±10 %; identical (state, seed) ⇒ identical plan; every planned concept is frontier-eligible; review share respects due pressure bounds.
- Frontier: never contains a node with an unmet `requires` edge; monotone under mastery increase.
- Difficulty governor: never moves more than one band per session; converges into 70–85 % success band in simulation.

**Simulation harness (NFR-TST-2):** virtual students with parameterized (learning rate, forgetting rate, session skip probability, guess tendency) run full journeys against the real planner/scheduler/assessment stack (fake AI ports return curated content). CI asserts invariants:
- review debt stays bounded for daily-active profiles;
- median simulated A1→A2 time within design band;
- no oscillation (concept taught-forgotten-retaught > 4 cycles flags failure);
- level never promoted with < required observations.
Runs nightly with 200 students; a 20-student smoke version on every PR.

## 3. AI testing without flakiness

**Principle:** CI never calls a live LLM. Three rings:

1. **Ring 1 — pure:** PromptEngine rendering (golden files per template × size class), placeholder strictness, BudgetTrimmer determinism, JSON Schema validation, ConstraintChecker (German test corpus with known violations: forbidden Perfekt at A1, off-whitelist vocab, sentence-length breaches).
2. **Ring 2 — recorded:** fixture library of real model outputs (good, malformed JSON, constraint-violating, truncated) per provider class, replayed through the orchestrator to test the full validate→repair→fallback path. Fixtures refreshed by a scheduled non-blocking job that runs the live bench and diffs validity rates.
3. **Ring 3 — live bench (manual/nightly, non-gating):** `lif ai-bench` runs the template suite against configured local models (3B/7B/8B/14B) and reports per-task validity %, informing routing defaults and template tuning. Results are tracked in a dashboard, alarmed at < 95 % (Success Metric).

**Evaluation-quality audits:** a curated set of A1–B1 learner texts with linguist-annotated gold errors; `eval_writing` outputs scored for precision/recall per error code. Gate: precision ≥ 0.8 on cloud/14B class before enabling full-claim mode by default.

## 4. Integration & E2E specifics

- **Per-user isolation:** integration tests assert cross-user data access is impossible (query-filter regression suite) — a hard security gate (NFR-SEC-3).
- **SSE:** conversation stream tests cover mid-stream provider failure → graceful fallback message.
- **Outbox:** kill-and-restart tests verify no lost/duplicated domain events.
- **Migrations:** every migration applied against a seeded snapshot of the previous release schema in CI (NFR-REL-4).
- **E2E happy paths:** onboarding→first lesson; review session with keyboard only; writing submission→feedback; provider-down degradation banner. Each E2E run includes axe accessibility scans on core screens.

## 5. Non-functional test hooks

- **Performance:** k6 scripts for NFR-PERF targets (attempt validation, plan computation, review queue) run against compose stack in nightly CI; regression budget ±15 %.
- **Load (server mode):** 10 k-user soak before any hosted launch (Phase 5).
- **Chaos-lite:** AI provider fault injection (timeouts, garbage, slow-drip tokens) via fake provider in integration runs.

## 6. Definition of Done (per story)

1. Unit + integration tests written and green; coverage gate holds.
2. Architecture tests unaffected or updated with ADR note.
3. If AI-touching: fixtures added for new template/schema; validity ≥ 95 % on recorded ring.
4. If pedagogy-touching: simulation smoke passes; property tests extended.
5. A11y check for new UI (keyboard + axe).
6. Docs updated (API doc regen, ADR if decision changed).
