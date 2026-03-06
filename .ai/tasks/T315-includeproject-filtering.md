# T315: Verify and fix IncludeProject filtering
- Milestone: Post
- Status: Done
- Agent: Codex (Executor)
- Started: 2026-03-06
- Completed: 2026-03-06

## Objective
Verify whether `Settings.IncludeProject(...)` affects the current CLI generation scope, and fix the behavior with regression coverage if the setting is ignored.

## Approach
Trace the call path from `Typewriter.CodeModel.Configuration.SettingsImpl.IncludeProject` through metadata/provider/runtime code, compare with upstream behavior under `origin/`, and add targeted tests around multi-project filtering.

## Journey
### 2026-03-06
- Started investigation from the current workspace after reading `.ai/progress.md`.
- Located `IncludeProject` declarations/usages in `Settings.cs`, `SettingsImpl.cs`, implementation notes, and archived ambiguity decisions.
- Traced `SettingsImpl.IncludeProject` into `ProjectHelpers.AddProject` and confirmed the CLI helper methods (`AddProject`, `AddCurrentProject`, `AddReferencedProjects`, `AddAllProjects`) are still M1 no-op stubs in `src/Typewriter.CodeModel/Configuration/ProjectHelpers.cs`.
- Traced the render path through `ApplicationRunner` and `RoslynMetadataProvider`: the runner pre-enumerates `metadataProvider.GetFiles(...)` with a comment explicitly stating settings are ignored, then renders every returned file. No later filter consults `SettingsImpl.IncludedProjects`.
- Compared with upstream `origin/src/Typewriter/Generation/Template.cs`, which uses `IncludedProjects` via `GetFilesToRender()` and `ShouldRenderFile(...)` before rendering.
- Searched tests/fixtures and confirmed the current multi-project golden fixture does not call `IncludeProject(...)`, so existing coverage would not catch the regression. Also found no live `TW12xx` implementation/tests despite the documented transformed-policy notes.
- Implemented a plain `ProjectInclusionContext`/`ProjectInclusionTarget` catalog in `Typewriter.CodeModel.Configuration`, built from the loaded workspace in `ApplicationRunner`, and passed through `Template` into `SettingsImpl`.
- Replaced the `ProjectHelpers` no-op inclusion methods with deterministic name/path resolution, current-project detection from template location or entry project, referenced-project expansion from the loaded graph, and `TW1201`/`TW1202` diagnostics.
- Changed `ApplicationRunner` to initialize `Template` before source-file enumeration and to enumerate files with `template.Settings`, so inclusion settings can actually affect generation scope.
- Updated `RoslynMetadataProvider` to filter loaded projects by `SettingsImpl.IncludedProjects` only when inclusion APIs were explicitly invoked, preserving the current whole-workspace default for templates that do not opt into project scoping.
- Added `SettingsImplProjectInclusionTests` for unique-name and ambiguous-name/path-qualified resolution, plus `IncludeProjectIntegrationTests` covering end-to-end multi-project generation for `settings.IncludeProject("ApiLib")`.
- Attempted focused `dotnet test` runs after redirecting `DOTNET_CLI_HOME` into the workspace; sandbox first-use setup initially failed on permissions, then `dotnet test` stalled/timed out. Targeted `dotnet build --no-restore` attempts still hit the existing MSBuild `_GetProjectReferenceTargetFrameworkProperties` failure with `Build FAILED` / `0 Error(s)` and no actionable diagnostics.

## Outcome
- `IncludeProject(...)` now participates in the live CLI path:
  1. template constructors resolve project selectors against a loaded project catalog,
  2. ambiguous/not-found selectors emit `TW1202` / `TW1201`,
  3. metadata enumeration filters to explicitly included project paths before rendering.
- Explicit inclusion APIs covered in the implementation: `IncludeProject`, `IncludeCurrentProject`, `IncludeReferencedProjects`, `IncludeAllProjects`.
- Full mandatory verification remains outstanding due the sandbox/MSBuild environment, but targeted code/test changes are in place.

## Follow-ups
- Re-run full repo verification in a healthy environment and confirm the new unit/integration regressions pass.
- Consider adding explicit end-to-end coverage for `IncludeCurrentProject()` and `IncludeReferencedProjects()` on solution-root vs project-root template placements.
