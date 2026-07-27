# 07 — Database Schema

**Status:** Draft for review · PostgreSQL 16 · EF Core 9 migrations · snake_case naming · UUIDv7 PKs (time-ordered).

Conventions: `created_at/updated_at timestamptz` on all tables (omitted below for brevity); FKs indexed; enums as PostgreSQL enum types or smallint + lookup where extension is expected.

---

## 1. Schema areas

| Schema | Purpose |
|---|---|
| `curriculum` | Static, versioned teaching content (write: authors; read: everyone) |
| `learner` | Per-student state |
| `learning` | Sessions, exercises, attempts (hot path, append-heavy) |
| `assessment` | Observations, estimates, exams |
| `scheduling` | FSRS states, review logs |
| `ai` | Providers, prompt templates, call logs, generation cache |
| `gamification` | XP, badges, goals |
| `analytics` | Rollups (job-written) |
| `identity` | ASP.NET Core Identity tables |

Separation rationale: curriculum is replaceable content (seed packages); learner data is precious and export-scoped; splitting schemas makes backup/export/versioning policies trivial.

## 2. `curriculum` schema

```sql
CREATE TABLE curriculum.curriculum_versions (
  id            uuid PRIMARY KEY,
  language_code text NOT NULL DEFAULT 'de',
  version       text NOT NULL,          -- semver of the content package
  status        text NOT NULL,          -- draft | published | retired
  UNIQUE (language_code, version)
);

CREATE TABLE curriculum.units (            -- thematic unit inside a CEFR level
  id             uuid PRIMARY KEY,
  version_id     uuid NOT NULL REFERENCES curriculum.curriculum_versions,
  cefr_level     text NOT NULL,            -- A1 | A2 | B1 | B2 | C1
  sub_level      smallint NOT NULL,        -- 1 | 2  (A1.1, A1.2 …)
  order_index    int  NOT NULL,
  title          text NOT NULL,
  description    text,
  culture_note   text,                     -- Landeskunde per unit
  UNIQUE (version_id, cefr_level, sub_level, order_index)
);

CREATE TABLE curriculum.concepts (          -- knowledge-graph nodes
  id                  uuid PRIMARY KEY,
  version_id          uuid NOT NULL REFERENCES curriculum.curriculum_versions,
  code                text NOT NULL,        -- stable key: 'gram.dativ.definite-articles'
  kind                text NOT NULL,        -- grammar | function | skill | culture
  cefr_level          text NOT NULL,
  difficulty          smallint NOT NULL CHECK (difficulty BETWEEN 1 AND 10),
  title               text NOT NULL,
  learning_objective  text NOT NULL,        -- "can-do" statement
  explanation_md      text NOT NULL,        -- curated teach content (markdown)
  estimated_minutes   smallint NOT NULL,
  mastery_threshold   real NOT NULL DEFAULT 0.80
                        CHECK (mastery_threshold BETWEEN 0.5 AND 0.95),
  importance          real NOT NULL DEFAULT 1.0,   -- weight in planner/review priority
  unit_id             uuid REFERENCES curriculum.units,
  UNIQUE (version_id, code)
);

CREATE TABLE curriculum.concept_dependencies (
  concept_id      uuid NOT NULL REFERENCES curriculum.concepts,
  prerequisite_id uuid NOT NULL REFERENCES curriculum.concepts,
  dep_type        text NOT NULL DEFAULT 'requires',  -- requires | supports
  min_mastery     real NOT NULL DEFAULT 0.80,
  PRIMARY KEY (concept_id, prerequisite_id),
  CHECK (concept_id <> prerequisite_id)
);
-- Acyclicity enforced in Application layer on write + CI validation of seed data.

CREATE TABLE curriculum.concept_examples (
  id          uuid PRIMARY KEY,
  concept_id  uuid NOT NULL REFERENCES curriculum.concepts,
  german      text NOT NULL,
  english     text NOT NULL,
  note        text,
  order_index int NOT NULL
);

CREATE TABLE curriculum.concept_common_mistakes (
  id          uuid PRIMARY KEY,
  concept_id  uuid NOT NULL REFERENCES curriculum.concepts,
  wrong       text NOT NULL,
  right       text NOT NULL,
  explanation text NOT NULL,
  error_code  text NOT NULL              -- links to error taxonomy (writing eval)
);

CREATE TABLE curriculum.vocab_entries (      -- the lexicon
  id             uuid PRIMARY KEY,
  version_id     uuid NOT NULL REFERENCES curriculum.curriculum_versions,
  lemma          text NOT NULL,
  pos            text NOT NULL,             -- noun | verb | adj | adv | prep | ...
  gender         text,                      -- der | die | das (nouns)
  plural         text,
  ipa            text,
  frequency_rank int,
  cefr_level     text NOT NULL,
  difficulty     smallint NOT NULL DEFAULT 3,
  audio_ref      text,                      -- storage key; TTS-generated lazily
  separable      boolean,                   -- verbs
  irregular_forms jsonb,                    -- {praeteritum, partizip2, praesens3sg}
  UNIQUE (version_id, lemma, pos)
);
CREATE INDEX ix_vocab_freq ON curriculum.vocab_entries (version_id, frequency_rank);
-- Full-text lookup:
CREATE INDEX ix_vocab_lemma_trgm ON curriculum.vocab_entries
  USING gin (lemma gin_trgm_ops);

CREATE TABLE curriculum.word_senses (
  id             uuid PRIMARY KEY,
  vocab_entry_id uuid NOT NULL REFERENCES curriculum.vocab_entries,
  meaning_en     text NOT NULL,
  example_de     text NOT NULL,
  example_en     text NOT NULL,
  topic          text NOT NULL,             -- food | travel | work | ...
  sense_order    smallint NOT NULL DEFAULT 1
);

CREATE TABLE curriculum.concept_required_vocab (
  concept_id     uuid REFERENCES curriculum.concepts,
  vocab_entry_id uuid REFERENCES curriculum.vocab_entries,
  PRIMARY KEY (concept_id, vocab_entry_id)
);

CREATE TABLE curriculum.lesson_templates (
  id           uuid PRIMARY KEY,
  unit_id      uuid NOT NULL REFERENCES curriculum.units,
  order_index  int NOT NULL,
  title        text NOT NULL,
  concept_ids  uuid[] NOT NULL,            -- 1–3 concepts taught together
  phase_config jsonb NOT NULL              -- teach/guided/free/wrap parameters
);

CREATE TABLE curriculum.scenarios (          -- conversation roleplay scripts
  id              uuid PRIMARY KEY,
  version_id      uuid NOT NULL REFERENCES curriculum.curriculum_versions,
  code            text NOT NULL,            -- 'restaurant', 'doctor', ...
  min_cefr        text NOT NULL,
  max_cefr        text NOT NULL,
  title           text NOT NULL,
  setting_md      text NOT NULL,            -- situation description
  ai_role         text NOT NULL,            -- 'waiter', 'doctor', ...
  stages          jsonb NOT NULL,           -- [{goal, hints, required_phrases, exit_check}]
  required_concept_ids uuid[] NOT NULL DEFAULT '{}',
  required_vocab_ids   uuid[] NOT NULL DEFAULT '{}',
  UNIQUE (version_id, code, min_cefr)
);

CREATE TABLE curriculum.reading_texts (
  id            uuid PRIMARY KEY,
  version_id    uuid NOT NULL REFERENCES curriculum.curriculum_versions,
  cefr_level    text NOT NULL,
  topic         text NOT NULL,
  title         text NOT NULL,
  body          text NOT NULL,
  word_count    int NOT NULL,
  source        text NOT NULL,              -- curated | ai_generated(+approved)
  questions     jsonb NOT NULL              -- [{q, options?, answer, type}]
);

CREATE TABLE curriculum.listening_items (
  id            uuid PRIMARY KEY,
  version_id    uuid NOT NULL,
  cefr_level    text NOT NULL,
  transcript    text NOT NULL,
  audio_ref     text,                       -- generated lazily per voice
  questions     jsonb NOT NULL,
  modes         text[] NOT NULL DEFAULT '{comprehension,dictation,shadowing}'
);

CREATE TABLE curriculum.writing_prompts (
  id            uuid PRIMARY KEY,
  version_id    uuid NOT NULL,
  cefr_level    text NOT NULL,
  prompt_md     text NOT NULL,
  min_words     smallint NOT NULL,
  max_words     smallint NOT NULL,
  rubric        jsonb NOT NULL,             -- criteria + descriptors per score
  target_concept_ids uuid[] NOT NULL DEFAULT '{}'
);

CREATE TABLE curriculum.assessment_blueprints (
  id           uuid PRIMARY KEY,
  version_id   uuid NOT NULL,
  kind         text NOT NULL,               -- checkpoint | level_up | placement_bank
  cefr_level   text NOT NULL,
  unit_id      uuid REFERENCES curriculum.units,
  sections     jsonb NOT NULL,              -- [{skill, item_count, item_source, pass_pct}]
  exit_requirements jsonb                   -- level_up: {mastery_coverage, per_skill_min}
);
```

