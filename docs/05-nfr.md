# 05 — Non-Functional Requirements

**Status:** Draft for review · `NFR-<area>-<n>`; each has a measurable acceptance criterion.

---

## Performance & Responsiveness

- **NFR-PERF-1** Deterministic exercise validation p95 < 200 ms (server-side).
- **NFR-PERF-2** Session plan computation p95 < 500 ms for a learner with 5 000 tracked items.
- **NFR-PERF-3** AI-dependent interactions show streaming or skeleton UI within 300 ms; conversation first-token < 3 s on a local 8B model (RTX-class GPU) — degraded-mode messaging if exceeded.
- **NFR-PERF-4** Dashboard queries served from rollups: p95 < 300 ms.
- **NFR-PERF-5** Review queue fetch (500 due items) p95 < 250 ms.

## Offline-First & Availability

- **NFR-OFF-1** Full learning loop (plan → lesson → evaluate → review) MUST work with zero internet: local LLM (LM Studio/Ollama), local Postgres, local STT/TTS (Whisper / Piper-class) or graceful text-only fallback.
- **NFR-OFF-2** Cloud providers are optional enhancements; any cloud outage MUST degrade to local routing automatically via the fallback chain.
- **NFR-OFF-3** Single-machine deployment via `docker compose up` (app + Postgres + Redis) or native install; no mandatory external services.

## Reliability & Data Integrity

- **NFR-REL-1** Attempt log is append-only; learner analytical state (mastery, estimates) MUST be recomputable from it (recovery tool provided).
- **NFR-REL-2** AI output failures (schema-invalid, constraint-violating) MUST never surface raw to the learner; fallback content always available for Must-have flows.
- **NFR-REL-3** Background jobs are idempotent and safely re-runnable.
- **NFR-REL-4** Database migrations are forward-only, tested against seeded snapshots of prior versions.

## Security & Privacy

- **NFR-SEC-1** All provider API keys encrypted at rest; never logged; never included in exports.
- **NFR-SEC-2** Local mode: no telemetry leaves the machine without explicit opt-in.
- **NFR-SEC-3** Multi-user server mode: ASP.NET Core Identity + JWT; per-user data isolation enforced at the query layer (global query filters + integration tests).
- **NFR-SEC-4** OWASP ASVS L2 alignment for the API surface; dependency scanning in CI.
- **NFR-SEC-5** Learner audio recordings stored locally by default, deletable, excluded from export unless opted in.

## Scalability

- **NFR-SCA-1** Architecture MUST support 10 k concurrent learners in server mode by scaling API nodes horizontally (stateless API; state in Postgres/Redis).
- **NFR-SCA-2** AI throughput is the bottleneck by design: task queue with per-provider concurrency limits and backpressure (429 + retry-after to clients).
- **NFR-SCA-3** Attempt/event tables partitioned by month when row count > 10 M.

## Maintainability & Extensibility

- **NFR-MNT-1** Clean Architecture layering enforced by architecture tests (NetArchTest): Domain has zero dependencies; Application depends only on Domain; no engine references a concrete AI provider.
- **NFR-MNT-2** New AI provider addable by implementing `IAiProvider` + config only (measured: no diff outside `Infrastructure.Ai.*` + config).
- **NFR-MNT-3** New exercise type addable without modifying planner core (strategy registration).
- **NFR-MNT-4** Curriculum content changes require no code deployment (data + optional seed package).
- **NFR-MNT-5** Code coverage gates: Domain ≥ 90 %, Application ≥ 80 % lines; mutation testing on scheduler & mastery math.

## Testability

- **NFR-TST-1** All engines testable without a live LLM (recorded fixtures + fake providers).
- **NFR-TST-2** Learning simulation harness: simulated students (fast/slow/forgetful profiles) run 90-day journeys in CI to detect pedagogy regressions (e.g., review debt explosion, difficulty oscillation).
- **NFR-TST-3** Prompt templates have golden-output contract tests per supported model class.

## Accessibility & UX Quality

- **NFR-ACC-1** WCAG 2.2 AA: keyboard navigation for every exercise type, ARIA labels, visible focus, contrast ≥ 4.5:1.
- **NFR-ACC-2** Reduced-motion mode; dyslexia-friendly font toggle; adjustable font size.
- **NFR-ACC-3** All audio content has text alternatives (transcripts revealable post-attempt).
- **NFR-UX-1** Responsive 360 px → 1920 px; core loop fully usable on mobile browser (React Native path later).

## Internationalization

- **NFR-I18N-1** UI strings externalized (ICU message format); v1 ships English + German UI.
- **NFR-I18N-2** Schema is target-language-parameterized (`language_code` on curriculum roots) to keep multi-language expansion open.

## Observability

- **NFR-OBS-1** Structured logging (Serilog) with correlation IDs across request → AI call → persistence.
- **NFR-OBS-2** Metrics: AI latency/token usage/failure rate per provider & task type; planner decisions; review completion — exposed via OpenTelemetry.
- **NFR-OBS-3** Health endpoints: liveness, readiness (DB, Redis, configured AI provider ping).

## Compliance

- **NFR-CMP-1** GDPR-ready: export (Art. 20), erasure (Art. 17) implemented for server mode.
- **NFR-CMP-2** Licensing audit for lexicon/frequency data sources tracked in `docs/content-licenses.md` (to be created with content work).

---

## Trade-off notes

- **Offline-first vs. best AI quality:** we accept lower generation quality locally; the constraint architecture (whitelists + schema validation + fallbacks) is designed so *pedagogy never depends on model quality* — the planner and validators carry the quality floor.
- **Recomputability (NFR-REL-1) vs. storage:** append-only attempt logs cost storage but make every scoring-algorithm improvement retroactive — essential for an evolving assessment engine.
- **Strict layering vs. speed:** architecture tests add friction but prevent the most expensive failure mode of AI apps: provider details leaking into pedagogy logic.
