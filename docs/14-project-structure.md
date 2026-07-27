# 14 — Project Folder Structure

**Status:** Draft for review · Clean Architecture, .NET 9 + React 19, monorepo.

---

## 1. Repository layout

```
LIF/
├─ docs/                                # this documentation set + ADRs
├─ src/
│  ├─ backend/
│  │  ├─ Lif.sln
│  │  ├─ Directory.Build.props          # lang version, nullable, analyzers, style
│  │  ├─ Directory.Packages.props       # central package management
│  │  │
│  │  ├─ Lif.SharedKernel/              # base types shared across contexts
│  │  │  ├─ Domain/                     # Entity, AggregateRoot, ValueObject, IDomainEvent
│  │  │  ├─ Results/                    # Result<T>, Error catalog
│  │  │  └─ Primitives/                 # CefrLevel, Skill, Difficulty, LanguageCode
│  │  │
│  │  ├─ Lif.Domain/                    # ZERO external dependencies
│  │  │  ├─ Curriculum/                 #   entities, VOs, events per bounded context
│  │  │  ├─ Learner/
│  │  │  ├─ Assessment/                 #   + pure domain services (AbilityUpdater, CefrDeriver)
│  │  │  ├─ Scheduling/                 #   + FsrsScheduler, ReviewPrioritizer (pure)
│  │  │  ├─ Learning/                   #   + SessionPlanner, DifficultyGovernor, MasteryUpdater
│  │  │  ├─ Production/
│  │  │  ├─ Conversation/
│  │  │  └─ Gamification/
│  │  │
│  │  ├─ Lif.Application/               # use cases (CQRS), ports, policies
│  │  │  ├─ Abstractions/               # IUnitOfWork, IClock, ICache, IEventBus
│  │  │  ├─ Ai/                         # TASK PORTS: IExplainer, IExerciseGenerator,
│  │  │  │                              #  IWritingEvaluator, IConversationPartner, …
│  │  │  │                              #  + ContextPack, ContextPackBuilder
│  │  │  ├─ Speech/                     # ISpeechToText, ITextToSpeech, IPronunciationScorer
│  │  │  ├─ Curriculum/{Commands,Queries}/
│  │  │  ├─ Learning/{Commands,Queries}/    # PlanSession, RecordAttempt, CompleteSession…
│  │  │  ├─ Reviews/  Assessment/  Vocabulary/
│  │  │  ├─ Writing/  Speaking/  Conversations/
│  │  │  ├─ Analytics/  Gamification/  Onboarding/
│  │  │  └─ Behaviors/                  # validation, logging, transaction pipeline
│  │  │
│  │  ├─ Lif.Infrastructure/            # cross-cutting adapters (no EF here)
│  │  │  ├─ Ai/
│  │  │  │  ├─ Orchestration/           # AiTaskOrchestrator, Router, FallbackChain
│  │  │  │  ├─ Prompting/               # PromptEngine, PlaceholderBinder, BudgetTrimmer
│  │  │  │  ├─ Validation/              # OutputValidator, ConstraintChecker, ErrorTaxonomy
│  │  │  │  └─ Providers/               # LmStudio/ Ollama/ OpenAi/ Anthropic/
│  │  │  │                              #  Gemini/ AzureOpenAi/ OpenRouter/
│  │  │  ├─ Speech/                     # WhisperLocal/, PiperTts/, cloud adapters
│  │  │  ├─ Caching/                    # Redis + in-memory fallback
│  │  │  ├─ Jobs/                       # Hangfire jobs: rollups, priorities, cache warmup
│  │  │  ├─ Storage/                    # audio blob storage (filesystem/S3-compatible)
│  │  │  └─ Security/                   # key encryption (DPAPI/keychain), token services
│  │  │
│  │  ├─ Lif.Persistence/               # EF Core 9 + Npgsql
│  │  │  ├─ LifDbContext.cs             # global query filters (per-learner isolation)
│  │  │  ├─ Configurations/<Context>/   # IEntityTypeConfiguration per aggregate
│  │  │  ├─ Repositories/
│  │  │  ├─ Migrations/
│  │  │  ├─ Outbox/                     # transactional outbox for async events
│  │  │  └─ Seeding/                    # seed-package loader + validators (DAG check…)
│  │  │
│  │  ├─ Lif.Api/                       # Presentation
│  │  │  ├─ Endpoints/<area>/           # minimal API groups mirroring 12-api-design
│  │  │  ├─ Sse/                        # SSE infrastructure for streaming
│  │  │  ├─ Auth/  Middleware/  HealthChecks/
│  │  │  └─ Program.cs                  # composition root
│  │  │
│  │  └─ tests/
│  │     ├─ Lif.Domain.Tests/           # pure unit + property-based (FsCheck)
│  │     ├─ Lif.Application.Tests/      # handler tests w/ fake ports
│  │     ├─ Lif.Simulation.Tests/       # simulated-student 90-day journeys (NFR-TST-2)
│  │     ├─ Lif.Ai.ContractTests/       # prompt golden tests, schema validation
│  │     ├─ Lif.Persistence.Tests/      # Testcontainers Postgres
│  │     ├─ Lif.Api.IntegrationTests/   # WebApplicationFactory end-to-end
│  │     └─ Lif.Architecture.Tests/     # NetArchTest layer rules (NFR-MNT-1)
│  │
│  └─ frontend/
│     ├─ package.json  vite.config.ts  tsconfig.json
│     └─ src/
│        ├─ app/                        # router, providers, layout shells
│        ├─ shared/
│        │  ├─ api/                     # generated OpenAPI client + TanStack Query hooks
│        │  ├─ ui/                      # design system (Radix + Tailwind)
│        │  ├─ lib/                     # sse client, audio recorder, i18n (ICU)
│        │  └─ config/
│        ├─ features/                   # feature-sliced, mirrors backend areas
│        │  ├─ today/        lesson-player/   review/
│        │  ├─ conversation/ writing/         reading/     listening/
│        │  ├─ vocabulary/   curriculum-map/  progress/
│        │  ├─ onboarding/   gamification/    settings-ai/ authoring/
│        │  └─ (each: components/ hooks/ model/ index.ts — public API only)
│        └─ test/                       # vitest setup, MSW handlers, e2e (Playwright)
│
├─ seeds/
│  ├─ curriculum/de/                    # versioned content packages (JSON)
│  │  ├─ a1/ a2/ b1/ b2/ c1/           # concepts, deps, units, scenarios, blueprints
│  │  └─ lexicon/                       # vocab entries + senses
│  └─ prompts/                          # template sources (task × size class × version)
│
├─ deploy/
│  ├─ docker-compose.yml                # app + postgres + redis (+ optional whisper, piper)
│  ├─ docker-compose.dev.yml
│  └─ k8s/                              # server-mode manifests (later phase)
│
├─ tools/
│  ├─ lif-cli/                          # ai-bench, seed-validate, recompute-mastery
│  └─ content-pipeline/                 # AI-assisted authoring drafts → review queue
│
└─ .github/workflows/                   # ci.yml (build+test+arch+seeds), release.yml
```

