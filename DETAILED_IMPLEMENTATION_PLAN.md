# 1. Executive Summary

## Chosen Approach (10-15 bullets)
- Build a new cross-platform CLI on `net10.0` only, with no Visual Studio runtime dependencies.
- Reuse upstream generation and CodeModel logic as the default strategy; rewrite only where Visual Studio coupling makes reuse unsafe.
- Keep upstream project layering concepts to reduce drift: `CodeModel`, `Metadata`, `Roslyn metadata`, `Generation`, plus CLI orchestration.
- Use a two-phase loading pipeline: `ProjectGraph` for deterministic traversal, then Roslyn/MSBuild workspace loading for semantic models.
- Treat `.slnx` as first-class via native graph loading; do not own a custom `.slnx` parser as the primary path.
- Adopt MSBuild-compatible diagnostics with stable Typewriter error codes (`TWxxxx`) for CI parsing.
- Keep template syntax and behavior unchanged (`#reference`, `${ }`, filters, single-file mode, output rules).
- Introduce explicit parity gates in CI: golden output diffs, diagnostic snapshots, and metadata parity suites.
- Run restore as an explicit stage (`--restore` opt-in), with deterministic exit-code mapping for SDK/restore/load failures.
- Use `dotnet tool` as the primary packaging target; self-contained binaries are optional post-v1.
- Treat source generators and partial `requestRender` behavior as explicit validation tracks, not implicit assumptions.
- Prioritize a vertical slice early (load -> metadata -> render -> write) before broad optimizations.
- Add performance/caching hardening only after parity and determinism pass on all three OSes.

## Top 10 Final Decisions
1. Target framework: `net10.0` everywhere.
   - Rationale: both plans agree; required by constraints; best long-term baseline.
2. Primary distribution: `dotnet tool`.
   - Rationale: both plans agree; CI-native; lowest operational friction.
3. Loading architecture: `ProjectGraph` + Roslyn workspace hybrid.
   - Rationale: resolved in favor of Codex from project-loading divergence outcomes.
4. `.slnx` strategy: native SDK/graph support first; fallback only on explicit loader gaps.
   - Rationale: empirical spike evidence beats custom parser design.
5. Reuse policy: "lift first, rewrite last" with path-level ownership mapping.
   - Rationale: drift minimization is a hard requirement.
6. Diagnostics contract: MSBuild-compatible text + `TW` codes.
   - Rationale: Claude's stronger UX insight adopted.
7. Template assembly loading: dedicated `AssemblyLoadContext`.
   - Rationale: Claude risk R-0003 is valid and high impact.
8. Multi-target default: choose first TFM deterministically unless `--framework` is supplied.
   - Rationale: avoids duplicate generation and ambiguity; still supports explicit multi-target selection.
9. Parity governance: explicit `identical / transformed / deferred` contract tied to test IDs.
   - Rationale: Codex parity rigor is retained to prevent optimistic parity claims.
10. CI gate model: no release without cross-platform parity, golden pass, and deterministic diagnostics.
    - Rationale: aligns CI-first objective and drift prevention.

# 2. Inputs and Source Material

## Primary Plans
- `_archive/IMPLEMENTATION_PLAN_CODEX.md`
- `_archive/IMPLEMENTATION_PLAN_CLAUDE.md`

## Comparison Artifacts (Primary Decision Inputs)
- `_archive/OVERALL_ANALYSIS_COMPARISON_CODEX.md`
- `_archive/OVERALL_ANALYSIS_COMPARISON_CLAUDE.md`
- `_archive/IMPLEMENTATION_PLAN_COMPARISON_CODEX.md`
- `_archive/IMPLEMENTATION_PLAN_COMPARISON_CLAUDE.md`
- `_archive/D-0003-project-loading-strategy_COMPARISON_CODEX.md`
- `_archive/D-0003-project-loading-strategy_COMPARISON_CLAUDE.md`
- `_archive/PR-0001-msbuild-loading-spike_COMPARISON_CODEX.md`
- `_archive/PR-0001-msbuild-loading-spike_COMPARISON_CLAUDE.md`
- `_archive/P-0001-feature-matrix_COMPARISON_CODEX.md`
- `_archive/P-0001-feature-matrix_COMPARISON_CLAUDE.md`
- `_archive/questions-folder_COMPARISON_CODEX.md`
- `_archive/questions-folder_COMPARISON_CLAUDE.md`
- `_archive/risks-folder_COMPARISON_CODEX.md`
- `_archive/risks-folder_COMPARISON_CLAUDE.md`

## Supporting Evidence Notes
- `_archive/.ai_CODEX/findings/F-0001-solution-and-project-inventory.md`
- `_archive/.ai_CODEX/findings/F-0004-template-discovery-and-execution.md`
- `_archive/.ai_CODEX/findings/F-0005-output-path-and-file-io-rules.md`
- `_archive/.ai_CODEX/findings/F-0007-semantic-model-extraction-and-fidelity.md`
- `_archive/.ai_CODEX/findings/F-0008-diagnostics-and-error-surface.md`
- `_archive/.ai_CODEX/findings/F-0009-vs-sdk-vsix-dependency-cluster.md`
- `_archive/.ai_CODEX/findings/F-0010-envdte-and-solution-object-model-cluster.md`
- `_archive/.ai_CODEX/findings/F-0011-vs-services-events-and-threading-cluster.md`
- `_archive/.ai_CODEX/findings/F-0012-mef-editor-language-service-cluster.md`
- `_archive/.ai_CODEX/findings/F-0013-com-registry-and-windows-assumptions-cluster.md`
- `_archive/.ai_CODEX/findings/F-0015-buildalyzer-and-msbuild-usage-gap.md`
- `_archive/.ai_CODEX/prototypes/PR-0001-msbuild-loading-spike.md`
- `_archive/.ai_CLAUDE/risks/R-0003-assembly-loading-crossplatform.md`
- `_archive/.ai_CLAUDE/questions/Q-0001-source-generators.md`

# 3. Final Meta-Assessment (What We Trust)

