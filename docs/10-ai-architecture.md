# 10 — AI Architecture

**Status:** Draft for review · The AI subsystem as a strictly bounded *executor*, never a decision-maker.

---

## 1. Position in the system

```
Application layer (engines)                    Infrastructure.Ai
┌─────────────────────────────┐    ports    ┌──────────────────────────────┐
│ LessonEngine  WritingEngine │──────────►──│  AiTaskOrchestrator          │
│ ConversationEngine  etc.    │  IExplainer │   ├─ PromptEngine             │
│                             │  IExercise- │   ├─ OutputValidator          │
│  (decide WHAT & WHEN)       │  Generator  │   ├─ ConstraintChecker        │
│                             │  IWriting-  │   ├─ Router + FallbackChain   │
│                             │  Evaluator  │   ├─ GenerationCache          │
│                             │  IConversa- │   └─ IAiProvider ────────────►│ LM Studio
│                             │  tionPartner│        implementations        │ Ollama
└─────────────────────────────┘             └──────────────────────────────┘ OpenAI…
```

Two abstraction tiers — this is the key design decision:

1. **Task ports (Application):** `IExplainer`, `IExerciseGenerator`, `IDialogueGenerator`, `IWritingEvaluator`, `ISpeakingEvaluator`, `IConversationPartner`, `ITranslator`, `ISummarizer`, `IEncourager`. Business code speaks *pedagogy*, not prompts.
2. **Provider port (Infrastructure):** `IAiProvider` — one method surface: chat completion (+streaming, +JSON mode capability flags). Providers: LM Studio, Ollama, OpenAI, Anthropic, Gemini, Azure OpenAI, OpenRouter.

*Alternative considered:* a single `IAiProvider` used directly by engines. Rejected — engines would accumulate prompt logic and provider quirks, violating "business logic never depends on provider" and making the planner untestable without prompt fixtures.

```csharp
public interface IAiProvider
{
    AiProviderCapabilities Capabilities { get; }   // json_mode, streaming, max_ctx
    Task<AiCompletion> CompleteAsync(AiRequest request, CancellationToken ct);
    IAsyncEnumerable<AiToken> StreamAsync(AiRequest request, CancellationToken ct);
    Task<ProviderHealth> ProbeAsync(CancellationToken ct);   // setup wizard + readiness
}
```

## 2. Request lifecycle

```
Engine → AiTask { type, ContextPack, priority }
  1. Cache check      hash(taskType, templateVersion, contextPack) → hit? return
  2. Template select  by (taskType, provider.model_size_class), active version
  3. Prompt render    strict placeholder substitution — missing key = error, no silent blanks
  4. Route            task_routes[type] → provider chain
  5. Execute          timeout + retry (Polly); per-provider concurrency semaphore
  6. Validate         (a) JSON Schema  (b) ConstraintChecker (vocab/level/whitelists)
  7. On failure       repair-prompt retry (≤2) → next provider in chain → curated fallback
  8. Log + cache      ai.call_logs (latency, tokens, status); cache if cacheable type
```

**ContextPack** (the only student data the model ever sees):

```json
{
  "level": "A2.1",
  "objective": "Practice Dativ after 'mit' and 'bei'",
  "allowed_grammar": ["praesens", "akkusativ", "dativ.intro", "modal.koennen"],
  "forbidden_grammar": ["perfekt", "subclauses"],
  "allowed_vocab_hint": ["Arzt", "Termin", "Schmerzen", "..."],   // capped list, task-dependent
  "weak_concepts": ["dativ.articles"],
  "scenario_stage": { "role": "doctor", "goal": "elicit symptoms" },
  "constraints": { "max_sentences": 3, "language": "de", "output_schema": "dialogue.v2" }
}
```

No chat history is ever concatenated wholesale; the ConversationEngine passes a **rolling window of the last 4 turns + stage summary**, produced by the application (not the model).

## 3. Task taxonomy & routing defaults

| Task type | Output | Cacheable | Default local target | Escalation |
|---|---|---|---|---|
| `explain` | markdown, ≤ 200 words | ✔ | 7B–8B | cloud if configured |
| `gen_examples` | JSON list | ✔ | 3B–7B | — |
| `gen_exercise` | JSON (per-type schema) | ✔ | 7B | 14B on repeated schema failures |
| `gen_dialogue` | JSON turns | ✔ | 7B–8B | — |
| `gen_reading` | JSON passage+questions | ✔ | 8B–14B | cloud |
| `conversation_turn` | text (streamed) | ✘ | 7B–8B | — |
| `eval_writing` | JSON annotations+rubric | ✘ | 14B / cloud preferred | fallback: local + reduced-claim mode |
| `eval_answer` | JSON verdict | ✘ | 7B | — |
| `translate`, `summarize`, `encourage` | text/JSON | ✔/✘ | 3B–7B | — |

**Reduced-claim mode:** when only a small model is available for `eval_writing`, the pipeline narrows the ask (error *detection* on a fixed taxonomy, no rubric nuance) and the UI labels feedback confidence accordingly. Honest degradation beats hallucinated precision.

## 4. Small-model optimization strategy (3B–14B)

1. **One task, one call.** Never multi-objective prompts. Compound needs = pipeline of small calls.
2. **Terse templates per size class.** `s3b` templates: ≤ 300 token system prompt, single example, rigid output skeleton. `cloud` templates may be richer.
3. **JSON discipline:** use provider JSON mode / GBNF grammar where available (LM Studio, Ollama); otherwise skeleton-echo technique (model completes a pre-opened JSON object).
4. **Few-shot from curriculum, not invented:** the 1–2 examples embedded in generation templates come from the concept's curated examples — doubles as style anchor and constraint demonstration.
5. **Token budgets enforced** (FR-AI-6): context packs cap vocab hint lists (e.g., 40 words max, prioritized by relevance) — retrieval, not history.
6. **Determinism bias:** temperature 0.2–0.4 for generation, 0.0 for evaluation, 0.7 only for conversation turns.
7. **Capability probe on setup:** JSON-compliance and German-quality smoke tests grade each configured model per task type (`ai.providers.capabilities`), feeding routing defaults and wizard verdicts.

## 5. Trust model — what AI output may touch

| AI output | May directly affect | Never affects |
|---|---|---|
| Generated exercise | displayed content (post-validation) | which concept is practiced, difficulty label |
| Answer evaluation | attempt outcome (bounded 0..1, logged as `evaluated_by: ai`) | mastery *formula*, thresholds |
| Writing annotations | mistake records (only via known error-taxonomy codes) | concept graph, curriculum |
| Conversation turn | dialogue text (post constraint check) | scenario stage advancement (app checks exit conditions) |
| Encouragement | UI message | anything |

Evaluation observations sourced from AI carry `weight < 1.0` in the assessment stream and are down-weighted further when deterministic cross-checks disagree (FR-ASM-6).

## 6. Speech providers

Same two-tier pattern: `ISpeechToText` (local Whisper via whisper.cpp server, or cloud), `ITextToSpeech` (local Piper-class, or cloud). Pronunciation scoring adapter exposes word-level scores from STT confidences in v1; phoneme-level interface reserved (`IPronunciationScorer`) for providers that support it.

## 7. Failure & resilience policy

- Per-provider circuit breaker; trip → next in chain; all tripped → curated-content fallback + user-visible "AI offline, using library content" banner.
- Conversation engine keeps a stock of scenario-scripted fallback lines so roleplay degrades to branching-script mode rather than dying.
- All failures are observable: `ai.call_logs.status` distribution is a first-class dashboard for operators.

---

# See also
`11-prompt-architecture.md` for the Prompt Engine internals and template catalog.
