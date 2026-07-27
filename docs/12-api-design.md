# 12 — API Design

**Status:** Draft for review · REST over HTTPS, JSON; versioned `/api/v1`; OpenAPI generated from code; SSE for AI streaming.

---

## 1. Conventions

- **Style:** resource-oriented REST; commands that aren't CRUD use verb sub-resources (`.../attempts`, `.../complete`). CQRS internally; the API mirrors commands/queries 1:1.
- **Auth:** JWT bearer (ASP.NET Core Identity). Local single-user mode auto-issues a device token — same API shape everywhere (React Native-ready).
- **Errors:** RFC 9457 `application/problem+json` with stable `errorCode`.
- **Pagination:** cursor-based (`?cursor=&limit=`); **Idempotency:** mutating AI-cost endpoints accept `Idempotency-Key`.
- **Streaming:** Server-Sent Events for conversation turns and explanations (`text/event-stream`); WebSocket reserved for future live audio.
- **Versioning:** URI major version; additive changes only within v1.

## 2. Endpoint map

### Auth & Profile
```
POST   /api/v1/auth/register | /auth/login | /auth/refresh
GET    /api/v1/me                          # profile + goals + cefr state
PUT    /api/v1/me/goals
PUT    /api/v1/me/preferences
GET    /api/v1/me/export                   # full JSON archive (async job → download)
POST   /api/v1/me/import
```

### Onboarding & Placement
```
POST   /api/v1/placement                   # start adaptive session (or skip:true)
GET    /api/v1/placement/{id}/next-item
POST   /api/v1/placement/{id}/answers      # → next item | finished + result
```

### Study Sessions (the core loop)
```
POST   /api/v1/sessions                    # body: {timeBudgetMin} → planned session (plan visible)
GET    /api/v1/sessions/current
POST   /api/v1/sessions/{id}/start
GET    /api/v1/sessions/{id}/activities/{aid}            # activity payload (exercises etc.)
POST   /api/v1/exercises/{id}/attempts     # {response, hintsUsed, latencyMs}
                                           # → {outcome, feedback, explanation, conceptLinks}
POST   /api/v1/exercises/{id}/hints        # next hint in ladder
POST   /api/v1/sessions/{id}/activities/{aid}/complete
POST   /api/v1/sessions/{id}/complete      # → summary, XP, streak, next-review preview
POST   /api/v1/sessions/{id}/abandon
```

### Reviews
```
GET    /api/v1/reviews/queue?limit=50      # due items, priority-ordered, budget-aware
POST   /api/v1/reviews/{itemId}/answers    # {rating: again|hard|good|easy, elapsedMs}
GET    /api/v1/reviews/forecast?days=30    # upcoming load chart
```

### Curriculum & Knowledge Graph (read-only for learners)
```
GET    /api/v1/curriculum/levels                        # structure + my coverage
GET    /api/v1/curriculum/units/{id}
GET    /api/v1/curriculum/concepts/{id}                 # content + my mastery + history
GET    /api/v1/curriculum/graph?level=A2                # nodes+edges+my mastery (map view)
```

### Vocabulary
```
GET    /api/v1/vocabulary?status=learning&topic=food&cursor=...
GET    /api/v1/vocabulary/{entryId}                     # entry + senses + my state
POST   /api/v1/vocabulary/personal                      # {lemma, contextSentence, sourceRef}
GET    /api/v1/vocabulary/stats                         # sizes by status, growth series
```

### Skills
```
POST   /api/v1/reading/sessions            # planner-driven or from library
POST   /api/v1/reading/sessions/{id}/lookups            # word tap
POST   /api/v1/reading/sessions/{id}/answers
POST   /api/v1/listening/sessions          # {mode: comprehension|dictation|shadowing}
POST   /api/v1/listening/sessions/{id}/answers
GET    /api/v1/audio/{ref}                 # generated TTS (cached), range requests
POST   /api/v1/writing/submissions         # {promptId?, text} → evaluation report
GET    /api/v1/writing/submissions?cursor=              # history with scores
POST   /api/v1/speaking/attempts           # multipart audio → transcript + scores + feedback
```

