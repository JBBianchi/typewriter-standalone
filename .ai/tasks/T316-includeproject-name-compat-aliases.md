# T316: Restore `IncludeProject(name)` Backward Compatibility via Name Aliases
- Milestone: Post
- Status: Done
- Agent: Codex (Executor)
- Started: 2026-03-06
- Completed: 2026-03-06

## Objective
Preserve legacy `settings.IncludeProject("Project.Name")` behavior in CLI mode when Roslyn/MSBuild naming differs from historical VS DTE project naming.

## Approach
Add compatibility aliases to the project inclusion catalog and resolve `IncludeProject(name)` against both canonical project name and aliases. Populate aliases from loaded workspace metadata (`Project.Name`, `Project.AssemblyName`, and project file stem).

## Journey
### 2026-03-06 - Analysis and root cause
- Confirmed current include filtering logic in:
  - `src/Typewriter.CodeModel/Configuration/ProjectHelpers.cs`
  - `src/Typewriter.Metadata.Roslyn/RoslynMetadataProvider.cs`
  - `src/Typewriter.Application/ApplicationRunner.cs`
- Compared upstream behavior:
  - `origin/src/Typewriter/CodeModel/Configuration/ProjectHelpers.cs`
- Found compatibility gap: CLI inclusion context matched only `Project.Name` (plus path selector), which can diverge from prior VS-facing naming expectations.

### 2026-03-06 - Implementation
- Updated `ProjectInclusionTarget` to carry optional alias selectors:
  - `src/Typewriter.CodeModel/Configuration/ProjectInclusionContext.cs`
- Extended name matching in `ProjectHelpers.AddProject(...)`:
  - `src/Typewriter.CodeModel/Configuration/ProjectHelpers.cs`
  - `IsNameMatch(...)` now checks `ProjectName` and `NameAliases`.
- Populated aliases in workspace-derived inclusion context:
  - `src/Typewriter.Application/ApplicationRunner.cs`
  - Added `BuildProjectNameAliases(...)` to include:
    - Roslyn project name
    - Roslyn assembly name
    - `.csproj` filename stem

### 2026-03-06 - Regression tests
- Added unit regression:
  - `tests/Typewriter.UnitTests/CodeModel/SettingsImplProjectInclusionTests.cs`
  - `IncludeProject_NameAlias_AddsResolvedProjectPath`
- Added integration regression (end-to-end):
  - `tests/Typewriter.IntegrationTests/IncludeProjectIntegrationTests.cs`
  - `IncludeProject_FiltersGenerationByAssemblyNameAlias`
  - Creates a temp project with `AssemblyName=Agencr.Platform.Modules.Agents.Integration` and validates `IncludeProject("Agencr.Platform.Modules.Agents.Integration")`.
- Fixed nullability assertion in existing integration test (`Path.GetFileName(...)!`) to satisfy warnings-as-errors in test build.

## Outcome
- Implemented backward-compatible project-name resolution without requiring path-qualified selectors.
- Kept deterministic diagnostics (`TW1201`, `TW1202`) and explicit-selection semantics unchanged.
- Verification results:
  - `dotnet restore` ✅
  - `dotnet build -c Release` ✅ (known existing `MINVER1008` warning)
  - `dotnet test -c Release` ❌ due unrelated pre-existing failures:
    - `Typewriter.UnitTests.Loading.RoslynWorkspaceServiceTests.IsActionableCompilationError_ReturnsTrue_ForRegularSourceError`
    - `Typewriter.UnitTests.Metadata.MetadataParityTests.AllowedValuesAttribute_ParamsArray_DoesNotCrash`
  - Targeted regressions for this task pass:
    - `dotnet test ... --filter FullyQualifiedName~SettingsImplProjectInclusionTests` ✅
    - `dotnet test ... --filter FullyQualifiedName~IncludeProjectIntegrationTests` ✅

## Follow-ups
- Investigate and fix unrelated failing unit tests currently blocking full `dotnet test -c Release` green.