| Claim | Source | Confidence (1-5) | Why We Trust It | Implications |
|---|---|---:|---|---|
| `net10.0` is the right baseline | Codex + Claude decisions | 5 | Full alignment across both plans and comparisons | Use single TFM across all new projects |
| `dotnet tool` is the right primary package | Codex + Claude decisions | 5 | Full alignment and direct requirement fit | Build release around tool pack/install flow |
| ProjectGraph loads `.csproj`, `.sln`, `.slnx` in practice | Codex PR-0001 + comparisons | 5 | Backed by executed spike evidence, not design-only | `.slnx` support should be graph-native first |
| Custom `.slnx` parser should not be primary design | D-0003 + PR-0001 comparisons | 5 | Divergence resolved repeatedly in favor of native support | Remove parser ownership from critical path |
| Semantic fidelity depends on Roslyn model parity | Both findings + risk docs | 5 | Strong independent agreement and upstream evidence | Preserve Roslyn metadata behavior with parity tests |
| Template assembly loading is a real cross-platform risk | Claude R-0003 + risk comparisons | 4 | Identified as a concrete migration trap; no contradiction | Implement `AssemblyLoadContext` with resolver tests |
| Source-generator visibility is uncertain and must be tested | Claude Q-0001 + question comparisons | 4 | Explicitly called out as unresolved; no direct contradictory evidence | Add generator fixture in semantic parity milestone |
| `IncludeProject(name)` ambiguity is real | Codex Q-0001 + question comparisons | 4 | Upstream name-based DTE behavior can be ambiguous in monorepos | Resolved with deterministic ambiguity error policy (`TW12xx`) and path-qualified selector requirement |
| `requestRender` combined-partial behavior must be mapped | Codex Q-0004 + question comparisons | 4 | Upstream callback behavior is evidence-backed and parity-critical | Implement deterministic render-session queue with scope boundary, bounded convergence, and enqueue observability |
| Performance risk on large solutions is real | Codex R-0004 + risk comparisons | 4 | Upstream warning + graph expansion evidence | Add caching and perf thresholds before release |
| MSBuild-compatible diagnostics improve CI usability | Claude implementation comparison | 5 | Explicit comparative advantage with no downside | Standardize parseable error format and codes |
| CI must enforce parity gates | Both plans, stronger in Codex quality gates | 5 | Strong alignment and direct fit for CI-first goal | Block release on golden/diagnostic drift |
| Overly granular assembly split can slow delivery | Implementation plan comparison | 3 | Reasonable concern; not hard evidence | Use balanced modularity (clear boundaries, fewer thin projects) |
| Buildalyzer is not required in runtime path | Codex F-0015 + both analyses | 5 | Direct source search evidence | Keep Buildalyzer out of v1 critical path |
| VS editor features are out of CLI scope | Both parity analyses | 5 | Full alignment and clear product boundary | Explicitly mark as deferred/not planned |

# 4. Strategy: Reuse Upstream to Prevent Drift

## 4.1 Reuse Policy
- Primary rule: copy/adapt upstream core code before considering rewrite.
- Rewrite allowed only for:
  - Visual Studio host coupling (`EnvDTE`, `Microsoft.VisualStudio.*`, COM, registry).
  - .NET Framework API migration points that break on .NET 10.
  - Determinism/CI contracts that upstream does not define (exit codes, diagnostic structure).
- Every rewrite must have:
  - explicit reason,
  - parity test,
  - drift mitigation owner.

## 4.2 Concrete Reuse Plan
### Lift mostly verbatim (high reuse)
- `origin/src/CodeModel/*` abstractions and extension methods.
- `origin/src/Metadata/*` interfaces.
- Roslyn metadata wrappers in `origin/src/Roslyn/Roslyn*Metadata.cs` (17 files plus synthetic `RoslynVoidTaskMetadata`).
- `origin/src/Typewriter/CodeModel/Implementation/*` and `Collections/*`.
- `origin/src/Typewriter/Generation/Parser.cs`, `SingleFileParser.cs`, `ItemFilter.cs`.

### Reuse with minimal adaptation
- `origin/src/Typewriter/Generation/TemplateCodeParser.cs` (`ProjectItem` -> file path context).
- `origin/src/Typewriter/TemplateEditor/Lexing/Roslyn/ShadowClass.cs` (relocated to generation core).
- `origin/src/Typewriter/Generation/Compiler.cs` (`Assembly.LoadFrom` path replaced by dedicated load context).
- `origin/src/Typewriter/Generation/Template.cs` (remove DTE mutation, preserve output semantics).
- `origin/src/Roslyn/RoslynFileMetadata.cs` (remove `ThreadHelper`, keep logic).

### Replace entirely
- VS host lifecycle and events:
  - `origin/src/Typewriter/VisualStudio/*`
  - `origin/src/Typewriter/Generation/Controllers/*`
- DTE project traversal and mutation helpers:
  - `origin/src/Typewriter/CodeModel/Configuration/ProjectHelpers.cs`
  - DTE-dependent parts of `SettingsImpl`.
- VS diagnostics surfaces (`ErrorList`, output window, status bar).

## 4.3 Drift Prevention Mechanisms
- Golden parity suite:
  - Run upstream fixture templates and compare generated output snapshots.
  - Snapshot tests include line endings and encoding policy.
- Metadata parity suite:
  - Port upstream metadata assertions; include nullable/task/tuple/partial/generics coverage.
- Diagnostic contract snapshots:
  - Freeze expected `TW` code output for common failures.
- Fixture repos:
  - dedicated fixture trees for `.csproj`, `.sln`, `.slnx`, multi-target, source generators.
- Compatibility profile:
  - `compatibilityMode: "upstream-vsix"` in config (default true in v1).
  - Any behavior change requires explicit profile/version change and new parity baseline.
- CI parity gate:
  - release blocked if golden diffs or contract snapshots change without approval.
# 5. Target Architecture (.NET 10)

## 5.1 Solution layout

### Proposed Projects
- `src/Typewriter.Cli`
  - Purpose: command-line entry point, argument parsing, process exit contract, console diagnostics writer.
  - Depends on: `Typewriter.Application`.
- `src/Typewriter.Application`
  - Purpose: orchestration pipeline, config precedence, command handlers, parity gate invocation.
  - Depends on: `Typewriter.Loading.MSBuild`, `Typewriter.Generation`, `Typewriter.CodeModel`, `Typewriter.Metadata`, `Typewriter.Metadata.Roslyn`.
- `src/Typewriter.Loading.MSBuild`
  - Purpose: input resolution, restore checks/trigger, graph traversal, workspace bootstrap.
  - Depends on: MSBuild/Roslyn loading packages.
- `src/Typewriter.CodeModel`
  - Purpose: template-facing object model, settings API, implementations, helpers, extensions.
  - Depends on: `Typewriter.Metadata`.
- `src/Typewriter.Metadata`
  - Purpose: metadata interfaces and contracts.
  - Depends on: none.
- `src/Typewriter.Metadata.Roslyn`
  - Purpose: Roslyn-backed metadata provider and wrappers.
  - Depends on: `Typewriter.Metadata`, `Typewriter.CodeModel`.
