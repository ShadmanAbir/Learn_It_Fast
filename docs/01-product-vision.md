# 01 — Product Vision

**Project:** LIF — *Lernen ist Freiheit* (Adaptive AI German Tutor)
**Version:** 1.0 · **Status:** Draft for review

---

## 1. Vision Statement

> **LIF is a self-driving German school.** It takes a learner from zero (A1) to advanced
> proficiency (C1) through a continuously adapting, evidence-based curriculum — where the
> **application is the teacher** and the AI model is merely one of its instruments.

Unlike chatbot-based "AI tutors," LIF owns a structured CEFR-aligned curriculum, a knowledge
graph of the German language, and a persistent student model. Every minute of study is decided
deterministically by the platform's pedagogy engine; the LLM only *executes* teaching tasks
(explain, generate, evaluate) inside strict guardrails.

## 2. The Problem

| Existing solution | Weakness |
|---|---|
| Duolingo / Babbel / Busuu | Fixed linear paths, shallow adaptivity, weak grammar depth beyond B1, no true free production evaluation |
| ChatGPT-style tutoring | No curriculum, no memory of mastery, hallucinated progression, no spaced repetition, level drift |
| Classroom / Goethe courses | Expensive, schedule-bound, no per-learner pacing, feedback latency of days |
| Anki + textbooks | High discipline cost, no production practice, no assessment, no orchestration |

**Gap:** nobody combines a *structured, dependency-aware curriculum* with *AI-powered generation
and evaluation* and *continuous psychometric assessment* in one offline-capable product.

## 3. Product Principles (non-negotiable)

1. **The application is the teacher.** The LLM never decides *what* or *when* to teach.
2. **Curriculum is data, not generation.** All concepts, dependencies, and thresholds live in the database.
3. **Assessment is continuous.** Every interaction is an observation that updates skill estimates.
4. **Evidence-based pedagogy.** Active recall, spaced repetition, interleaving, scaffolding, mastery learning, immediate feedback, error-based learning.
5. **Provider independence.** Any LLM (3B local → frontier cloud) and any speech provider can be swapped without touching business logic.
6. **Offline first.** A learner with LM Studio and no internet gets the full experience.
7. **Small-model friendly.** Prompts are engineered so a 3B–14B local model produces reliable output.
8. **Commercial quality.** Clean Architecture, tests, telemetry, accessibility, i18n-ready UI.

## 4. Target Users (Personas)

| Persona | Description | Primary needs |
|---|---|---|
| **Amir, 29** — Immigrant to Germany | Needs B1 for residence, C1 for career. Studies evenings. | Structured path, speaking confidence, exam readiness (Goethe/telc) |
| **Sofia, 21** — University student | Erasmus semester, currently A2. | Fast vocabulary growth, listening comprehension, conversation practice |
| **Mark, 45** — Professional | Works with German clients, plateaued at B1. | Business scenarios, writing correction, weak-area targeting |
| **Lena, 34** — Privacy-conscious hobbyist | Refuses cloud AI. Runs LM Studio locally. | Fully offline operation, data ownership |

## 5. Value Proposition

- **Personalized pace:** the lesson planner selects the next best concept per learner, per session.
- **True production skills:** free writing and speaking are evaluated with structured feedback, not just multiple choice.
- **Never forget:** vocabulary and grammar enter a scientifically scheduled review queue (FSRS-based).
- **Transparent progress:** live CEFR estimation per skill with confidence intervals.
- **Your AI, your data:** local models, local database, exportable profile.

## 6. Success Metrics (North Stars)

| Metric | Target |
|---|---|
| 30-day retention | ≥ 40 % |
| Median time A1 → A2 (active learners, 30 min/day) | ≤ 10 weeks |
| Review completion rate | ≥ 70 % daily |
| Vocabulary retention at 30 days | ≥ 85 % of "mastered" items |
| CEFR estimate vs. external exam correlation | r ≥ 0.8 |
| Local-model output validity (parseable, constraint-respecting) | ≥ 95 % of generations |

## 7. Scope

### In scope (v1.x)
- Full A1→C1 German curriculum (grammar, vocabulary, reading, listening, writing, speaking, culture).
- Adaptive lesson planning, continuous assessment, FSRS spaced repetition.
- AI provider abstraction (LM Studio, Ollama, OpenAI, Claude, Gemini, Azure OpenAI, OpenRouter).
- Speech pipeline abstraction (STT/TTS provider-independent).
- Web app (React); desktop-class responsive UI; analytics dashboards; gamification.

### Out of scope (v1, future-ready)
- React Native mobile apps (architecture must not block them — API-first).
- Multi-language expansion (schema is language-parameterized but content is German-only).
- Social features (leaderboards limited to self-competition in v1).
- Official exam certification.

## 8. Key Differentiators — Design Decisions & Trade-offs

| Decision | Alternative considered | Why chosen |
|---|---|---|
| Deterministic lesson planner + LLM as executor | LLM-driven "agent tutor" | Reproducibility, no level drift, works with 3B models, testable pedagogy |
| Curriculum as seeded relational data | Runtime curriculum generation | Quality control by linguists, dependency integrity, offline operation |
| FSRS scheduler | SM-2 (Anki classic) | FSRS has demonstrably better retention/workload trade-off; parameters trainable per learner |
| Bayesian mastery estimation (BKT-style) per concept | Simple accuracy % | Handles guess/slip, gives confidence, supports "multiple observations before level change" |
| PostgreSQL for everything incl. knowledge graph | Neo4j for graph | One database to operate; graph is small (≈ 500 nodes) — recursive CTEs suffice; offline simplicity |
| Offline-first monolith with modular boundaries | Microservices | Single-machine deployment target; modular monolith keeps options open (see ADR-002) |

## 9. Naming

Working title **LIF** ("Lernen ist Freiheit"). Product-name research is a marketing task, out of engineering scope.