## 3. `learner` schema

```sql
CREATE TABLE learner.profiles (
  id               uuid PRIMARY KEY,        -- = identity user id
  display_name     text NOT NULL,
  ui_language      text NOT NULL DEFAULT 'en',
  target_cefr      text NOT NULL,
  target_date      date,
  minutes_per_day  smallint NOT NULL DEFAULT 30,
  motivation_type  text,                    -- career | exam | relocation | hobby
  preferences      jsonb NOT NULL DEFAULT '{}',  -- practice prefs, voice, a11y
  curriculum_version_id uuid NOT NULL REFERENCES curriculum.curriculum_versions,
  learning_speed   real,                    -- rolling estimate (concepts/hour)
  retention_rate   real                     -- rolling estimate
);

CREATE TABLE learner.cefr_state (
  learner_id    uuid NOT NULL REFERENCES learner.profiles,
  skill         text NOT NULL,     -- overall|reading|writing|listening|speaking|grammar|vocabulary|pronunciation
  cefr_level    text NOT NULL,
  provisional   boolean NOT NULL DEFAULT false,
  confidence    real NOT NULL,
  changed_at    timestamptz NOT NULL,
  PRIMARY KEY (learner_id, skill)
);

CREATE TABLE learner.concept_mastery (
  learner_id    uuid NOT NULL,
  concept_id    uuid NOT NULL REFERENCES curriculum.concepts,
  mastery       real NOT NULL DEFAULT 0 CHECK (mastery BETWEEN 0 AND 1),
  confidence    real NOT NULL DEFAULT 0,
  attempts      int  NOT NULL DEFAULT 0,
  correct       int  NOT NULL DEFAULT 0,
  streak_wrong  smallint NOT NULL DEFAULT 0,
  provisional   boolean NOT NULL DEFAULT false,   -- placement-seeded
  last_practiced_at timestamptz,
  PRIMARY KEY (learner_id, concept_id)
);
CREATE INDEX ix_mastery_frontier ON learner.concept_mastery (learner_id, mastery);

CREATE TABLE learner.word_states (
  learner_id     uuid NOT NULL,
  vocab_entry_id uuid NOT NULL REFERENCES curriculum.vocab_entries,
  status         text NOT NULL DEFAULT 'new',
      -- new | learning | known | mastered | forgotten | relearning
  source         text NOT NULL DEFAULT 'lesson',
  fsrs_stability  real,
  fsrs_difficulty real,
  due_at          timestamptz,
  last_review_at  timestamptz,
  lapse_count     smallint NOT NULL DEFAULT 0,
  leech           boolean NOT NULL DEFAULT false,
  personal_note   text,
  context_sentence text,                    -- where the learner met the word
  PRIMARY KEY (learner_id, vocab_entry_id)
);
CREATE INDEX ix_word_due ON learner.word_states (learner_id, due_at)
  WHERE status IN ('learning','known','relearning');

CREATE TABLE learner.mistakes (              -- append-only
  id          uuid PRIMARY KEY,
  learner_id  uuid NOT NULL,
  skill       text NOT NULL,
  error_code  text NOT NULL,                -- taxonomy: CASE_DAT, ART_GEN, WO_V2 ...
  concept_id  uuid,                         -- mapped concept (nullable = unmapped)
  source_kind text NOT NULL,                -- exercise | writing | conversation | review
  source_id   uuid NOT NULL,
  detail      jsonb                          -- span, wrong, right
);
CREATE INDEX ix_mistakes_recent ON learner.mistakes (learner_id, created_at DESC);
```