- `src/Typewriter.Generation`
  - Purpose: template discovery, compile/preprocess, parser execution, output path and write policy.
  - Depends on: `Typewriter.CodeModel`, `Typewriter.Metadata`, `Typewriter.Metadata.Roslyn`.
- `tests/Typewriter.UnitTests`
- `tests/Typewriter.IntegrationTests`
- `tests/Typewriter.GoldenTests`
- `tests/Typewriter.PerformanceTests`

### Core NuGet packages and rationale
- `System.CommandLine`: robust CLI parsing with validation.
- `Microsoft.Build`, `Microsoft.Build.Graph`, `Microsoft.Build.Locator`: deterministic project graph and SDK binding.
- `Microsoft.CodeAnalysis.CSharp.Workspaces`, `Microsoft.CodeAnalysis.Workspaces.MSBuild`: Roslyn semantic model loading from MSBuild projects.
- `Microsoft.Extensions.Logging` (+ console provider): structured verbosity control.
- Testing stack: `xunit`, `FluentAssertions`, `Verify.Xunit` (golden snapshots).

### Shared abstractions
- Command context DTOs (`GenerateRequest`, `GenerateResult`).
- Load-plan DTOs (`ProjectLoadPlan`, `LoadTarget`, `WorkspaceLoadResult`).
- Diagnostic DTO (`TwDiagnostic` with `Code`, `Severity`, `File`, `Line`, `Column`).
- Output DTO (`RenderedArtifact`, `WriteResult`).

## 5.2 Module boundaries + key interfaces

| Interface/Class | Responsibility | Inputs | Outputs | Error Contract |
|---|---|---|---|---|
| `IGenerateCommandHandler` | End-to-end orchestration | `GenerateRequest` | `GenerateResult` | Returns mapped failure with `TwDiagnostic` + exit code |
| `IInputResolver` | Resolve `--solution/--project` and template globs | CLI args + cwd | `ResolvedInput` | `TW100x` invalid input errors |
| `IRestoreService` | Check/execute restore | Entry path + load props | `RestoreResult` | `TW200x`; maps to exit code 3 |
| `IProjectGraphService` | Build deterministic graph traversal plan | Entry path + global props | `ProjectLoadPlan` | `TW210x` graph/load failures |
| `IRoslynWorkspaceService` | Open projects/solution into Roslyn context | `ProjectLoadPlan` | `WorkspaceLoadResult` | `TW220x` workspace failures |
| `ISemanticMetadataProvider` | Build metadata from documents | Document path + settings | `IFileMetadata` | `TW300x` semantic failures |
| `ITemplateDiscoveryService` | Resolve templates and ordering | glob patterns + graph info | ordered template list | `TW110x` template discovery failures |
| `ITemplateCompilationService` | Preprocess and compile template code | template text + refs | compiled template type/context | `TW310x` compile failures |
| `ITemplateRenderService` | Evaluate template for file sets | compiled template + metadata | rendered outputs | `TW320x` parse/runtime failures |
| `IOutputPathPolicy` | Compute output paths and collision names | settings + source file + overrides | normalized path | `TW400x` path policy failures |
| `IOutputWriter` | Write outputs with change detection and encoding policy | rendered outputs + write options | write summary | `TW410x` IO failures |
| `IDiagnosticReporter` | Emit parseable diagnostics + summary | `TwDiagnostic` | console/log sink output | Never throws; accumulates state |
| `IParityGate` | Run optional parity checks in CI mode | generated outputs + baselines | pass/fail result | Fails with `TW900x` parity violations |

## 5.3 Data flow (end-to-end)
1. `Typewriter.Cli` parses `generate` command and creates `GenerateRequest`.
2. `IInputResolver` validates template globs and resolves one input root (`.csproj`/`.sln`/`.slnx`).
3. `IRestoreService` validates assets; executes restore when `--restore` is set.
4. `IProjectGraphService` produces `ProjectLoadPlan` with deterministic project order and selected TFM/config.
5. `IRoslynWorkspaceService` loads Roslyn projects/documents per `ProjectLoadPlan`.
6. `ITemplateDiscoveryService` enumerates `.tst` templates deterministically.
7. For each template:
   - `ITemplateCompilationService` runs pre-pass (`#reference`, `${}`, lambda rewrite) and compiles runtime type.
   - `ISemanticMetadataProvider` builds `IFileMetadata` for included files/projects.
   - `ITemplateRenderService` executes parser (per-file or single-file mode).
8. `IOutputPathPolicy` computes target paths; `IOutputWriter` writes only changed files.
9. `IDiagnosticReporter` emits summary; `Typewriter.Cli` maps aggregate result to exit code.
# 6. MSBuild and Workspace Design

## 6.1 Final strategy decision
- Final architecture: hybrid `ProjectGraph` (traversal/control) + Roslyn workspace (semantics).
- Decision basis:
  - Codex position: graph-first hybrid with empirical `.slnx` proof.
  - Claude position: MSBuildWorkspace-centric with custom `.slnx` parser.
  - Technical evaluation: graph-first eliminates custom format ownership and gives better deterministic orchestration.
  - Final decision: adopt graph-first hybrid.
  - Confidence: 5/5.
  - Validation: integration tests for `.csproj/.sln/.slnx` + multi-target fixtures.
- Evidence references:
  - `_archive/D-0003-project-loading-strategy_COMPARISON_CLAUDE.md` Divergence #1 and #2.
  - `_archive/PR-0001-msbuild-loading-spike_COMPARISON_CLAUDE.md` Divergence #1 and #3.

## 6.2 Bridge object between graph and semantic phases
- Introduce explicit phase bridge (resolves shared blind spot):

```csharp
public sealed record ProjectLoadPlan(
    string EntryPath,
    string? SolutionDirectory,
    IReadOnlyList<LoadTarget> Targets,
    IReadOnlyDictionary<string, string> GlobalProperties);

public sealed record LoadTarget(
    string ProjectPath,
    string ProjectName,
    string? TargetFramework,
    string Configuration,
    string? RuntimeIdentifier,
    int TraversalOrder);
```

- Stage A produces `ProjectLoadPlan`.
- Stage B consumes `ProjectLoadPlan` and opens each target in deterministic order.

## 6.3 `.sln` loading
- Use `ProjectGraph(entrySlnPath, globalProperties)`.
- Build ordered target list by:
  - topological order,
  - stable path sort as tie-breaker.
- Emit `TW2110` if graph build fails.

