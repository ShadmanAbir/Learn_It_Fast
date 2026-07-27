# 13 — UI/UX Wireframes

**Status:** Draft for review · Low-fi ASCII wireframes + interaction notes. Design system: Tailwind + Radix primitives; tokens for theming; WCAG 2.2 AA.

---

## 1. Information architecture

```
├─ Today (home / session hub)          ← default screen
├─ Learn
│   ├─ Curriculum map (graph view)
│   ├─ Grammar library
│   └─ Scenarios (conversation)
├─ Review (queue + forecast)
├─ Vocabulary (my words, stats)
├─ Progress (dashboards, CEFR, weaknesses)
├─ Profile (goals, streak, badges)
└─ Settings (AI providers, audio, a11y, data)
```

Primary navigation: left rail (desktop) / bottom tabs (mobile): **Today · Learn · Review · Progress · Profile**.

## 2. Today (session hub)

```
┌────────────────────────────────────────────────────────────┐
│ LIF        Streak 🔥 17   XP ▮▮▮▮▯ 42/50        A2.1 ▲    │
├────────────────────────────────────────────────────────────┤
│  Guten Morgen, Amir!                                       │
│  Goal: B1 by March — ● on track                            │
│                                                            │
│  ┌──────────────────────────────────────────────────┐      │
│  │  TODAY'S SESSION                     ~30 min     │      │
│  │  ├ 🔁 Reviews (24 due)                 10 min    │      │
│  │  ├ 📖 Lesson: Dative articles          12 min    │      │
│  │  └ 💬 Beim Arzt (roleplay)              8 min    │      │
│  │  Why this? ▸ (planner rationale)                 │      │
│  │            [ ▶ Start session ]                   │      │
│  │  Short on time?  [10 min] [20 min]               │      │
│  └──────────────────────────────────────────────────┘      │
│                                                            │
│  Review forecast ▁▂▅▃▂▁▂    Weekly goal ▮▮▮▮▮▯▯ 5/7       │
└────────────────────────────────────────────────────────────┘
```

Notes: single dominant CTA; time-budget switcher re-plans instantly (deterministic, <500 ms); "Why this?" expands the planner rationale — the teacher explains its choices.

## 3. Lesson player

```
┌────────────────────────────────────────────────────────────┐
│ ← Exit    Dative articles          ●●●○○○○○  step 3/8      │
├────────────────────────────────────────────────────────────┤
│  TEACH (step 1)         GUIDED PRACTICE (this step)        │
│                                                            │
│   Fill the gap:                                            │
│   „Ich gehe mit ____ Freundin ins Kino."                   │
│                                                            │
│      [ der ]   [ dem ]   [ den ]   [ die ]                 │
│                                                            │
│   💡 Hint (1/3)                        [ Check ]           │
├────────────────────────────────────────────────────────────┤
│  ✅ Richtig! · „mit" always takes dative; die Freundin →   │
│     der Freundin.        [Explain more ▸]  [Continue →]    │
└────────────────────────────────────────────────────────────┘
```

Notes: feedback panel slides in ≤ 2 s, is keyboard-actionable (Enter = continue), always names the rule; "Explain more" streams an AI explanation grounded to this concept (SSE). Wrong answers: shake-free (a11y), red-free-reliance-free (color + icon + text).

## 4. Conversation (roleplay)

```
┌────────────────────────────────────────────────────────────┐
│ ← End & get feedback     Beim Arzt 🩺      Stage 2/4:      │
│                                    "Describe your symptoms"│
├────────────────────────────────────────────────────────────┤
│  🧑‍⚕️ Guten Tag! Was fehlt Ihnen denn?                      │
│                                🔊 replay  🐢 slow  „show EN"│
│                                                            │
│                       Ich habe seit gestern Kopfschmerzen. │
│                                            ✓ sent          │
│  🧑‍⚕️ Seit gestern? Haben Sie auch Fieber?  ▌(streaming)    │
├────────────────────────────────────────────────────────────┤
│  🎤 Hold to speak      [type instead…]     ❓ phrase help   │
│  Task list: ☑ greet  ☐ symptoms  ☐ duration  ☐ thank       │
└────────────────────────────────────────────────────────────┘
```

Notes: stage goals visible as checklist (app-controlled progression); mic-first but text always available; "phrase help" opens scenario phrasebook (curated) — not an AI freebie that solves the task.

## 5. Review queue

