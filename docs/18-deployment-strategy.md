# 18 — Deployment Strategy

**Status:** Draft for review · Two deployment modes from one codebase: **Local-First** (primary) and **Server** (multi-user, later).

---

## 1. Deployment modes

| | Local-First (primary) | Server mode |
|---|---|---|
| Users | 1 (or household) | 10 k+ concurrent |
| AI | LM Studio / Ollama on host; optional cloud keys | Cloud providers + shared local inference pool |
| Cache | In-memory (`ICache` in-proc impl) | Redis |
| Auth | Auto-issued device token | Identity + JWT, refresh rotation |
| Telemetry | Opt-in only | Standard, GDPR-compliant |
| Delivery | Docker Compose or native installer | Kubernetes (deploy/k8s) |

Same binaries, mode chosen by configuration — no code forks (NFR-OFF-3, NFR-SCA-1).

## 2. Local-first deployment

```yaml
# deploy/docker-compose.yml (profiles)
services:
  lif:            # ASP.NET Core (serves API + built SPA)
    image: ghcr.io/lif/lif:latest
    ports: ["8080:8080"]
    environment:
      - LIF_MODE=local
      - ConnectionStrings__Db=Host=db;Database=lif;...
      - Ai__Autodetect=true        # scans localhost:1234 (LM Studio), :11434 (Ollama)
    volumes: [lif-audio:/data/audio, lif-keys:/data/keys]
  db:
    image: postgres:16
    volumes: [lif-db:/var/lib/postgresql/data]
  redis:          # profile: server-ish local; omitted in minimal profile
    image: redis:7
    profiles: ["full"]
  whisper:        # optional local STT
    image: ghcr.io/lif/whisper-server:latest
    profiles: ["speech"]
  piper:          # optional local TTS
    image: ghcr.io/lif/piper-server:latest
    profiles: ["speech"]
```

- `docker compose up` → minimal profile (app + Postgres). `--profile speech` adds STT/TTS. LLM stays on the host (GPU access, user's own LM Studio) — the app connects to `host.docker.internal`.
- **Native option (later):** single-file publish + windows service/systemd; Postgres via embedded distribution or existing instance — evaluated in Phase 5 based on beta feedback (Docker is the supported path first; smallest support matrix).
- **Migrations:** app runs EF migrations + seed-package sync on startup (local mode only; server mode uses migration jobs).
- **Backup:** built-in scheduled `pg_dump` to user folder + one-click restore; profile JSON export as belt-and-braces.

## 3. Server mode (Phase 5+)

- **Topology:** N stateless API pods behind ingress; Postgres (managed/HA); Redis; Hangfire workers as separate deployment (same image, `LIF_ROLE=worker`); object storage for audio.
- **AI pool:** cloud providers via routing table; optional vLLM/TGI inference deployment for shared local models with per-tenant rate limits; the same `IAiProvider` abstraction covers it (OpenAI-compatible endpoint).
- **Scaling:** HPA on CPU + custom metric (AI queue depth). Backpressure per NFR-SCA-2.
- **Migrations:** `migrate` Job gated in the release pipeline before rollout; forward-only.
- **Zero-downtime:** rolling deploys; SSE connections drain with `Retry-After` reconnect hints.

## 4. Release engineering

- **Versioning:** SemVer app; independent SemVer for curriculum seed packages; prompt template versions ride app releases but activate via data flag.
- **Pipelines (GitHub Actions):**
  - `ci.yml`: build → unit/arch/persistence/contract tests → seed validation → frontend build/test → Docker image (SBOM + Trivy scan).
  - `release.yml`: tag → multi-arch images (amd64/arm64) → GHCR push → compose bundle artifact → draft release notes.
  - nightly: simulation harness (200 students), k6 perf, live AI bench (non-gating).
- **Channels:** `stable` and `beta` image tags; feature flags decouple deploy from release (FR-SET-4).
- **Config:** all via environment/appsettings layering; secrets never in images; local keys encrypted at rest (DPAPI/keychain), server keys via K8s secrets/vault.

## 5. Observability in production

- OpenTelemetry traces (request → AI call → DB) exported to console/file locally, OTLP in server mode.
- Metrics dashboards: AI validity & latency per provider/task, planner rationale distribution, review completion, error rates.
- Crash + error reporting: local = log bundle exporter ("share diagnostics" button); server = Sentry-compatible sink.
- SLOs (server): 99.5 % availability API; AI-task success (incl. fallback) ≥ 99 %.

## 6. Update & data-safety policy

1. App update never blocks on content update; seed sync is transactional and rollback-safe.
2. Pre-migration automatic backup in local mode.
3. Learner export format is versioned; imports accept N-2 versions.
4. Curriculum version pinning: learners finish an active session on their pinned version; upgrade happens at session boundaries with state migration by concept `code` (ADR-014).

---

# Review Gate

This completes deliverables 1–18. Per the implementation strategy, **no production code is written until this set is reviewed and approved**. Approval flips ADRs to *Accepted* and unlocks Phase 0 of `16-development-roadmap.md`.
