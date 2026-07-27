# 08 — Knowledge Graph Design

**Status:** Draft for review · The German language as a dependency DAG driving all progression decisions.

---

## 1. Purpose

The knowledge graph is the **single source of truth for progression**. It answers, deterministically:

1. *What can this learner study next?* (frontier computation)
2. *Why is this learner failing X?* (prerequisite diagnosis)
3. *What must be reviewed before it decays?* (importance-weighted scheduling)
4. *Is this learner ready for a level-up exam?* (coverage check)

The LLM never traverses or modifies this graph.

## 2. Node model

Nodes = `curriculum.concepts` (see `07-database-schema.md`). ~450–550 nodes for A1–C1.

**Node kinds**

| Kind | Example | Share |
|---|---|---|
| `grammar` | `gram.case.dativ.definite-articles` | ~55 % |
| `function` (communicative) | `func.request.polite` ("making polite requests") | ~25 % |
| `skill` | `skill.listening.gist.a2` | ~15 % |
| `culture` | `cult.du-sie` | ~5 % |

**Per-node static data (curriculum):** CEFR level, difficulty 1–10, learning objective, curated explanation, examples, common mistakes (with error codes), estimated minutes, mastery threshold, importance weight, required vocabulary.

**Per-node learner state (`learner.concept_mastery`):** mastery, confidence, attempts, wrong-streak, last practiced, provisional flag — plus FSRS state when the concept has a drill item in the review queue.

## 3. Edge model

```
(concept) --requires(min_mastery=0.8)--> (prerequisite)
(concept) --supports--> (helper)          # soft edge: boosts, doesn't gate
```

- **`requires`**: hard gate. Concept enters the frontier only when *all* `requires` prerequisites meet their `min_mastery`.
- **`supports`**: soft edge used for (a) diagnosis ordering, (b) interleaving affinity, (c) partial credit propagation (§6). Never gates.

**Why two edge types:** a single hard-edge model either over-gates (learner blocked by pedantic links) or under-gates (missing real prerequisites). Alternatives considered: weighted single edge type (harder to author and explain), no soft edges (loses diagnosis quality). Two named types keep authoring intuitive for linguists.

## 4. Backbone of the German graph (excerpt)

The full graph is seed data (`seeds/curriculum/de/*.json`); the A1→B1 grammar spine:

```
gram.gender.articles.def ──► gram.gender.articles.indef
        │                            │
        ▼                            ▼
gram.verb.praesens.regular ──► gram.wordorder.v2 ──► gram.negation.nicht-kein
        │                            │
        ▼                            ▼
gram.verb.praesens.irregular   gram.case.akkusativ ──► gram.pronoun.akk
        │                            │
        ▼                            ▼
gram.verb.modal ────────────► gram.wordorder.bracket (Satzklammer)
        │                            │
        ▼                            ▼
gram.verb.separable            gram.case.dativ ──► gram.prep.dativ / gram.prep.wechsel
        │                            │
        ▼                            ▼
gram.verb.perfekt ──────────► gram.wordorder.subclause (weil/dass)
        │                            │
        ▼                            ▼
gram.verb.praeteritum.haben-sein    gram.adjective.endings.nom-akk
        │                            │
        ▼                            ▼
gram.clause.relative ◄──────── gram.adjective.endings.dativ-gen
        │
        ▼
gram.voice.passiv.praesens ──► gram.voice.passiv.vergangenheit
        │
        ▼
gram.mood.konjunktiv2.wuerde ──► gram.mood.konjunktiv2.full ──► gram.mood.konjunktiv1.reported
        │
        ▼
gram.style.nominalisierung ──► skill.writing.essay.c1
```

Functional nodes hang off grammar nodes (e.g., `func.ordering-food` *requires* `gram.verb.modal` ("ich möchte…") + vocab pack `food.basic`), which is exactly how the conversation engine gates scenarios.

## 5. Frontier computation

```
eligible(c) :=  mastery(c) < threshold(c)
            AND ∀ p ∈ requires(c): mastery(p) ≥ min_mastery(edge)
frontier    :=  { c | eligible(c) } ranked by:
                  1. CEFR distance from learner level  (closest first)
                  2. importance weight                  (descending)
                  3. unit order                         (curriculum coherence)
                  4. dependency out-degree              (unlocks more first)
```

Implementation: recursive CTE over `concept_dependencies` joined with `concept_mastery`; result cached per learner in Redis, invalidated by `ConceptMasteryChanged`. p95 target < 50 ms cold (≈500 nodes).

## 6. Mastery dynamics on the graph

- **Update (BKT-style):** per observation, Bayesian update with concept-level guess/slip priors (defaults g=0.2, s=0.1; MC exercises use option-count-aware guess). Hint usage scales outcome credit ×(1 − 0.25·hints).
- **Decay:** mastery is not decayed synthetically; instead, *retrievability* from the FSRS state of the concept's drill item modulates *effective mastery* used by the planner: `effective = mastery × max(retrievability, floor=0.6)`. This keeps "learned but stale" concepts re-entering practice without corrupting the learned-probability semantics.
- **Propagation:**
  - Failing concept C with high mastery on paper → increases *review priority* of its `requires` parents (diagnosis first, re-teach only if parents also fail re-check).
  - Success on C grants small credit (×0.1 weight observations) to `supports` sources — practicing Perfekt inherently exercises word order.
- **Remediation trigger (FR-PLN-4):** `streak_wrong ≥ 3` → planner inserts prerequisite re-check activity; if a prerequisite fails, its mastery is cut to `min(mastery, 0.6)` and a re-teach lesson is scheduled.

## 7. Review priority (per graph node & word)

```
priority = w1·overdueness            # (now − due)/interval, capped
         + w2·error_rate_recent      # last 20 attempts
         + w3·importance             # node weight (out-degree normalized in)
         + w4·(1 − retrievability)   # FSRS forgetting risk
         + w5·(1 − confidence)       # uncertain estimates gain observations
(w defaults: 0.30/0.25/0.15/0.20/0.10 — config, unit-tested)
```

## 8. Level coverage & exit requirements

A CEFR level is *exam-ready* when:
- ≥ 90 % of the level's `requires`-reachable concepts have `mastery ≥ threshold`,
- every skill estimate ≥ level minimum with confidence ≥ 0.7,
- vocabulary: known words ≥ level target (see `09-cefr-curriculum.md`).

Coverage is a pure graph query — the exit gate is fully explainable to the learner ("you're missing these 4 concepts").

## 9. Authoring & validation rules (CI-enforced on seed data)

1. Graph is a DAG (topological sort must succeed).
2. No `requires` edge crosses more than one CEFR level downward.
3. Every non-A1.1 concept has ≥ 1 `requires` edge (no orphan advanced nodes).
4. Required vocabulary of a concept is at/below the concept's CEFR.
5. Every `common_mistake.error_code` exists in the error taxonomy.
6. Every scenario/writing prompt references existing concepts.
7. Estimated minutes per unit sum within band (guards curriculum pacing).

## 10. Storage decision recap

Adjacency list in PostgreSQL + recursive CTE + Redis-cached per-learner frontier. Neo4j/graph DB rejected: operational cost for an offline product, tiny graph, and all queries are bounded-depth. If multi-language expansion multiplies node counts, the query layer (`IFrontierRepository`) isolates a future switch.