## 4. `learning` schema (hot path)

```sql
CREATE TABLE learning.study_sessions (
  id           uuid PRIMARY KEY,
  learner_id   uuid NOT NULL,
  status       text NOT NULL,       -- planned | active | completed | abandoned
  time_budget_min smallint NOT NULL,
  plan         jsonb NOT NULL,      -- SessionPlan snapshot (activities, seeds, ratios)
  planner_version text NOT NULL,    -- reproducibility
  started_at   timestamptz,
  completed_at timestamptz
);

CREATE TABLE learning.activities (
  id           uuid PRIMARY KEY,
  session_id   uuid NOT NULL REFERENCES learning.study_sessions,
  order_index  smallint NOT NULL,
  kind         text NOT NULL,       -- review|lesson|reading|listening|writing|conversation
  ref_id       uuid,                -- lesson_template / reading_text / scenario ...
  status       text NOT NULL DEFAULT 'pending',
  result       jsonb                -- summary metrics per kind
);

CREATE TABLE learning.exercise_instances (
  id           uuid PRIMARY KEY,
  activity_id  uuid NOT NULL REFERENCES learning.activities,
  order_index  smallint NOT NULL,
  type         text NOT NULL,       -- mc|cloze|order|match|translate|transform|error_spot|dictation|free_write|speak
  concept_ids  uuid[] NOT NULL,
  difficulty   smallint NOT NULL,
  spec         jsonb NOT NULL,      -- stem, options, answer key, distractors
  source       text NOT NULL,       -- curated | ai
  gen_meta     jsonb                -- template version, context hash, validation report
);

CREATE TABLE learning.attempts (             -- append-only, partitioned monthly at scale
  id            uuid PRIMARY KEY,
  learner_id    uuid NOT NULL,
  exercise_id   uuid NOT NULL REFERENCES learning.exercise_instances,
  response      jsonb NOT NULL,
  outcome       real NOT NULL CHECK (outcome BETWEEN 0 AND 1),  -- partial credit
  hints_used    smallint NOT NULL DEFAULT 0,
  latency_ms    int,
  evaluated_by  text NOT NULL       -- deterministic | ai(provider/model) | hybrid
);
CREATE INDEX ix_attempts_learner_time ON learning.attempts (learner_id, created_at DESC);

CREATE TABLE learning.reading_sessions (
  id            uuid PRIMARY KEY,
  learner_id    uuid NOT NULL,
  text_id       uuid NOT NULL,
  wpm           real,
  unknown_word_taps int NOT NULL DEFAULT 0,
  comprehension real,               -- 0..1
  detail        jsonb
);

CREATE TABLE learning.listening_sessions (
  id            uuid PRIMARY KEY,
  learner_id    uuid NOT NULL,
  item_id       uuid NOT NULL,
  mode          text NOT NULL,      -- comprehension | dictation | shadowing
  replays       smallint NOT NULL DEFAULT 0,
  playback_speed real NOT NULL DEFAULT 1.0,
  accuracy      real,
  detail        jsonb
);

CREATE TABLE learning.writing_submissions (
  id            uuid PRIMARY KEY,
  learner_id    uuid NOT NULL,
  prompt_id     uuid,
  text          text NOT NULL,
  word_count    int NOT NULL,
  rubric_scores jsonb,              -- {grammar:4, vocabulary:3, structure:4, task:5, naturalness:3}
  annotations   jsonb,              -- [{span:[s,e], error_code, concept_id, correction, explanation}]
  rewrite       text,               -- model version at learner level
  eval_meta     jsonb               -- provider, model, template version, checks
);

CREATE TABLE learning.speaking_attempts (
  id            uuid PRIMARY KEY,
  learner_id    uuid NOT NULL,
  prompt_ref    jsonb NOT NULL,
  audio_ref     text,
  transcript    text,
  stt_confidence real,
  word_scores   jsonb,              -- [{word, score}]
  grammar_feedback jsonb,
  eval_meta     jsonb
);

CREATE TABLE learning.conversation_sessions (
  id            uuid PRIMARY KEY,
  learner_id    uuid NOT NULL,
  scenario_id   uuid NOT NULL,
  stage_index   smallint NOT NULL DEFAULT 0,
  constraints   jsonb NOT NULL,     -- ContextPack snapshot for reproducibility
  status        text NOT NULL,
  feedback      jsonb               -- final report
);

CREATE TABLE learning.conversation_turns (
  id            uuid PRIMARY KEY,
  session_id    uuid NOT NULL REFERENCES learning.conversation_sessions,
  turn_index    smallint NOT NULL,
  speaker       text NOT NULL,      -- learner | ai
  text          text NOT NULL,
  audio_ref     text,
  violations    jsonb               -- constraint violations found in AI output (pre-regen)
);
```

