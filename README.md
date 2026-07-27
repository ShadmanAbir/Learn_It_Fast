# LIF — Lernen ist Freiheit

Adaptive AI-powered German tutor (A1 → C1). **The application is the teacher; the AI model is only one tool used by the teacher.**

- Architecture & design package: [`docs/`](docs/README.md) (18 deliverables, approved — ADRs *Accepted*)
- Roadmap: [`docs/16-development-roadmap.md`](docs/16-development-roadmap.md) — currently in **Phase 0** (foundations)

## Repository layout

| Path | Contents |
|---|---|
| `src/backend/` | ASP.NET Core 9 modular monolith, Clean Architecture (`Lif.sln`, 13 projects) |
| `src/frontend/` | React 19 + Vite + TypeScript SPA (TanStack Query, Zustand) |
| `seeds/` | Versioned curriculum content + prompt template sources |
| `deploy/` | Dockerfile, docker compose (local-first), k8s later |
| `tools/` | Operator CLI (Phase 1+) |
| `docs/` | Full architecture & design documentation |

## Quick start

```bash
# Full local stack (app + Postgres)
cd deploy && docker compose up
# → http://localhost:8080  ·  readiness: /health/ready

# Development loop
cd deploy && docker compose -f docker-compose.dev.yml up -d   # Postgres on :5432
cd src/backend && dotnet run --project Lif.Api                # API on :8080
cd src/frontend && npm run dev                                # Vite dev server, proxies /api + /health
```

## Tests

```bash
cd src/backend && dotnet test          # unit + architecture + integration
cd src/frontend && npm run lint && npm test
```

Architecture rules (`docs/14-project-structure.md` §2) are enforced by `Lif.Architecture.Tests` — the build fails on layer violations.
