# ST Monorepo — Agent Instructions

This file applies to the whole repository.

## Must-read documents before coding

1. `docs/ai/README.md` — mandatory AI execution rules, demand review, task splitting, uncertainty handling, documentation sync.
2. `docs/README.md` — documentation map and truth-source hierarchy.
3. Task-specific docs under `docs/architecture/`, `docs/backend/`, `docs/frontend/`, `docs/database/`, `docs/devops/`, and `docs/skills/`.

## Execution discipline

- Before coding, analyze whether the user's request is reasonable. If it is unreasonable, explain why and propose a better option. Only proceed with the unreasonable approach after the user confirms a second time.
- If any requirement, business rule, path, service boundary, or implementation direction is uncertain, ask the user before coding. Do not guess.
- Do not implement an entire roadmap phase in one prompt. Split work into small tasks with clear write scope, acceptance criteria, and tests.
- Before editing, inspect existing code with `rg` and follow current project patterns.
- For any feature change, update documentation in the same change set.
- For new services, wire Aspire AppHost, Gateway routing, Docker Compose, configuration, migrations, and docs in the same task or explicitly sequenced subtasks.
- Do not commit secrets, production connection strings, JWT signing keys, or local-only environment files.

## Validation baseline

Use the narrowest meaningful checks for the changed area. Prefer:

- Backend: `dotnet build Api/src/ST.slnx`
- Frontend: `cd Web && pnpm build`
- Documentation-only: `git diff --check`

If a check cannot run because of environment limitations, document the limitation in the final response.