## 5. `assessment` schema

```sql
CREATE TABLE assessment.observations (       -- append-only stream
  id          uuid PRIMARY KEY,
  learner_id  uuid NOT NULL,
  skill       text NOT NULL,
  concept_id  uuid,
  difficulty  real NOT NULL,        -- item difficulty on theta scale
  outcome     real NOT NULL,        -- 0..1
  weight      real NOT NULL DEFAULT 1.0,   -- down-weighted if AI-scored & flagged
  source_kind text NOT NULL,
  source_id   uuid NOT NULL
);
CREATE INDEX ix_obs_learner ON assessment.observations (learner_id, skill, created_at);

CREATE TABLE assessment.skill_estimates (
  learner_id   uuid NOT NULL,
  skill        text NOT NULL,
  theta        real NOT NULL,
  std_error    real NOT NULL,
  observation_count int NOT NULL DEFAULT 0,
  observations_since_level_change int NOT NULL DEFAULT 0,
  PRIMARY KEY (learner_id, skill)
);

CREATE TABLE assessment.estimate_snapshots (  -- for trend charts
  learner_id uuid NOT NULL,
  skill      text NOT NULL,
  theta      real NOT NULL,
  std_error  real NOT NULL,
  taken_at   date NOT NULL,
  PRIMARY KEY (learner_id, skill, taken_at)
);

CREATE TABLE assessment.exam_sessions (
  id          uuid PRIMARY KEY,
  learner_id  uuid NOT NULL,
  blueprint_id uuid NOT NULL REFERENCES curriculum.assessment_blueprints,
  kind        text NOT NULL,        -- placement | checkpoint | level_up
  status      text NOT NULL,
  sections    jsonb NOT NULL,       -- per-section items, responses, scores
  result      jsonb                 -- pass/fail, per-skill scores, gap report
);
```

