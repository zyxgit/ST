# ST Monorepo — Agent Instructions

This file is the repository-level instruction entry for Codex-style coding agents. It applies to the whole repository.

## Must-read documents before coding

1. `docs/ai/AI-RULES.md` — mandatory global rules.
2. `docs/ai/common/AgentExecutionGuide.md` — how to split roadmap work into AI-executable tasks.
3. `docs/ai/common/DevelopmentRoadmap.md` — phased roadmap for high-concurrency and cross-service transaction features.
4. Task-specific docs under `docs/ai/api/`, `docs/ai/web/`, and `docs/ai/skills/`.

## Execution discipline

- Do not implement an entire roadmap phase in one prompt. Split work into small tasks with clear write scope, acceptance criteria, and tests.
- Before editing, inspect existing code with `rg` and follow current project patterns.
- For any feature change, update documentation in the same change set according to `docs/ai/common/DocumentationSync.md`.
- For new services, wire Aspire AppHost, Gateway routing, Docker Compose, configuration, migrations, and docs in the same task or in explicitly sequenced subtasks.
- Do not commit secrets, production connection strings, JWT signing keys, or local-only environment files.

## Validation baseline

Use the narrowest meaningful checks for the changed area. Prefer:

- Backend: `dotnet build Api/src/ST.slnx`
- Frontend: `cd Web && pnpm build`
- Documentation-only: `git diff --check`

If a check cannot run because of environment limitations, document the limitation in the final response.
