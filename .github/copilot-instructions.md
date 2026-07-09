# ST Copilot Instructions

- Read `docs/ai/README.md` before generating code.
- First judge whether the request is reasonable. If it is unreasonable, explain why and propose a better plan; proceed with the original unreasonable plan only after the user confirms again.
- If paths, business rules, service boundaries, permissions, configuration keys, or data migration details are uncertain, ask before coding.
- Use `rg` to inspect existing patterns before editing.
- Follow task-specific docs in `docs/backend/`, `docs/frontend/`, `docs/architecture/`, `docs/database/`, `docs/devops/`, and `docs/skills/`.
- Keep code and docs in the same change set for feature changes.
- Do not commit secrets, production connection strings, JWT signing keys, or local-only environment files.
- Prefer checks: `dotnet build Api/src/ST.slnx`, `cd Web && pnpm build`, and `git diff --check`.

- For new services read `docs/backend/service-template.md`; for new APIs read `docs/backend/api-routing.md` and verify downstream + Gateway paths to avoid 404/502.
