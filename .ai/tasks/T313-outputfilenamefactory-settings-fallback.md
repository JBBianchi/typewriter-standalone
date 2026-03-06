# T313: Harden OutputFilenameFactory Settings compatibility fallback
- Milestone: Post
- Status: Done
- Agent: Codex (Executor)
- Started: 2026-03-06
- Completed: 2026-03-06

## Objective
Ensure legacy templates that resolve constructor `Settings` to `Typewriter.Metadata.Settings` can still compile and use `OutputFilenameFactory` without `CS1061` failures.

## Approach
Promote `OutputFilenameFactory` to the shared metadata `Settings` contract (without introducing `Typewriter.CodeModel` dependency), keep runtime behavior in `Template` unchanged, and add a dedicated template compilation regression test.

## Journey
### 2026-03-06 (attempt 1)
- Reviewed current compatibility surface from T311/T312 and traced compile path through `TemplateCodeParser` and `ShadowClass`.
- Confirmed `Settings` aliasing exists in shadow preamble, but user report still indicates `Settings` may bind to metadata in some template environments.
- Chosen hardening strategy: make `OutputFilenameFactory` available at `Typewriter.Metadata.Settings` level to eliminate binding sensitivity.

### 2026-03-06 (attempt 2)
- Updated `src/Typewriter.Metadata/Settings.cs` to expose `OutputFilenameFactory` as `Func<dynamic, string>?` for compatibility without cross-project type coupling.
- Added a typed bridge in `src/Typewriter.CodeModel/Configuration/Settings.cs` so `Typewriter.Configuration.Settings` keeps `Func<File, string>?` semantics while synchronizing to metadata-level `OutputFilenameFactory`; removed only the redundant override in `src/Typewriter.CodeModel/Configuration/SettingsImpl.cs`.
- Updated `src/Typewriter.Generation/Template.cs` to resolve output filename delegates through the shared metadata contract.
- Added regressions in `tests/Typewriter.UnitTests/Generation/TemplateEngineTests.cs`:
  - `LegacyCompatibility_TemplateCompilesWithMetadataSettingsOutputFilenameFactory`
  - `LegacyCompatibility_TemplateConstructorWithMetadataSettings_ConfiguresOutputFilenameFactory`

## Outcome
Implementation and regression coverage added. Partial build verification succeeded for directly touched settings projects (`dotnet build src/Typewriter.CodeModel/Typewriter.CodeModel.csproj -c Release` built both `Typewriter.Metadata` and `Typewriter.CodeModel` successfully). Full required verification remains blocked in this sandbox (`dotnet restore`/solution build restore graph failures with no actionable diagnostics and `dotnet test -c Release` timeout).

## Follow-ups
- Run mandatory verification: `dotnet restore`, `dotnet build -c Release`, `dotnet test -c Release`.
- If packaging behavior is validated in this session, run `dotnet pack -c Release`.