## 6.4 `.slnx` loading (primary + fallback)
- Primary path: same as `.sln` via `ProjectGraph`.
- Fallback path (only if graph loader fails with format/tooling gap):
  1. invoke `dotnet sln <file.slnx> list`.
  2. parse project paths from output and create `ProjectLoadPlan`.
  3. load projects directly via workspace service.
- If fallback also fails: emit `TW2310`, exit code 3.
- This keeps custom parser ownership out of v1 while still providing resilience.

## 6.5 `.csproj` loading
- Use `ProjectGraph(entryCsprojPath, globalProperties)`.
- Graph may include references; inclusion policy follows template settings (`IncludeCurrentProject`, `IncludeReferencedProjects`, `IncludeAllProjects`).

## 6.6 Restore strategy and CI behavior
- Default behavior: no implicit restore; fail fast on missing assets.
- `--restore`: run restore before Stage A using same global properties.
- Missing assets without restore: `TW2003` and exit code 3.
- CI recommendation:
  - explicit `dotnet restore` stage before generate,
  - optionally keep `--restore` in smoke tests only.

## 6.7 `global.json` and SDK binding
- Respect SDK resolution through standard .NET host behavior.
- Register MSBuild instance once (via `MSBuildLocator`) before workspace operations.
- Log selected SDK + MSBuild location at `detailed` verbosity.
- Invalid SDK pin maps to `TW2001` and exit code 3.

## 6.8 Multi-target iteration rules
- If `--framework` provided: load only that TFM.
- If not provided and project has `TargetFrameworks`:
  - choose first declared TFM deterministically,
  - emit informational `TW2401` noting selected default,
  - recommend `--framework` for explicit behavior in CI.
- `--configuration` default: `Debug` unless set.
- `--runtime` optional: forwarded as `RuntimeIdentifier` global property.

# 7. CLI UX and Configuration System

## 7.1 Commands and flags

### Command
- `typewriter-cli generate <templates> [--solution <path> | --project <path>] [options]`

### Flags
- Required/selection:
  - `<templates>`: glob(s) for template files, for example `"**/*.tst"`.
  - `--solution <path>`: `.sln` or `.slnx` input.
  - `--project <path>`: `.csproj` input.
- Runtime and loading:
  - `--framework <TFM>`
  - `--configuration <Debug|Release>`
  - `--runtime <RID>`
  - `--restore`
  - `--msbuild-path <path>`
- Generation:
  - `--output <dir>`
  - `--fail-on-warnings`
- Diagnostics:
  - `--verbosity quiet|normal|detailed`
## 7.2 Configuration file and precedence

### File
- Optional `typewriter.json` at repo root.

### Example
```json
{
  "solution": "Typewriter.slnx",
  "templates": "**/*.tst",
  "framework": "net10.0",
  "configuration": "Release",
  "output": "generated",
  "verbosity": "normal",
  "compatibilityMode": "upstream-vsix"
}
```

### Precedence (highest to lowest)
1. CLI flags.
2. Template `Settings` code (for template-behavior knobs).
3. `typewriter.json` defaults.
4. Built-in defaults.

### Conflict rules
- CLI `--output` overrides `Settings.OutputDirectory`.
- CLI `--framework` overrides inferred framework.
- Template settings remain authoritative for template semantics (`SingleFileMode`, filters, partial mode, etc.) unless explicitly overridden by a CLI flag.

## 7.3 Logging and diagnostics

### Diagnostic format
- MSBuild-compatible default:
  - `<file>(<line>,<column>): <severity> TWxxxx: <message>`
- Non-file diagnostics:
  - `typewriter: <severity> TWxxxx: <message>`

### Code ranges
- `TW1xxx`: argument/input/template discovery.
- `TW2xxx`: SDK/restore/MSBuild/graph/workspace loading.
- `TW3xxx`: template compile/parse/runtime.
- `TW4xxx`: output path/write.
- `TW9xxx`: parity gate or internal contract violations.

### Verbosity
- `quiet`: errors only.
- `normal`: errors, warnings, summary.
- `detailed`: adds stage timings, resolved SDK/MSBuild info, project/template counts.

## 7.4 Exit codes mapping
- `0`: success.
- `1`: generation/runtime/template errors; warnings elevated by `--fail-on-warnings`.
- `2`: invalid arguments/inputs.
- `3`: restore/load/build/SDK errors.

## 7.5 Usage examples

### Common usage
```bash
typewriter-cli generate "**/*.tst" --solution ./MyApp.slnx --framework net10.0
```

### CI usage
```bash
dotnet restore ./MyApp.slnx
typewriter-cli generate "**/*.tst" --solution ./MyApp.slnx --configuration Release --framework net10.0 --verbosity normal --fail-on-warnings
```

### Failure mode example (missing restore)
```text
typewriter: error TW2003: Restore assets missing for project ./src/App/App.csproj. Run 'dotnet restore' or pass '--restore'.
```

# 8. Feature Parity Contract

## 8.1 Consolidated parity commitments

### Must-have parity for v1
- Template preprocessor behavior (`#reference`, `${}`, lambda rewrite).
- Parser behavior (identifiers, filters, separators, conditionals, parent context).
- CodeModel semantics and type mapping.
- Roslyn metadata fidelity (nullable/task/tuple/generics/partial behavior).
- Output semantics (directory/filename rules, collision naming, unchanged-write skip, BOM options).
- Deterministic include behavior for current/referenced/all projects.

### Acceptable parity gaps for v1 (with mitigation)
- VS editor features: not planned; documented as out of scope.
- Watch mode: deferred; explicit one-shot CLI scope.
- Project-file mutation (`AddFromFile`, `CustomToolNamespace`): deferred by default; mitigation via SDK-style globs and documentation.

## 8.2 Per-feature test strategy and acceptance criteria

| Feature | Parity Target | Test Strategy | Acceptance Criteria |
|---|---|---|---|
| Template pre-pass (`#reference`, `${}`) | identical | unit + golden | output matches upstream fixtures; compile errors map to `TW31xx` |
| Parser blocks/filters/separators | identical | unit + golden | rendered content byte-equal to upstream `.result` baselines |
| Single-file mode | identical | integration + golden | single artifact name/content matches baseline |
| IncludeCurrent/Referenced/All | identical | integration | selected file set equals expected fixture manifest |
| IncludeProject(name) | transformed with explicit policy | integration + ambiguity tests | duplicate names produce deterministic `TW12xx` behavior |
| Partial combined `requestRender` behavior | transformed with deterministic batching | integration + regression | cross-file partial updates produce complete output set, no duplicate renders, and bounded queue convergence |
| Roslyn type semantics (nullable/task/tuple/etc.) | identical | unit metadata suite | all upstream-equivalent assertions pass |
| WebApi/type extensions | identical | unit + golden | helper method outputs match expected baseline |
| Output path and collisions | identical | unit | collision sequence, invalid-char normalization, directory resolution all match |
| Unchanged write skip | identical | integration | second run writes zero files when inputs unchanged |
| BOM policy | identical | unit + integration | generated bytes include/exclude BOM per settings |
| Diagnostics | transformed but contract-stable | snapshot tests | diagnostics parse format and codes remain stable |
# 9. Implementation Roadmap (Milestones)

