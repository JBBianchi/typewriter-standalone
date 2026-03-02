# T005: Port CodeModel Implementation Files
- Milestone: M1
- Status: Done
- Agent: Executor (claude-sonnet-4-6)
- Started: 2026-03-02
- Completed: 2026-03-02

## Objective

Port all 19 `*Impl.cs` files + `DocComment.cs` from `origin/src/Typewriter/CodeModel/Implementation/` into `src/Typewriter.CodeModel/Implementation/`, updating namespaces, and also port `Helpers.cs` (required dependency) from `origin/src/Typewriter/CodeModel/Helpers.cs` into `src/Typewriter.CodeModel/`.

## Approach

1. Read T001 audit confirming all 19 implementation files are clean (no VS/COM refs).
2. Identify dependencies: all impl files use `using static Typewriter.CodeModel.Helpers;` → must port `Helpers.cs` first.
3. `Helpers.cs` and most impl files use `using Typewriter.Configuration;` for `Settings` → change to `using Typewriter.Metadata;` (consistent with T004 decision for `Type.cs`).
4. `Settings` stub in `src/Typewriter.Metadata/Settings.cs` was empty — `Helpers.cs` uses `settings.StrictNullGeneration` → add `public abstract bool StrictNullGeneration { get; }` to stub.
5. Port `Helpers.cs` with namespace `Typewriter.CodeModel` (matches origin, no change needed).
6. Port all 19 impl files into `src/Typewriter.CodeModel/Implementation/` with namespace `Typewriter.CodeModel.Implementation` (matches origin, no change needed).
7. Verify: zero VS/COM refs in all ported files; `origin/` unchanged.

## Journey

### 2026-03-02

- T001 audit confirmed: all 19 files in `origin/src/Typewriter/CodeModel/Implementation/` classified as **clean**.
- Task description said "12 files" but there are actually 19 (18 `*Impl.cs` + 1 `DocComment.cs`). Ported all 19.
- Discovered `Helpers.cs` is needed by impl files (via `using static Typewriter.CodeModel.Helpers;`) but was not yet ported — added it to scope.
- Key issue: `Settings` stub in `Typewriter.Metadata` had no properties. `Helpers.cs` calls `settings.StrictNullGeneration` → added abstract property to stub.
- Namespace change: `using Typewriter.Configuration;` → `using Typewriter.Metadata;` in `Helpers.cs` and all 18 impl files that use `Settings` (TypeParameterImpl has no Settings, so no change there).
- `docnet` not available in execution environment; build verification deferred to CI.
- Verified `origin/` unchanged: `git diff --name-only origin/` returned empty.
- Verified zero VS/COM refs in all created files.
- CI revealed 3 nullable errors (commit 71d2d69 fixed some, but 3 remained): CS8602 in DocComment.cs lines 22/26 (double `.Element()` call) and CS8604 in Helpers.cs line 115 (`FirstOrDefault()` → nullable arg).
- Final CI fix (commit eb72afa): DocComment.cs — capture XElement in local var with `is { }` pattern; Helpers.cs — use `typeArguments[0]` (non-nullable index) since count==1 is already checked.

## Outcome

Files created or modified:

| Category | Count | Target |
|----------|-------|--------|
| `Helpers.cs` | 1 | `src/Typewriter.CodeModel/Helpers.cs` |
| Implementation impl files | 19 | `src/Typewriter.CodeModel/Implementation/*.cs` |
| Modified: Settings stub | 1 | `src/Typewriter.Metadata/Settings.cs` (added `StrictNullGeneration`) |

`origin/` unchanged.

## Follow-ups

- T006: Port Roslyn metadata wrappers (18 clean files from `origin/src/Roslyn/`).
- Full `Settings` class (from `origin/src/CodeModel/Configuration/Settings.cs`) needs a complete port when VS-coupled members are rewritten — update `src/Typewriter.Metadata/Settings.cs` stub at that time.