## 6. `scheduling` schema

```sql
CREATE TABLE scheduling.review_items (
  id           uuid PRIMARY KEY,
  learner_id   uuid NOT NULL,
  item_kind    text NOT NULL,       -- word | concept_drill
  item_ref     uuid NOT NULL,       -- vocab_entry_id | concept_id
  fsrs_stability  real NOT NULL,
  fsrs_difficulty real NOT NULL,
  due_at       timestamptz NOT NULL,
  priority     real NOT NULL DEFAULT 0,   -- recomputed by job/query
  postponed_count smallint NOT NULL DEFAULT 0,
  UNIQUE (learner_id, item_kind, item_ref)
);
CREATE INDEX ix_review_due ON scheduling.review_items (learner_id, due_at, priority DESC);

CREATE TABLE scheduling.review_logs (        -- append-only; FSRS optimizer input
  id           uuid PRIMARY KEY,
  review_item_id uuid NOT NULL,
  learner_id   uuid NOT NULL,
  rating       smallint NOT NULL,   -- 1 Again | 2 Hard | 3 Good | 4 Easy
  elapsed_days real NOT NULL,
  scheduled_days real NOT NULL,
  reviewed_at  timestamptz NOT NULL
);
```

## 7. `ai` schema

```sql
CREATE TABLE ai.providers (
  id           uuid PRIMARY KEY,
  name         text NOT NULL,       -- user label: "Local LM Studio"
  kind         text NOT NULL,       -- lmstudio|ollama|openai|anthropic|gemini|azure_openai|openrouter
  base_url     text,
  api_key_enc  bytea,               -- encrypted, nullable for local
  default_model text NOT NULL,
  model_size_class text NOT NULL,   -- s3b | s7b | s14b | cloud
  enabled      boolean NOT NULL DEFAULT true,
  capabilities jsonb                -- probe results: json_mode, german_quality, max_ctx
);

CREATE TABLE ai.task_routes (
  task_type    text PRIMARY KEY,    -- explain|gen_exercise|gen_dialogue|eval_writing|...
  provider_chain uuid[] NOT NULL,   -- ordered fallback
  max_tokens   int NOT NULL,
  temperature  real NOT NULL
);

CREATE TABLE ai.prompt_templates (
  id           uuid PRIMARY KEY,
  task_type    text NOT NULL,
  size_class   text NOT NULL,       -- s3b | s7b | s14b | cloud (smaller = terser template)
  version      int  NOT NULL,
  system_tmpl  text NOT NULL,
  user_tmpl    text NOT NULL,       -- with {{placeholders}}
  output_schema jsonb NOT NULL,     -- JSON Schema the response must satisfy
  active       boolean NOT NULL DEFAULT false,
  UNIQUE (task_type, size_class, version)
);

CREATE TABLE ai.call_logs (                  -- observability; partitioned monthly
  id           uuid PRIMARY KEY,
  task_type    text NOT NULL,
  provider_id  uuid,
  model        text,
  template_id  uuid,
  latency_ms   int,
  prompt_tokens int, completion_tokens int,
  status       text NOT NULL,       -- ok | schema_fail | constraint_fail | provider_error
  retry_count  smallint NOT NULL DEFAULT 0,
  learner_id   uuid,
  correlation_id uuid
);

CREATE TABLE ai.generation_cache (
  cache_key    text PRIMARY KEY,    -- hash(task_type, template_version, context_pack)
  task_type    text NOT NULL,
  payload      jsonb NOT NULL,      -- validated output
  hits         int NOT NULL DEFAULT 0,
  expires_at   timestamptz
);
```