## M0 - Repo bootstrap and packaging skeleton
- Goal:
  - establish .NET 10 solution and tool packaging skeleton.
- Scope:
  - no business logic; compileable project graph and baseline tests.
- Tasks:
  - create projects in Section 5.1.
  - set all TFMs to `net10.0`.
  - configure `Typewriter.Cli` as `PackAsTool` with command name `typewriter-cli`.
  - add baseline CI workflow with restore/build/test placeholders.
- Files/projects touched:
  - `Typewriter.Cli.slnx`, `src/*/*.csproj`, `Directory.Build.props`, `global.json`, `.github/workflows/ci.yml`.
- Acceptance tests (exact):
  - `dotnet build Typewriter.Cli.slnx -c Release`
  - `dotnet test tests/Typewriter.UnitTests/Typewriter.UnitTests.csproj`
  - `dotnet pack src/Typewriter.Cli/Typewriter.Cli.csproj -c Release -o ./artifacts/nupkg`
- Definition of Done:
  - builds and packs on Windows/Linux/macOS.
- Risks and mitigations:
  - SDK drift -> pin `global.json` and fail if SDK mismatch.

## M1 - Core reuse extraction (CodeModel/Metadata)
- Goal:
  - lift reusable upstream core with minimal edits.
- Scope:
  - `CodeModel`, `Metadata`, Roslyn metadata wrappers (not provider rewrite yet).
- Tasks:
  - copy upstream abstractions/interfaces/implementations/extensions/helpers.
  - update namespaces and project references.
  - remove obvious net472-only constructs.
  - create compatibility checklist per copied path.
- Files/projects touched:
  - `src/Typewriter.CodeModel/**`, `src/Typewriter.Metadata/**`, `src/Typewriter.Metadata.Roslyn/Roslyn*Metadata.cs`.
- Acceptance tests (exact):
  - `dotnet test tests/Typewriter.UnitTests --filter "FullyQualifiedName~CodeModel"`
  - `dotnet test tests/Typewriter.UnitTests --filter "FullyQualifiedName~TypeMapping"`
- Definition of Done:
  - reusable core compiles on `net10.0` with unit coverage for helpers and extensions.
- Risks and mitigations:
  - hidden VS coupling -> enforce compile-time ban list for `Microsoft.VisualStudio.*` and `EnvDTE`.

## M2 - CLI contract, diagnostics, and configuration precedence
- Goal:
  - lock external CLI behavior before deep internals.
- Scope:
  - command syntax, flags, config parsing, diagnostic formatter, exit-code mapping.
- Tasks:
  - implement `generate` command parser.
  - implement `typewriter.json` loader and precedence merge.
  - implement `IDiagnosticReporter` and `TW` code catalog.
  - implement `--fail-on-warnings` behavior.
- Files/projects touched:
  - `src/Typewriter.Cli/**`, `src/Typewriter.Application/Configuration/**`, `src/Typewriter.Application/Diagnostics/**`.
- Acceptance tests (exact):
  - `CliContractTests.Generate_InvalidArgs_Returns2`
  - `CliContractTests.Generate_WarningsWithFailFlag_Returns1`
  - `DiagnosticFormatTests.MsBuildStyleMessage_IsParseable`
  - `ConfigurationPrecedenceTests.CliOverridesConfigAndTemplate`
- Definition of Done:
  - contract tests pass; diagnostics snapshots approved.
- Risks and mitigations:
  - format churn -> snapshot lock and code review rule for diagnostic changes.

## M3 - MSBuild loading: `.csproj` and restore pipeline
- Goal:
  - load standalone project inputs deterministically.
- Scope:
  - input resolution, restore checks, graph plan for `.csproj`.
- Tasks:
  - implement `IInputResolver`, `IRestoreService`, `IProjectGraphService` for project input.
  - implement asset checks and `--restore` execution.
  - map loader errors to `TW2xxx` + exit code 3.
- Files/projects touched:
  - `src/Typewriter.Loading.MSBuild/**`, `src/Typewriter.Application/Orchestration/**`.
- Acceptance tests (exact):
  - `ProjectLoaderTests.Csproj_LoadsWithoutRestore_WhenAssetsExist`
  - `ProjectLoaderTests.Csproj_MissingAssetsWithoutRestore_ReturnsTW2003`
  - `ProjectLoaderTests.Csproj_WithRestore_LoadsAfterRestore`
- Definition of Done:
  - `.csproj` path supports end-to-end dry render in integration fixture.
- Risks and mitigations:
  - environment-specific restore errors -> include raw stderr in detailed diagnostics.

## M4 - MSBuild loading: `.sln` and `.slnx`
- Goal:
  - unify solution loading paths with deterministic traversal.
- Scope:
  - graph loading for `.sln` and `.slnx`, plus fallback for `.slnx` tooling gaps.
- Tasks:
  - implement solution input loading through `ProjectGraph`.
  - implement `.slnx` fallback via `dotnet sln <path> list` parsing.
  - add traversal ordering and project identity map.
- Files/projects touched:
  - `src/Typewriter.Loading.MSBuild/ProjectGraphService.cs`
  - `src/Typewriter.Loading.MSBuild/SolutionFallbackService.cs`
- Acceptance tests (exact):
  - `SolutionLoaderTests.Sln_LoadsExpectedProjects`
  - `SolutionLoaderTests.Slnx_LoadsExpectedProjects`
  - `SolutionLoaderTests.SlnAndSlnx_ProduceSameTraversalPlan`
  - `SolutionLoaderTests.Slnx_WhenGraphFails_UsesFallback`
- Definition of Done:
  - `.sln` and `.slnx` fixtures both pass identical project-set assertions.
- Risks and mitigations:
  - fallback parse fragility -> fallback only used conditionally and heavily tested.
## M5 - Semantic model extraction parity
- Goal:
  - achieve Roslyn metadata fidelity with non-VS workspace.
