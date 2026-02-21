# Q1 Decision: IncludeProject(name) Ambiguity Policy

- Date: 2026-02-21
- Scope: `Settings.IncludeProject(string)` resolution in CLI for `.sln` / `.slnx` inputs
- Status: Final

## Context
`IncludeProject(name)` is ambiguous when multiple projects share the same display name across different folders in a solution.

## Evidence (Microsoft Learn)
1. Solution file format identifies projects by GUID (not by display name), and GUIDs are the unique identity:
   - https://learn.microsoft.com/en-us/visualstudio/extensibility/internals/solution-dot-sln-file?view=vs-2022
2. `dotnet sln remove` by name fails on ambiguity and requires disambiguation:
   - https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-sln
3. .NET SDK `.slnx` behavior explicitly allows same project names in different solution folders:
   - https://learn.microsoft.com/en-us/dotnet/core/compatibility/sdk/9.0/dotnet-sln

## Decision
- `IncludeProject(name)` must **not** silently choose one project when multiple name matches exist.
- Matching rules:
  1. Resolve by case-insensitive name.
  2. If exactly one match: include it.
  3. If zero matches: emit not-found diagnostic (`TW12xx`).
  4. If multiple matches: emit ambiguity diagnostic (`TW12xx`) and require a path-qualified selector.

## Why this choice
- Aligns with official CLI ambiguity behavior.
- Avoids non-deterministic or environment-dependent project selection.
- Preserves deterministic CI outcomes.

## Implementation impact
- Add ambiguity fixtures with duplicate names under `.sln`/`.slnx`.
- Add parser support for path-qualified include selector in settings.
- Add integration tests asserting deterministic `TW12xx` on ambiguous name-only selection.
