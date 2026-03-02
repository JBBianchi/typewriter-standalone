# T007: Stub/Rewrite VS-Coupled Configuration Files
- Milestone: M1
- Status: Done
- Agent: Executor (claude-sonnet-4-6)
- Started: 2026-03-02
- Completed: 2026-03-02

## Objective

Create CLI-compatible replacements for `SettingsImpl.cs` and `ProjectHelpers.cs`.
Both files are VS-coupled in origin (EnvDTE, ThreadHelper, VSLangProj) and cannot
be ported verbatim.

## Approach

1. Read T001 audit confirming VS-coupling in both files.
2. Expand `src/Typewriter.Metadata/Settings.cs` stub to a full abstract class
   (noted as a follow-up in T005).
3. Create `src/Typewriter.CodeModel/Configuration/SettingsImpl.cs` extending
   `Settings` with a CLI-path-based, immutable-at-construction implementation.
4. Create `src/Typewriter.CodeModel/Configuration/ProjectHelpers.cs` with
   filesystem-based stubs; project-graph methods deferred to M3.

## Journey

### 2026-03-02

- Read origin `SettingsImpl.cs` and `ProjectHelpers.cs` to understand all
  VS-coupled members (`ProjectItem`, `DTE`, `ThreadHelper`, `VSProject`, `Reference`).
- Read origin `Settings.cs` abstract base class to understand the full interface.
- Read existing `src/Typewriter.Metadata/Settings.cs` stub (only had `StrictNullGeneration`).
- Identified circular-dependency constraint: `Settings` (in `Typewriter.Metadata`)
  cannot include `PartialRenderingMode` (in `Typewriter.CodeModel`) or
  `OutputFilenameFactory` (uses `Typewriter.CodeModel.File`) without creating a
  circular project reference. Resolution: keep those members out of the abstract
  base; add them as concrete properties directly on `SettingsImpl`.
- Verified `File` type in `Func<File, string>?` on `SettingsImpl` resolves to
  `Typewriter.CodeModel.File` via C# enclosing-namespace lookup, not
  `System.IO.File` from implicit usings.
- dotnet toolchain not present in execution environment; build verification
  deferred to CI (same situation as T005).

## Outcome

Files created or modified:

| File | Action |
|------|--------|
| `src/Typewriter.Metadata/Settings.cs` | Expanded: full abstract class with all portable members |
| `src/Typewriter.CodeModel/Configuration/SettingsImpl.cs` | Created: CLI impl, no VS/COM refs |
| `src/Typewriter.CodeModel/Configuration/ProjectHelpers.cs` | Created: filesystem stubs, no VS/COM refs |

**Design decisions:**
- `PartialRenderingMode` and `OutputFilenameFactory` omitted from `Settings`
  abstract class to avoid circular project dependency; both added directly to
  `SettingsImpl`.
- `ILog`/`Log` omitted entirely — not used by CodeModel in M1; will be
  reconsidered if needed for M2 CLI diagnostics.
- `ProjectHelpers`: `AddProject`, `AddCurrentProject`, `AddReferencedProjects`,
  `AddAllProjects` are M1 no-ops; `GetProjectItems` and `ProjectListContainsItem`
  use filesystem path matching without DTE.
- `IncludedProjects` lazy-population pattern from upstream preserved on
  `SettingsImpl`; mirrors the VS-side behavior (current + referenced projects by
  default).

**Accepted parity gap (documented):**
- M1 `IncludeProject(name)`, `IncludeCurrentProject()`, `IncludeReferencedProjects()`,
  `IncludeAllProjects()` are no-ops. Full MSBuild-backed implementation is M3 scope.
  This is intentional — see task description and M3 milestone.

`origin/` unchanged.

## Follow-ups

- M3: Replace no-op stubs in `ProjectHelpers` with `ProjectGraph` traversal.
- M2: Decide whether `ILog` interface belongs in `Typewriter.Metadata` for CLI
  diagnostics wiring.