- Scope:
  - rewrite provider layer, keep metadata wrappers.
- Tasks:
  - rewrite `RoslynMetadataProvider` to use workspace service output.
  - remove `ThreadHelper` usage from `RoslynFileMetadata`.
  - implement source-generator visibility test harness.
  - resolve partial combined render callback behavior in batch model using a deterministic render queue.
  - enforce a bounded render-session safety cap to prevent infinite queue loops.
  - enforce scope boundary so callback-enqueued files stay within current load/render scope.
  - add `detailed` verbosity logging when `requestRender` enqueues a new file.
- Files/projects touched:
  - `src/Typewriter.Metadata.Roslyn/RoslynMetadataProvider.cs`
  - `src/Typewriter.Metadata.Roslyn/RoslynFileMetadata.cs`
  - `tests/Typewriter.IntegrationTests/Fixtures/SourceGenerators/**`
- Acceptance tests (exact):
  - `MetadataParityTests.NullableTaskTupleGenericParity`
  - `MetadataParityTests.PartialCombinedMode_RequestRenderEquivalent`
  - `MetadataParityTests.PartialCombinedMode_RequestRender_RespectsScopeBoundary`
  - `MetadataParityTests.PartialCombinedMode_RequestRender_ConvergesWithinSafetyCap`
  - `MetadataParityTests.PartialCombinedMode_RequestRender_DetailedLogsNewEnqueue`
  - `MetadataParityTests.SourceGeneratorTypes_AreVisible`
- Definition of Done:
  - parity metadata suites pass on all OSes.
- Risks and mitigations:
  - generator gaps -> fallback path documented and flagged before release.

## M6 - Template execution and output management
- Goal:
  - port template engine and output semantics without VS coupling.
- Scope:
  - preprocess, compilation, parser runtime, path/write policies.
- Tasks:
  - adapt `TemplateCodeParser`, `Compiler`, `Template`, `ShadowClass`.
  - implement `TemplateAssemblyLoadContext` with resolver.
  - preserve collision naming, unchanged-write skip, BOM behavior.
  - implement deterministic batching for partial combined rendering.
- Files/projects touched:
  - `src/Typewriter.Generation/TemplateCodeParser.cs`
  - `src/Typewriter.Generation/Compiler.cs`
  - `src/Typewriter.Generation/TemplateAssemblyLoadContext.cs`
  - `src/Typewriter.Generation/Template.cs`
  - `src/Typewriter.Generation/Output/**`
- Acceptance tests (exact):
  - `TemplateEngineTests.ReferenceDirective_LoadsExternalAssembly`
  - `TemplateEngineTests.SingleFileMode_MatchesBaseline`
  - `OutputPolicyTests.CollisionSequence_MatchesUpstream`
  - `OutputPolicyTests.UnchangedContent_SkipsWrite`
  - `OutputPolicyTests.BomPolicy_IsRespected`
- Definition of Done:
  - golden tests for execution and output pass for core fixture set.
- Risks and mitigations:
  - assembly resolution failures -> add Linux/macOS-specific resolver tests.

## M7 - Golden parity and fixture repos
- Goal:
  - lock behavior parity and drift prevention.
- Scope:
  - comprehensive fixture repos and golden baselines.
- Tasks:
  - port upstream render fixtures and expected outputs.
  - add fixture sets: `simple`, `multi-project`, `multi-target`, `source-generators`, `complex-types`.
  - add deterministic normalization policy for EOL/encoding checks.
- Files/projects touched:
  - `tests/fixtures/**`
  - `tests/Typewriter.GoldenTests/**`
  - `tests/baselines/**`
- Acceptance tests (exact):
  - `dotnet test tests/Typewriter.GoldenTests/Typewriter.GoldenTests.csproj`
  - all baselines pass with zero unexpected diffs.
- Definition of Done:
  - parity matrix features tagged `identical` are covered by at least one passing test.
- Risks and mitigations:
  - noisy snapshots -> adopt explicit baseline update workflow.

## M8 - CI pipelines and release readiness
- Goal:
  - finalize CI/CD for build, test, generate verification, and tool publish.
- Scope:
  - full matrix, caching, release workflow, artifact policy.
- Tasks:
  - implement `ci.yml` and `release.yml`.
  - add smoke step: install local packed tool and run generate against fixture.
  - enforce parity gate before publish.
- Files/projects touched:
  - `.github/workflows/ci.yml`
  - `.github/workflows/release.yml`
  - `eng/versioning.props` (or equivalent)
- Acceptance tests (exact):
  - CI matrix green across `windows-latest`, `ubuntu-latest`, `macos-latest`.
  - release dry-run publishes package to local feed and installs successfully.
- Definition of Done:
  - tagged release can publish `dotnet tool` package without manual intervention.
- Risks and mitigations:
  - publish regressions -> keep release workflow isolated and tag-gated.

## M9 - Performance and caching hardening
- Goal:
  - validate runtime on larger solution shapes and prevent CI timeout risk.
- Scope:
  - caching policies and measurable performance thresholds.
- Tasks:
  - implement per-invocation cache for compilations/metadata.
  - capture stage timings (`load`, `metadata`, `render`, `write`).
  - add large-solution fixture and performance tests.
- Files/projects touched:
  - `src/Typewriter.Application/Performance/**`
  - `tests/Typewriter.PerformanceTests/**`
- Acceptance tests (exact):
  - `PerformanceTests.LargeSolution_CompletesUnderThreshold`
  - memory and timing budget assertions reported in CI.
- Definition of Done:
  - performance budgets documented and enforced by CI non-regression checks.
- Risks and mitigations:
  - overfitting benchmarks -> maintain representative fixture diversity.
# 10. Test Strategy (Parity + Regression)

## 10.1 Unit tests
- Focus:
  - parsing helpers,
  - type mapping,
  - output path policy,
  - diagnostic formatting,
  - config precedence,
  - include resolution policy.
- Key suites:
  - `TemplatePreprocessTests`
  - `ItemFilterTests`
  - `TypeMappingTests`
  - `OutputPathPolicyTests`
  - `DiagnosticsContractTests`

## 10.2 Integration tests
- Focus:
  - real MSBuild project loading,
  - semantic extraction,
  - full generate command execution on fixtures.
- Fixture plan:
  - `tests/fixtures/csproj-basic`
  - `tests/fixtures/solution-sln`
  - `tests/fixtures/solution-slnx`
  - `tests/fixtures/multi-target`
  - `tests/fixtures/source-generators`