## 8. `gamification` & `analytics` schemas

```sql
CREATE TABLE gamification.states (
  learner_id    uuid PRIMARY KEY,
  xp            int NOT NULL DEFAULT 0,
  level         int NOT NULL DEFAULT 1,
  streak_days   int NOT NULL DEFAULT 0,
  longest_streak int NOT NULL DEFAULT 0,
  freeze_tokens smallint NOT NULL DEFAULT 0,
  last_active_date date
);

CREATE TABLE gamification.badges (
  code        text PRIMARY KEY,     -- 'first_100_words', 'streak_30', ...
  title       text NOT NULL,
  description text NOT NULL,
  rule        jsonb NOT NULL        -- rule-engine predicate over analytics facts
);

CREATE TABLE gamification.badge_awards (
  learner_id uuid NOT NULL,
  badge_code text NOT NULL REFERENCES gamification.badges,
  awarded_at timestamptz NOT NULL,
  PRIMARY KEY (learner_id, badge_code)
);

CREATE TABLE gamification.goal_periods (
  id          uuid PRIMARY KEY,
  learner_id  uuid NOT NULL,
  period      text NOT NULL,        -- daily | weekly
  start_date  date NOT NULL,
  target      jsonb NOT NULL,       -- {xp: 50, reviews: 30, minutes: 30}
  progress    jsonb NOT NULL DEFAULT '{}',
  achieved    boolean
);

CREATE TABLE analytics.daily_rollups (
  learner_id   uuid NOT NULL,
  day          date NOT NULL,
  xp           int NOT NULL DEFAULT 0,
  minutes      real NOT NULL DEFAULT 0,
  reviews_done int NOT NULL DEFAULT 0,
  reviews_due  int NOT NULL DEFAULT 0,
  new_words    int NOT NULL DEFAULT 0,
  words_known  int NOT NULL DEFAULT 0,    -- running total snapshot
  words_mastered int NOT NULL DEFAULT 0,
  mastery_delta jsonb,                    -- {concept_id: delta} compressed
  skill_thetas  jsonb,                    -- snapshot of 7 skills
  wpm           real,
  listening_accuracy real,
  writing_avg_rubric real,
  speaking_fluency  real,
  PRIMARY KEY (learner_id, day)
);
```