```
┌────────────────────────────────────────────────────────────┐
│ Review          24 due · ~10 min          [flame streak]   │
├────────────────────────────────────────────────────────────┤
│                                                            │
│                     die Rechnung                           │
│                    ⟨what does it mean?⟩                    │
│                                                            │
│                  [ show answer  ␣ ]                        │
│  ──────────────────────────────────────────                │
│  the bill / invoice · „Die Rechnung, bitte!"  🔊           │
│                                                            │
│  [1 Again]   [2 Hard]   [3 Good]   [4 Easy]                │
│    <1d         3d          8d         15d                  │
└────────────────────────────────────────────────────────────┘
```

Notes: Anki-grade keyboard flow (space/1–4); interval preview under buttons; card types rotate (recognition/production/audio/cloze); grammar micro-drills interleaved with words.

## 6. Writing studio

```
┌────────────────────────────────────────────────────────────┐
│ Writing: "Schreiben Sie eine E-Mail an Ihren Vermieter…"   │
│ Target: 60–80 words · A2 · uses: Dativ, modal verbs        │
├──────────────────────────┬─────────────────────────────────┤
│  [textarea]              │  FEEDBACK (after submit)        │
│  Sehr geehrter Herr      │  Grammar     ▮▮▮▮▯ 4/5          │
│  Müller, ich schreibe    │  Vocabulary  ▮▮▮▯▯ 3/5          │
│  Ihnen weil die Heizung  │  Structure   ▮▮▮▮▯ 4/5          │
│  kaputt ist…             │  ⚠ „weil die Heizung kaputt     │
│                          │    ist" – comma before „weil"   │
│  words: 64 ✓             │    [WO_SUB → see concept]       │
│  [Submit for feedback]   │  ✨ Natural version ▸ (diff)     │
└──────────────────────────┴─────────────────────────────────┘
```

Notes: inline error highlights with hover cards; every error links to its concept page; model rewrite shown as a diff, not a replacement (learner compares).

## 7. Progress dashboard

```
┌────────────────────────────────────────────────────────────┐
│ Progress                        range: [7d] [30d] [90d]    │
├─────────────────┬──────────────────┬───────────────────────┤
│ Estimated CEFR  │ Vocabulary       │ Grammar mastery       │
│   A2.1 ●───○ B1 │  1,240 known ↗   │  [heatmap by unit]    │
│ R ▮▮▮ W ▮▮ L ▮▮▮│  +38 this week   │  weak: Dativ prep.    │
│ S ▮▮ G ▮▮▮ V ▮▮▮│  [growth chart]  │        adj. endings   │
├─────────────────┴──────────────────┴───────────────────────┤
│ XP & streak calendar [garden-style heatmap]                │
│ Reviews: 92 % completion · Reading 118 WPM ↗ · Listen 81 % │
│ ⚡ Weakness report: 1. Dativ prepositions → [practice now] │
└────────────────────────────────────────────────────────────┘
```

"Practice now" enqueues a planner request — even shortcuts route through the teacher.

## 8. Curriculum map (graph view)

Zoomable DAG (levels as horizontal bands, A1 bottom → C1 top). Node states: 🟢 mastered · 🟡 learning · ⚪ unlocked · 🔒 locked (with "why locked" = missing prerequisites listed). Click → concept page (objective, examples, my history, [Practice] via planner).

## 9. Settings — AI providers

```
│ AI Providers                                               │
│ ┌ Local — LM Studio  http://localhost:1234  ● online      │
│ │  Model: llama-3.1-8b-instruct   size: 8B                │
│ │  Fitness: convo ✅  exercises ✅  writing-eval ⚠ reduced  │
│ ├ OpenRouter  ● configured (key ●●●●)   [probe]           │
│ └ [+ add provider]                                        │
│ Task routing:  conversation → Local · writing eval →      │
│                OpenRouter (fallback Local-reduced)  [edit] │
```

Plain-language fitness verdicts from the capability probe; warnings explain consequences ("writing feedback will be less detailed"), never technobabble.

## 10. Key UX principles applied

| Principle | Where |
|---|---|
| One decision per screen | Today = single CTA; lesson steps atomic |
| Teacher transparency | Planner rationale, "why locked", interval previews |
| Never punish, always explain | Error feedback names the rule + links concept |
| Latency honesty | Streaming text, skeletons ≤ 300 ms, degraded-mode banners |
| Keyboard-first | Full flows operable without mouse (review: space/1–4) |
| Mobile parity | All core-loop screens single-column capable (360 px) |

**Deliverable note:** these low-fi frames are the contract for the React implementation; hi-fi Figma work happens in Phase 1 alongside the design-token setup.