## 10.3 Golden tests (upstream parity)
- Compare generated outputs to expected baselines derived from upstream-style fixtures.
- Include diagnostics snapshot checks for known failure scenarios.
- Fail on:
  - content diffs,
  - path diffs,
  - encoding diffs,
  - unordered output sets.

## 10.4 Cross-platform matrix
- Required matrix:
  - Windows latest,
  - Ubuntu latest,
  - macOS latest.
- Required SDK:
  - `10.0.x` pinned via `global.json`.

## 10.5 Fixture repositories plan
- `fixtures/parity-core`: direct parity scenarios.
- `fixtures/parity-edge`: ambiguous project names, partial combined render, long paths.
- `fixtures/runtime-edge`: assembly load context and `#reference` dependency chains.
- `fixtures/perf-large`: high project count and multi-target graph.

# 11. CI/CD Plan

## 11.1 Workflow design

### `ci.yml`
- Triggers:
  - pull requests,
  - pushes to main.
- Jobs:
  1. `build-test` matrix by OS.
  2. `golden-parity` matrix by OS.
  3. `tool-smoke` matrix by OS.
- Steps per job:
  - checkout,
  - setup dotnet `10.0.x`,
  - restore,
  - build (`Release`),
  - run unit/integration/golden tests,
  - pack tool,
  - install tool from local artifact,
  - smoke generate command.

### `release.yml`
- Trigger:
  - tag `v*`.
- Steps:
  - rerun full matrix gate,
  - pack signed release artifacts,
  - publish NuGet package for `dotnet tool`,
  - attach artifacts to release.

## 11.2 Cache strategy
- Cache NuGet packages keyed by:
  - OS + SDK version + lockfile hash.
- Do not cache generated outputs or mutable fixture artifacts between jobs.

## 11.3 Restore/build/generate verification
- Mandatory stage order:
  1. restore,
  2. build,
  3. tests,
  4. generate parity smoke,
  5. package.
- `--restore` path is validated in integration smoke but not relied on for CI determinism.

## 11.4 Release strategy for `dotnet tool`
- Primary package: `Typewriter.Cli` as global/local tool.
- Optional post-v1: self-contained binaries for SDK-less environments.

## 11.5 Versioning approach
- SemVer:
  - `0.x` during parity stabilization,
  - `1.0.0` when parity gates are consistently green.
- Tag-driven release:
  - only tagged commits publish.
- Pre-release channels:
  - `-alpha`, `-rc` as needed for validation cycles.

# 12. Risk Register (Consolidated)

| Risk | Likelihood | Impact | Mitigation Plan | Early Warning Signals |
|---|---|---|---|---|
| Semantic fidelity regression | Medium | High | port metadata tests early; golden parity gates | mismatch in metadata assertions across OS |
| SDK/restore/load nondeterminism | Medium | High | explicit load stages; SDK logging; exit code 3 mapping | intermittent `TW2xxx` in CI without code changes |
| Template assembly load failure on Linux/macOS | Medium | High | dedicated `AssemblyLoadContext` + resolver tests | `#reference` templates fail only on non-Windows |
| Source generator outputs missing | Medium | Medium | generator fixture and validation in M5 | generated symbols absent in metadata logs |
| IncludeProject ambiguity in monorepos | Medium | High | explicit ambiguity policy with deterministic errors | project selection differs by environment |
| Partial combined render callback mismatch | Medium | High | deterministic request queue + bounded session cap + scope boundary + regression tests/log validation | partial-type outputs incomplete after changes or enqueue growth without convergence |
| Large-solution performance degradation | Medium | Medium | caching + perf tests + thresholds | runtime trend increases across commits |
| Drift from rewritten components | Medium | High | reuse-first policy + path ownership + parity gates | repeated golden diffs in same features |
| Security/trust risks from `#reference` loading | Low | High | allowlist/safe-path policy option, clear docs for untrusted templates | loading arbitrary binaries in CI contexts |
| Output determinism issues (EOL/encoding/path order) | Medium | Medium | deterministic ordering and normalization policy in tests | platform-specific snapshot diffs |
| User adoption friction from no project mutation | Medium | Medium | document mitigation, optional post-v1 feature flag | repeated feature requests/issues for csproj updates |
# 13. Open Questions and Resolution Plan

## Resolved Decision (Q1)
- Q1 `IncludeProject(name)` ambiguity is resolved as of 2026-02-21: if more than one project matches by name, fail with `TW12xx` and require a path-qualified selector (or a unique match). Rationale and source references: `_archive/Q1-include-project-name-ambiguity-decision.md`.

## Open Questions (Remaining)
| ID | Question | Resolution Method | Target Milestone | Decision Rule |
|---|---|---|---|---|
| Q2 | How is upstream `requestRender` callback mirrored in batch mode? | deterministic render-session queue with dedupe/order + partial combined regression fixture with cross-file declarations + scope/cap/log validation (design notes: `_archive/Q2-request-render-batch-mode-resolution-notes.md`) | M5 | outputs must match baseline without duplicate renders; callback enqueues must stay in scope and converge within configured safety cap |
| Q3 | Should v1 mutate project files? | usage analysis + spike in SDK-style csproj edit safety | M8 | default no; only enable with explicit opt-in in post-v1 unless a blocker is found |
| Q4 | Are source-generated symbols visible in workspace pipeline? | generator fixture + compile inspection | M5 | if missing, add generator execution fallback or document unsupported scenario |
| Q5 | Is `.slnx` fallback needed in practice? | force-failure simulation in loader tests | M4 | keep fallback only if it demonstrates real recovery value |
| Q6 | Watch mode in v1 or post-v1? | assess scope impact after parity complete | M9 | keep deferred unless required for release criteria |

# 14. Appendices

## A. Reuse Map (Upstream -> New Projects)