### Conversation (SSE)
```
POST   /api/v1/conversations               # {scenarioId} → session + opening turn
POST   /api/v1/conversations/{id}/turns    # {text | audioRef} → SSE stream of AI turn
POST   /api/v1/conversations/{id}/complete # → feedback report
GET    /api/v1/scenarios?level=mine        # unlocked scenarios (gated by graph)
```

### Assessment & Progress
```
GET    /api/v1/assessment/skills           # 7 estimates + overall + confidence + trend
GET    /api/v1/assessment/level-up/eligibility
POST   /api/v1/assessment/exams            # start checkpoint/level-up (when eligible)
POST   /api/v1/assessment/exams/{id}/sections/{sid}/answers
POST   /api/v1/assessment/exams/{id}/complete            # → result | gap report
```

### Analytics & Gamification
```
GET    /api/v1/analytics/dashboard?range=30d             # rollup bundle for dashboard
GET    /api/v1/analytics/weaknesses
GET    /api/v1/gamification/state          # xp, level, streak, goals, badges
```

### AI Administration (settings UI)
```
GET    /api/v1/ai/providers                # configured providers + probe status
POST   /api/v1/ai/providers                # add; POST /{id}/probe → capability report
PUT    /api/v1/ai/routes                   # task routing table
GET    /api/v1/ai/health                   # chain status per task type
```

### Authoring (role-gated, separate SPA area)
```
POST/PUT /api/v1/authoring/concepts …      # full curriculum CRUD + /validate + /publish
```

### Ops
```
GET /health/live | /health/ready           # liveness; readiness = DB+Redis+AI ping
```

## 3. Representative contracts

**POST /sessions → 201**
```json
{
  "id": "0198...","status": "planned","timeBudgetMin": 30,
  "plan": {
    "summary": {"reviewShare": 0.4, "newShare": 0.4, "freeShare": 0.2},
    "activities": [
      {"id":"a1","kind":"review","estimatedMin":10,"itemCount":24},
      {"id":"a2","kind":"lesson","estimatedMin":12,
       "concepts":[{"id":"c-dativ-art","title":"Dative articles"}]},
      {"id":"a3","kind":"conversation","estimatedMin":8,
       "scenario":{"id":"s-doctor","title":"Beim Arzt"}}
    ],
    "plannerVersion":"1.3.0","rationale":[
      "24 reviews due (priority-capped from 41)",
      "Dative articles: frontier concept, blocks 6 concepts",
      "Doctor scenario reinforces today's Dativ focus"
    ]
  }
}
```
`rationale` is user-visible: the planner explains itself — transparency is a retention feature and a debugging tool.

**POST /exercises/{id}/attempts → 200**
```json
{
  "outcome": 0.5,
  "feedback": {
    "verdict":"partially_correct",
    "errors":[{"code":"CASE_DAT","span":[12,15],"wrong":"dem","right":"der",
               "explanation":"'mit' takes dative; 'Tasche' is feminine → 'der'",
               "conceptId":"c-dativ-art"}],
    "encouragement":"Fast richtig!"
  },
  "masteryDelta":{"conceptId":"c-dativ-art","from":0.62,"to":0.58},
  "evaluatedBy":"deterministic"
}
```

**Problem details example — AI degraded (503)**
```json
{ "type":"https://lif.app/errors/ai-unavailable", "title":"AI provider unavailable",
  "status":503, "errorCode":"AI_CHAIN_EXHAUSTED",
  "detail":"All providers for task 'conversation_turn' failed; fallback content served where possible.",
  "correlationId":"..." }
```

## 4. Design decisions & trade-offs

| Decision | Alternative | Rationale |
|---|---|---|
| REST + SSE | GraphQL | Screens map 1:1 to composed queries (dashboard bundle endpoints); GraphQL's flexibility unneeded and complicates caching/rate limiting of AI-cost endpoints |
| SSE for streaming | WebSockets everywhere | SSE is HTTP-native (proxies, auth, retries trivial); only future live-audio needs WS |
| Server returns *feedback + mastery delta* in the attempt response | Client computes/fetches separately | One round-trip for the tightest UX loop (US-2.3); keeps scoring authority server-side |
| Cursor pagination | Offset | Append-heavy history tables; stable under inserts |
| Planner `rationale` in API | Opaque plans | Explainability requirement; also enables snapshot-testing planner behavior end-to-end |