## 9. Design decisions & trade-offs

| Decision | Alternative | Rationale |
|---|---|---|
| **jsonb for exercise `spec`, session `plan`, exam `sections`** | Fully normalized child tables | These are write-once snapshots read as documents; schema evolves fast per exercise type; jsonb + CHECKed envelope keeps velocity without losing queryability (GIN indexes where needed). Learner *state* (mastery, FSRS) stays fully relational — it's queried analytically. |
| **UUIDv7 PKs** | bigserial | Time-ordered → index-friendly; safe for future sync/multi-device merge (offline-first). |
| **Separate `observations` stream + computed `skill_estimates`** | Update estimates in place only | Stream = recomputability (NFR-REL-1) and algorithm evolution; estimates table = O(1) reads. |
| **`concept_dependencies` adjacency list + recursive CTE** | Closure table / ltree / graph DB | ~500 nodes, depth ≤ 12: recursive CTEs are millisecond-fast; closure table adds write complexity for no measurable gain. Revisit if node count × learners makes frontier queries hot (then: materialize per-learner frontier — already planned as cache). |
| **Curriculum versioning via `version_id` + stable `code`** | In-place edits | Learner state references concepts by id; version migration maps old→new via `code`; enables content packages and rollback. |
| **Monthly partitioning deferred** (attempts, call_logs) | Partition from day 1 | Single-user local installs never need it; DDL prepared, activated by ops flag at scale (NFR-SCA-3). |
| **Audio as storage refs, generated lazily** | Pre-generate all audio | TTS voice choice is per-user; lazy generation + cache keeps seed packages small and offline-friendly. |

## 10. Integrity beyond SQL

Enforced in Application layer with dedicated tests (documented here because reviewers will look for missing FK/CHECKs):
- Knowledge-graph acyclicity (on authoring writes + seed CI check).
- `exercise_instances.spec` validated against per-type JSON Schema before insert.
- `word_states.status` transitions via state machine, not free updates.
- Per-user isolation via EF global query filters (`learner_id = current user`) + integration tests (NFR-SEC-3).