| Upstream Path/Module | New Location | Classification | Reason |
|---|---|---|---|
| `origin/src/CodeModel/*` | `src/Typewriter.CodeModel/*` | as-is | VS-independent abstractions/extensions |
| `origin/src/Metadata/*` | `src/Typewriter.Metadata/*` | as-is | pure interfaces/contracts |
| `origin/src/Roslyn/RoslynClassMetadata.cs` and peers | `src/Typewriter.Metadata.Roslyn/*` | as-is | Roslyn symbol wrappers remain valid |
| `origin/src/Roslyn/RoslynVoidTaskMetadata.cs` | `src/Typewriter.Metadata.Roslyn/RoslynVoidTaskMetadata.cs` | as-is | synthetic type behavior unchanged |
| `origin/src/Roslyn/RoslynMetadataProvider.cs` | `src/Typewriter.Metadata.Roslyn/RoslynMetadataProvider.cs` | rewrite | replace `VisualStudioWorkspace` coupling |
| `origin/src/Roslyn/RoslynFileMetadata.cs` | `src/Typewriter.Metadata.Roslyn/RoslynFileMetadata.cs` | adapt | remove `ThreadHelper`; keep logic |
| `origin/src/Typewriter/CodeModel/Implementation/*` | `src/Typewriter.CodeModel/Implementation/*` | as-is | metadata-to-model mapping core |
| `origin/src/Typewriter/CodeModel/Collections/*` | `src/Typewriter.CodeModel/Collections/*` | as-is | collection wrappers are host-agnostic |
| `origin/src/Typewriter/CodeModel/Helpers.cs` | `src/Typewriter.CodeModel/Helpers.cs` | as-is | type mapping core |
| `origin/src/CodeModel/Extensions/*` | `src/Typewriter.CodeModel/Extensions/*` | as-is | host-agnostic helpers |
| `origin/src/Typewriter/Generation/TemplateCodeParser.cs` | `src/Typewriter.Generation/TemplateCodeParser.cs` | adapt | `ProjectItem` removal |
| `origin/src/Typewriter/TemplateEditor/Lexing/Roslyn/ShadowClass.cs` | `src/Typewriter.Generation/ShadowClass.cs` | adapt | relocation + net10 compile context |
| `origin/src/Typewriter/Generation/Compiler.cs` | `src/Typewriter.Generation/Compiler.cs` | adapt | ALC and diagnostic sink migration |
| `origin/src/Typewriter/Generation/Parser.cs` | `src/Typewriter.Generation/Parser.cs` | as-is | parser behavior parity required |
| `origin/src/Typewriter/Generation/SingleFileParser.cs` | `src/Typewriter.Generation/SingleFileParser.cs` | as-is | same reason |
| `origin/src/Typewriter/Generation/ItemFilter.cs` | `src/Typewriter.Generation/ItemFilter.cs` | as-is | same filter semantics |
| `origin/src/Typewriter/Generation/Template.cs` | `src/Typewriter.Generation/Template.cs` | adapt | preserve output behavior, remove DTE mutation |
| `origin/src/Typewriter/CodeModel/Configuration/SettingsImpl.cs` | `src/Typewriter.CodeModel/Configuration/CliSettingsImpl.cs` | rewrite | DTE project resolution replaced by graph context |
| `origin/src/Typewriter/CodeModel/Configuration/ProjectHelpers.cs` | `src/Typewriter.Loading.MSBuild/ProjectInclusionPolicy.cs` | rewrite | DTE helpers replaced by graph identity policy |
| `origin/src/Typewriter/Generation/Controllers/*` | `src/Typewriter.Application/Orchestration/*` | rewrite | event-driven VS model replaced by command pipeline |
| `origin/src/Typewriter/VisualStudio/ErrorList.cs` | `src/Typewriter.Cli/Diagnostics/*` | rewrite | console diagnostics contract |
| `origin/src/Typewriter/VisualStudio/Log.cs` | `src/Typewriter.Cli/Diagnostics/*` | rewrite | structured logging |
| `origin/src/Typewriter/VisualStudio/*` (host lifecycle) | none | dropped | VSIX runtime removed |
| `origin/src/Typewriter/TemplateEditor/*` | none (v1) | dropped | editor features out of CLI scope |
| `origin/src/Tests/CodeModel/*` | `tests/Typewriter.UnitTests/Parity/CodeModel/*` | adapt | port test assertions to xUnit net10 |
| `origin/src/Tests/Render/*` | `tests/Typewriter.GoldenTests/Parity/Render/*` | adapt | baseline/golden harness migration |

## B. Decision Log (Final)

| Decision | Codex Position | Claude Position | Technical Evaluation | Final Choice | Confidence (1-5) | Validation Plan |
|---|---|---|---|---|---:|---|
| Loader architecture | Graph-first hybrid | MSBuildWorkspace-centric hybrid | Graph-first gives better traversal control and avoids parser ownership risk | Graph + Roslyn hybrid | 5 | M3/M4 integration tests |
| `.slnx` strategy | native graph support | custom parser | empirical spike supports native path | native first, fallback only | 5 | M4 parity test `.sln` vs `.slnx` |
| Diagnostics format | human-readable focus | MSBuild parseable with `TW` codes | parseable diagnostics are stronger for CI/tooling | adopt MSBuild format + codes | 5 | M2 snapshot tests |
| Template assembly loading | less explicit risk | explicit ALC risk/mitigation | ALC risk is real on .NET 10 | dedicated `TemplateAssemblyLoadContext` | 4 | M6 cross-platform assembly tests |
| Source generator handling | implicit | explicit open question | unresolved and potentially user-visible | treat as open question with fixture | 4 | M5 generator fixture |
| IncludeProject ambiguity | explicit risk -> resolved policy | largely omitted | ambiguity can silently misrender outputs | explicit `TW12xx` policy + path-qualified selector requirement (resolved 2026-02-21) | 4 | M5 ambiguity tests |
| Partial `requestRender` mapping | explicit risk -> constrained resolution | omitted | parity-critical in combined partial scenarios | deterministic batch equivalent with dedupe, scope boundary, safety cap, and detailed enqueue logs | 4 | M5 partial regression tests |
| Module granularity | 7 modules (clean separation) | fewer modules (simpler) | both valid; avoid thin-project overengineering while preserving boundaries | balanced 7 core + clear ownership | 3 | M0/M1 review after first vertical slice |
| CLI input UX | richer infra flags | better template glob UX | combine both strengths | positional templates + rich infra flags | 5 | M2 CLI contract tests |
| Multi-target default | broader graph handling | first-TFM default | first-TFM default is safer for deterministic output; explicit override remains | default first TFM + `--framework` | 4 | M3 multi-target fixtures |

### Divergence references used in final choices
- `_archive/D-0003-project-loading-strategy_COMPARISON_CLAUDE.md`: Divergence #1, #2, #4.
- `_archive/PR-0001-msbuild-loading-spike_COMPARISON_CLAUDE.md`: Divergence #1, #3.
- `_archive/IMPLEMENTATION_PLAN_COMPARISON_CLAUDE.md`: Divergence #5, #6, #7.
- `_archive/questions-folder_COMPARISON_CLAUDE.md`: Divergence #2, #4, #5.
- `_archive/risks-folder_COMPARISON_CLAUDE.md`: Divergence #3, #5.

