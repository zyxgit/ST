# ST Claude Instructions

## Required reading

1. `docs/ai/README.md`
2. `docs/README.md`
3. Task-specific docs under `docs/skills/`, `docs/backend/`, `docs/frontend/`, `docs/architecture/`, `docs/database/`, and `docs/devops/`.

## Execution rules

- Analyze request reasonableness before coding.
- If a request is unreasonable, explain risks and propose an optimized plan; implement the unreasonable approach only after second confirmation.
- Ask before coding when requirements, service boundaries, data migration, permissions, routes, or configuration are uncertain.
- Confirm existing code with `rg` and follow current project patterns.
- Keep feature changes and documentation updates in the same change set.
- Do not commit secrets, production connection strings, JWT signing keys, or local-only environment files.

## Validation

- Backend: `dotnet build Api/src/ST.slnx`
- Frontend: `cd Web && pnpm build`
- Documentation-only: `git diff --check`