## 2. Dependency rules (enforced by `Lif.Architecture.Tests`)

```
SharedKernel ← Domain ← Application ← { Infrastructure, Persistence } ← Api
```

1. `Domain` references only `SharedKernel`. No EF, no AI, no HTTP.
2. `Application` references `Domain`; owns all **ports**; never references providers.
3. `Infrastructure`/`Persistence` implement ports; never reference each other's internals; wired only in `Api` (composition root).
4. Planner/scheduler namespaces (`Application.Learning`, `Domain.*`) MUST NOT reference AI task ports that *generate* content — plan first, generate later, enforced structurally.
5. Frontend features may import `shared/*` and their own slice only (`eslint-boundaries`).

## 3. Why these choices

| Choice | Alternative | Rationale |
|---|---|---|
| Single `Lif.Domain` with context folders | Assembly per bounded context | 9 assemblies × 4 layers = 36 projects of ceremony; folders + arch tests give the same discipline with monolith-friendly ergonomics; split later if a context is extracted |
| Separate `Persistence` from `Infrastructure` | One infrastructure project | DB migration lifecycle differs from AI/speech adapters; also keeps EF types out of AI adapter tests |
| Minimal APIs with endpoint groups | MVC controllers | Less ceremony, better perf, natural fit for CQRS thin endpoints |
| Feature-sliced React | Type-based folders (components/, pages/) | Feature slices mirror bounded contexts, keep AI-heavy features (conversation) isolated, scale to React Native reuse (`model/` layer is portable) |
| Seeds in repo as JSON packages | Content in migrations | Content authored/reviewed like code (PRs, CI validation) yet deployable without recompilation (NFR-MNT-4) |
| `lif-cli` tools project | Ad-hoc scripts | Recompute/bench/validate are product features for operators, not dev scripts |
