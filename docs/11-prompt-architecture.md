# 11 — Prompt Architecture

**Status:** Draft for review · The Prompt Engine: versioned, size-class-aware, contract-tested templates.

---

## 1. Principles

1. **Prompts are artifacts, not string literals.** Stored in `ai.prompt_templates`, versioned, activated explicitly, seeded from the repo (`seeds/prompts/`), diffable in PRs.
2. **Every template has an output contract** — a JSON Schema (or plain-text rules for streamed conversation) that the OutputValidator enforces.
3. **Size classes:** each task ships `s3b`, `s7b`, `s14b`, `cloud` variants. Smaller class ⇒ terser instructions, more rigid skeleton, fewer degrees of freedom.
4. **The model receives constraints, not authority.** Templates phrase the student context as *rules* ("Use ONLY these grammar structures: …"), never as open questions ("what should the student learn?").
5. **English meta-language, German content.** Instructions in English (small models follow English instructions more reliably); learner-facing content in German with English glosses as required by the task.

## 2. Prompt Engine pipeline

```
PromptRequest(taskType, ContextPack)
   → TemplateStore.GetActive(taskType, provider.sizeClass)
   → PlaceholderBinder.Bind(template, contextPack)     // strict: unknown/missing key → throw
   → BudgetTrimmer.Trim(prompt, maxTokens)             // deterministic trim order:
                                                       // vocab hints → examples → notes; never trims rules
   → RenderedPrompt {system, user, outputSchema, temperature, maxTokens}
```

`BudgetTrimmer` guarantees FR-AI-6: rules and output schema survive; optional context shrinks first. Trims are logged so quality regressions are traceable to context starvation.

## 3. Template anatomy

```
[system]
You are a German language exercise generator. Follow ALL rules exactly.

[user]
TASK: Create {{count}} cloze exercises.
TARGET CONCEPT: {{concept_title}} — {{learning_objective}}
STUDENT LEVEL: {{cefr}}
RULES:
- Use ONLY these grammar structures: {{allowed_grammar}}
- Do NOT use: {{forbidden_grammar}}
- Prefer these words: {{vocab_hints}}
- One blank per sentence, blank tests the target concept.
- Sentences: {{max_words_per_sentence}} words maximum.
EXAMPLE (style anchor, from curriculum):
{{curated_example}}
OUTPUT: JSON only, matching this schema exactly:
{{output_schema_skeleton}}
```

Placeholders come **only** from the ContextPack (typed binder — no free interpolation). The skeleton for small models is pre-opened JSON the model completes.

## 4. Template catalog (v1)

| Task | Template family | Output contract (schema id) |
|---|---|---|
| explain | `explain.concept`, `explain.mistake` | `explanation.v1` (markdown, ≤ 200 words, must include 2 examples with gloss) |
| gen_examples | `examples.concept` | `examples.v1` (list of {de, en, note}) |
| gen_exercise | `exercise.cloze`, `exercise.mc`, `exercise.transform`, `exercise.error_spot`, `exercise.translate` | per-type `exercise.<type>.v1` |
| gen_dialogue | `dialogue.scenario` | `dialogue.v2` (turns[], each ≤ N sentences) |
| gen_reading | `reading.passage` | `reading.v1` (title, body, questions[]) |
| gen_quiz | `quiz.concept_batch` | `quiz.v1` |
| conversation_turn | `conversation.roleplay.turn` | text rules: ≤ {{max_sentences}} sentences, stay in role, level ceiling |
| eval_answer | `eval.free_answer` | `verdict.v1` ({correct: 0..1, error_codes[], brief_reason}) |
| eval_writing | `eval.writing.full` (s14b/cloud), `eval.writing.detect` (s3b/s7b reduced-claim) | `writing_eval.v2` / `writing_detect.v1` |
| eval_speaking | `eval.transcript` | `speaking_eval.v1` |
| translate | `translate.de_en`, `translate.en_de` | `translation.v1` |
| encourage | `encourage.session_end`, `encourage.struggle` | `message.v1` (≤ 2 sentences, no advice about what to study) |

Note the deliberate design of `encourage.*`: even motivational messages are forbidden from suggesting study directions — that's the planner's voice, not the model's.

## 5. Error taxonomy (shared contract)

Writing/speaking evaluation and curriculum common-mistakes share one closed error-code set (~60 codes), e.g.:

```
ART_GEN   wrong gender article        CASE_AKK  wrong accusative form
CASE_DAT  wrong dative form           WO_V2     verb-second violation
WO_BRACKET Satzklammer violation      WO_SUB    subclause verb-final violation
ADJ_END   adjective ending            VERB_CONJ conjugation error
VERB_PART wrong participle            PREP_CASE preposition governs wrong case
PLURAL    wrong plural form           SPELL     spelling
LEX_CHOICE unnatural word choice      REG       register mismatch
```

Why closed: (a) small models classify reliably into a fixed list, (b) each code maps to concept(s) so mistakes update the knowledge graph deterministically, (c) analytics aggregate cleanly. Unknown model output codes → `UNMAPPED` (logged, curated later).

## 6. Validation & repair loop

```
raw output
 → JSON parse            fail → repair prompt: "Your last output was invalid: <error>. Output ONLY corrected JSON."
 → JSON Schema check     fail → repair prompt (≤ 2 total repairs)
 → ConstraintChecker:
     vocabulary scan     lemmatized tokens ∉ (allowed ∪ A1-core function words) → violation
     grammar heuristics  forbidden-structure detectors (regex/pattern per structure)
     length/count checks
 → violations → regenerate once with violations listed → still failing → curated fallback
```

Constraint checking is deterministic .NET code (lemma lookup against lexicon + pattern detectors), not another AI call — an AI validating an AI would compound unreliability at small scales.

## 7. Prompt QA

- **Golden contract tests** (NFR-TST-3): per template × size class, recorded model outputs are replayed in CI; schema-validity and constraint-pass rates gate template activation.
- **Live benchmark command:** `lif ai-bench --provider X` runs the template suite against a configured provider (used by the setup wizard's capability probe).
- **A/B by version:** `prompt_templates.version` + call logs allow measuring validity rate per version before flipping `active`.

## 8. Anti-patterns explicitly banned (review checklist)

- ❌ Sending mastery tables, review queues, or study history to the model.
- ❌ Asking the model to pick topics, decide difficulty, or sequence content.
- ❌ Unbounded conversation history growth.
- ❌ Prompt string concatenation in engine code (must go through PromptEngine).
- ❌ Parsing model output with regex "good enough" hacks instead of schema validation.
