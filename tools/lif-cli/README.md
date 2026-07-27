# lif-cli

Operator tooling (doc 14 §3): `seed-validate` (shipped), `ai-bench`, `recompute-mastery` (later phases).

Standalone net9.0 console — no domain reference, no NuGet dependencies, so CI can `dotnet run` it cheaply.

## seed-validate

```
dotnet run --project tools/lif-cli -- seed-validate seeds/curriculum/de
```

Validates a curriculum seed package (doc 18 §4). Exit 0 = valid, 1 = violations (all listed), 2 = usage error. Checks:

- manifest completeness — every declared file exists, semver `packageVersion`, known categories only
- concept dependency graph is a DAG; `requires` edges carry `minMastery`, `supports` edges don't
- CEFR consistency: concept ↔ unit ↔ blueprint levels, valid sub-level strings (`A1.1` … `C1.2`)
- cross-references: lessons ↔ concepts, exercises ↔ concepts, blueprints ↔ units
- coverage: every concept appears in a lesson template and has at least one curated exercise
- exercise answer keys per type (MC option membership, cloze `___` gap, `left=right|…` pairs, ordering token multiset + shuffled options)
- exercise `errorCode`s must be declared by a referenced concept's `commonMistakes` (taxonomy discipline)
- lexicon: unique (lemma, pos), noun gender `der|die|das`, `separable` only on verbs, senses present
- `requiredVocabulary` resolution, including the `"der Tisch"` → lemma `Tisch` + gender convention

`testdata/invalid-package/` is a negative fixture; CI asserts the validator rejects it.
