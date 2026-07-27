# Curriculum Seed Packages — German (de)

Versioned content packages loaded by the seeding pipeline into `curriculum_versions` (ADR-014).

Layout (Phase 1 ships the A1.1 walking-skeleton slice: 2 units, 15 concepts, 41 curated
exercises, ~127 lexicon entries, 1 checkpoint blueprint):

```
de/
├─ manifest.json           # package name, semver, file map — the validator's entry point
├─ a1/ …                   # concepts, units, exercises, blueprints per level
└─ lexicon/                # vocabulary entries + senses
```

Rules:
- Published versions are immutable; learner state migrates by stable concept `code`.
- Noun references in `requiredVocabulary` carry their article ("der Tisch"); the lexicon
  stores the bare lemma plus a `gender` field.
- Every package is validated in CI: DAG acyclicity, CEFR consistency, error-code references,
  lesson/exercise coverage, answer-key encodings:

```
dotnet run --project tools/lif-cli -- seed-validate seeds/curriculum/de
```
