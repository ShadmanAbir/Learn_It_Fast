# 02 — Feature List

**Status:** Draft for review · MoSCoW-prioritized (M = Must, S = Should, C = Could, W = Won't in v1)

---

## F1. Onboarding & Placement

| ID | Feature | Prio |
|---|---|---|
| F1.1 | Account creation (local-first identity; optional cloud sync later) | M |
| F1.2 | Goal setting (target CEFR, deadline, minutes/day, motivation type) | M |
| F1.3 | Adaptive placement test (CAT-style: item difficulty adapts to responses; grammar + vocab + reading; optional listening/writing) | M |
| F1.4 | "I'm a total beginner" fast path (skip placement, start at A1.1) | M |
| F1.5 | Prior-knowledge import (self-reported known topics seed priors, verified later) | S |
| F1.6 | AI provider setup wizard (detect LM Studio/Ollama; test connection; model capability probe) | M |

## F2. Curriculum & Knowledge Graph

| ID | Feature | Prio |
|---|---|---|
| F2.1 | Seeded A1–C1 curriculum: grammar concepts, vocab topics, scenarios, reading/listening/writing objectives, culture notes | M |
| F2.2 | Dependency-aware knowledge graph (prerequisites, corequisites, "unlocks") | M |
| F2.3 | Visual curriculum map (graph view with mastery coloring) | S |
| F2.4 | Per-concept detail page: objective, examples, common mistakes, my mastery, my history | M |
| F2.5 | Curriculum versioning & migration of learner state across versions | S |
| F2.6 | Content authoring/admin tool for linguists (CRUD + validation of dependencies) | S |

## F3. Adaptive Learning Engine

| ID | Feature | Prio |
|---|---|---|
| F3.1 | Session planner: composes each study session (review %, new material %, interleaving, difficulty ramp) | M |
| F3.2 | Concept selector: eligible-frontier computation from knowledge graph + weakness ranking | M |
| F3.3 | Mastery model per concept (Bayesian, with confidence) | M |
| F3.4 | Automatic remediation: repeated failure triggers prerequisite re-check and scaffolded re-teach | M |
| F3.5 | Difficulty governor: keeps success rate in the 70–85 % "desirable difficulty" band | M |
| F3.6 | Lesson length adaptation to available time & fatigue signals | S |
| F3.7 | Learning-speed & retention-rate estimation feeding the planner | S |

## F4. Lesson Player & Exercise Types

| ID | Feature | Prio |
|---|---|---|
| F4.1 | Lesson player: teach → guided practice → free practice → wrap-up | M |
| F4.2 | Exercise types: multiple choice, cloze (gap fill), sentence ordering, matching, translation (both directions), transformation (e.g., present→Perfekt), error spotting, dictation, free writing, speaking prompt | M |
| F4.3 | Immediate feedback with explanation on every attempt | M |
| F4.4 | Hint ladder (scaffolding: nudge → rule reminder → partial answer) | S |
| F4.5 | "Explain this" — on-demand AI explanation grounded in the current concept | M |
| F4.6 | Grammar reference library (static, curated; AI examples on demand) | M |

## F5. Vocabulary Engine

| ID | Feature | Prio |
|---|---|---|
| F5.1 | Frequency-ranked lexicon with gender, plural, IPA, audio, example sentences, CEFR tag, topic | M |
| F5.2 | FSRS spaced-repetition reviews (recognition, recall, production, audio) | M |
| F5.3 | Leech detection & alternative practice for stubborn words | S |
| F5.4 | Personal vocabulary: add words met in reading/conversation | M |
| F5.5 | Topic packs aligned to lessons & scenarios | M |

## F6. Reading Engine

| ID | Feature | Prio |
|---|---|---|
| F6.1 | Level-appropriate reading passages (curated + AI-generated within constraints) | M |
| F6.2 | Tap-to-lookup with one-click "add to my vocabulary" | M |
| F6.3 | Comprehension questions (literal, inferential) auto-generated + validated | M |
| F6.4 | Reading speed (WPM) & unknown-word-rate measurement | S |

## F7. Listening Engine

| ID | Feature | Prio |
|---|---|---|
| F7.1 | TTS audio generation with transcript reveal | M |
| F7.2 | Comprehension questions; dictation mode; shadowing mode | M |
| F7.3 | Playback speed control (0.5×–1.25×) | M |
| F7.4 | Accent/voice selection (where provider supports) | C |

## F8. Writing Engine

| ID | Feature | Prio |
|---|---|---|
| F8.1 | Guided writing prompts per lesson/level | M |
| F8.2 | AI evaluation: error annotation (category-coded), naturalness, rewritten model version, rubric scores | M |
| F8.3 | Error→concept mapping (mistakes update the knowledge graph) | M |
| F8.4 | Writing history with improvement tracking | S |

## F9. Speaking & Conversation Engine

| ID | Feature | Prio |
|---|---|---|
| F9.1 | STT pipeline (provider-independent; local Whisper supported) | M |
| F9.2 | Scenario roleplay (restaurant, airport, hotel, office, interview, doctor, shopping, immigration, university, travel, friends, business) with level-constrained AI partner | M |
| F9.3 | Post-conversation feedback report (grammar, vocab use, task completion) | M |
| F9.4 | Pronunciation scoring (phoneme-level where provider supports; word-level fallback) | S |
| F9.5 | Free conversation mode (still level-constrained) | S |

## F10. Assessment Engine

| ID | Feature | Prio |
|---|---|---|
| F10.1 | Continuous per-skill CEFR estimation (reading, writing, listening, speaking, grammar, vocabulary, pronunciation) with confidence | M |
| F10.2 | Checkpoint assessments per curriculum unit (mastery gate) | M |
| F10.3 | Level-up exam (multi-skill, exit requirements per CEFR level) | M |
| F10.4 | Mock exam mode (Goethe/telc-style structure) | C |

## F11. Review Engine

| ID | Feature | Prio |
|---|---|---|
| F11.1 | Unified review queue (vocabulary + grammar micro-drills) with FSRS scheduling | M |
| F11.2 | Priority scoring: mistakes, difficulty, retention, recency, confidence | M |
| F11.3 | Daily review budget & overflow handling (postpone lowest priority) | M |
| F11.4 | Review-ahead & cram mode (exam prep; does not corrupt scheduler state) | C |

## F12. Analytics & Insights

| ID | Feature | Prio |
|---|---|---|
| F12.1 | Dashboards: vocabulary growth, grammar mastery heatmap, daily XP, weekly progress, reading speed, listening accuracy, writing improvement, speaking fluency, pronunciation trend, review completion, estimated CEFR timeline | M |
| F12.2 | Weakness report with recommended actions | M |
| F12.3 | Study calendar & time-on-task | S |

## F13. Gamification

| ID | Feature | Prio |
|---|---|---|
| F13.1 | XP + levels; streaks with freeze tokens | M |
| F13.2 | Badges & milestones (first 100 words, A2 unlocked, 30-day streak, …) | M |
| F13.3 | Daily/weekly goals with adaptive targets | M |
| F13.4 | Challenges (themed weeks, e.g., "Dativ week") | C |

## F14. Platform & Settings

| ID | Feature | Prio |
|---|---|---|
| F14.1 | AI provider management (multiple profiles, per-task routing, fallback chain) | M |
| F14.2 | Prompt template management (versioned, per model size variants) | M |
| F14.3 | Data export/import (full learner profile, JSON) | M |
| F14.4 | Feature flags; structured logging; health checks | M |
| F14.5 | Accessibility: keyboard-first, screen-reader labels, dyslexia-friendly font option | M |
| F14.6 | Background jobs: review scheduling, analytics rollups, content pre-generation cache | M |

---

## Prioritization rationale

- **Everything the learning loop needs is Must** (F1–F5, F8–F11 cores): without planner + mastery + reviews the product is "just another chatbot."
- **Pronunciation phoneme scoring is Should/Could** because provider capability varies wildly offline; word-level accuracy from STT confidence is an acceptable v1 fallback (see ADR-009).
- **Mock exams and challenges are Could**: valuable but not needed to validate the adaptive core.
