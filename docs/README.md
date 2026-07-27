# LIF — Adaptive AI German Tutor · Architecture & Design Package

**Version 1.0 · Status: approved** — review gate passed; ADRs flipped to *Accepted*; implementation started with Phase 0 of the [roadmap](16-development-roadmap.md).

## Core philosophy

> **The application is the teacher. The AI model is only one tool used by the teacher.**
> The LLM never owns the curriculum, never decides progression — it explains, generates, evaluates, and converses inside strict, validated constraints.

## Deliverables

| # | Document | Covers |
|---|---|---|
| 1 | [Product Vision](01-product-vision.md) | Vision, personas, principles, success metrics, scope |
| 2 | [Feature List](02-feature-list.md) | MoSCoW-prioritized feature catalog (F1–F14) |
| 3 | [User Stories](03-user-stories.md) | 9 epics with acceptance criteria |
| 4 | [Functional Requirements](04-functional-requirements.md) | Testable FRs per engine |
| 5 | [Non-Functional Requirements](05-nfr.md) | Performance, offline-first, security, testability… |
| 6 | [Domain Model](06-domain-model.md) | Bounded contexts, aggregates, events, pure domain services |
| 7 | [Database Schema](07-database-schema.md) | PostgreSQL DDL across 9 schemas + design rationale |
| 8 | [Knowledge Graph Design](08-knowledge-graph.md) | German dependency DAG, frontier, mastery dynamics |
| 9 | [CEFR Curriculum Structure](09-cefr-curriculum.md) | A1→C1 level definitions, targets, exit requirements |
| 10 | [AI Architecture](10-ai-architecture.md) | Two-tier abstraction, 7 providers, trust model, small-model strategy |
| 11 | [Prompt Architecture](11-prompt-architecture.md) | Prompt Engine, template catalog, error taxonomy, validation |
| 12 | [API Design](12-api-design.md) | REST + SSE endpoint map and contracts |
| 13 | [UI/UX Wireframes](13-ui-ux-wireframes.md) | IA + low-fi frames for all core screens |
| 14 | [Project Folder Structure](14-project-structure.md) | Clean Architecture monorepo layout + dependency rules |
| 15 | [ADRs](15-adrs.md) | 14 architecture decisions with alternatives & trade-offs |
| 16 | [Development Roadmap](16-development-roadmap.md) | Phases 0–5 with exit demos + risk register |
| 17 | [Testing Strategy](17-testing-strategy.md) | Test pyramid + pedagogy simulation + AI contract rings |
| 18 | [Deployment Strategy](18-deployment-strategy.md) | Local-first & server modes, release engineering |

## How to review

1. Start with **01 → 03** (what we're building and why).
2. Then **06 → 09** (the pedagogical machine: domain, data, graph, curriculum).
3. Then **10 → 11** (how AI is caged and used).
4. Finish with **15** (decisions) and **16** (sequencing).

Open questions worth explicit reviewer sign-off:
- FSRS + BKT + Elo/IRT-lite parameter defaults (docs 8, 15/ADR-004–006) — tunable, but defaults set expectations.
- Content production plan (doc 9 §5) — linguist capacity is the schedule's long pole.
- v1 pronunciation scope (ADR-009) — word-level only; confirm acceptable for launch.

## What happens after approval

Phase 0 of the [roadmap](16-development-roadmap.md): repo scaffold, CI, architecture tests, compose stack — then the Phase 1 walking skeleton of the full adaptive loop on an A1.1 curriculum slice.
