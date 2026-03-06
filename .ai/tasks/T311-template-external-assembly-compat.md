# T311: Fix template external assembly compatibility
- Milestone: Post
- Status: Done
- Agent: Codex (Executor)
- Started: 2026-03-05
- Completed: 2026-03-05

## Objective
Fix template compilation failures when templates depend on legacy Typewriter namespaces and framework assemblies not currently referenced by the standalone compiler.

## Approach
Trace template compilation source generation and reference resolution in `Typewriter.Generation` (`TemplateCodeParser`, `ShadowClass`, `Compiler`), add targeted compatibility shims and missing metadata references, then validate with tests and full repo verification commands.

## Journey
### 2026-03-05 (attempt 1)
- Created task record and set progress state to active investigation.
- Investigated runtime template compilation errors (`Settings`, `ILog`, `Typewriter.Extensions.WebApi`, `Regex`, `dynamic`, `ExpandoObject`) reported from external real-world templates.
- Implemented compatibility shims in runtime code:
  - Added `Typewriter.Configuration.Settings` bridge in `src/Typewriter.CodeModel/Configuration/Settings.cs` extending `Typewriter.Metadata.Settings` and exposing upstream-facing `OutputFilenameFactory`/`Log`.
  - Added `Typewriter.VisualStudio.ILog` and no-op `Typewriter.VisualStudio.Log` in `src/Typewriter.CodeModel/VisualStudio/`.
  - Updated `SettingsImpl` to derive from compatibility `Settings` and implement `Log`.
- Reintroduced upstream Web API extension namespace by adding:
  - `src/Typewriter.CodeModel/Extensions/WebApi/HttpMethodExtensions.cs`
  - `src/Typewriter.CodeModel/Extensions/WebApi/RequestDataExtensions.cs`
  - `src/Typewriter.CodeModel/Extensions/WebApi/UrlExtensions.cs`
- Expanded template compiler baseline references/usings in `src/Typewriter.Generation/ShadowClass.cs`:
  - Added default usings for `System.Dynamic` and `System.Text.RegularExpressions`.
  - Added trusted platform references for `System.Linq.Expressions.dll`, `System.Dynamic.Runtime.dll`, `System.Text.RegularExpressions.dll`, and `Microsoft.CSharp.dll`.
- Updated constructor activation logic in `src/Typewriter.Generation/Template.cs` to invoke any template constructor whose single parameter is assignable from `SettingsImpl` (covers both `Typewriter.Metadata.Settings` and legacy `Typewriter.Configuration.Settings` signatures).
- Added regression tests in `tests/Typewriter.UnitTests/Generation/TemplateEngineTests.cs`:
  - `LegacyCompatibility_TemplateCompilesWithSettingsLogWebApiAndDynamic`
  - `LegacyCompatibility_TemplateConstructorWithConfigurationSettings_IsInvoked`
- Validation:
  - `dotnet build src/Typewriter.CodeModel/Typewriter.CodeModel.csproj -c Release --no-restore` passed.
  - Full mandatory verification (`dotnet restore`, `dotnet build -c Release`, `dotnet test -c Release`) is blocked in this sandbox by private-feed/network failures (`NU1301` against `nuget.pkg.github.com/neuroglia-io`).

## Outcome
Template-compatibility fixes are implemented for the reported failure class. Full end-to-end verification remains pending in a credentialed environment due restore constraints external to code changes.

## Follow-ups
- Re-run the mandatory verification trio with valid NuGet credentials/network access:
  - `dotnet restore`
  - `dotnet build -c Release`
  - `dotnet test -c Release`
