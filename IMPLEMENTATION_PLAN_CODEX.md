# 1. Overview
This plan defines a production-grade migration of upstream Typewriter from a Visual Studio extension host to a cross-platform CLI (`typewriter-cli`) on modern .NET (`net10.0` baseline), with no Visual Studio runtime dependency.

Summary of approach:
- Standardize implementation/runtime baseline on `net10.0` and SDK `10.0.x`.
- Keep generation semantics and template language behavior parity with upstream.
- Replace host/runtime coupling (VSIX, DTE, IVs services, MEF editor features) with explicit CLI orchestration.
- Use a hybrid loading design: MSBuild project graph for deterministic traversal and Roslyn semantic loading for metadata fidelity.
- Ship primarily as a `dotnet tool` and validate in Linux/macOS/Windows CI.

Expected deliverables after implementation:
- `typewriter-cli` executable command via NuGet `dotnet tool`.
- Integration and golden test suites for parity-critical generation behavior.
- CI workflows validating restore/build/generate across supported OS and input types (`.csproj`, `.sln`, `.slnx`).

# 2. Goals and Non-goals
Goals:
- Use `net10.0` as the primary target framework for all CLI solution projects.
- Cross-platform CLI generation workflow with deterministic output and exit codes.
- No dependency on `Microsoft.VisualStudio.*`, `EnvDTE`, COM, registry, or VSIX runtime.
- Robust input loading for `.csproj`, `.sln`, `.slnx` including `global.json`, `Directory.Build.props/targets`, and multi-targeting.
- Feature parity for core template discovery/execution, metadata model, output naming/writing, and diagnostics quality.
- CI-first behavior with clear restore/load failure diagnostics.

Non-goals:
- Re-creating Visual Studio editor UX features (completion, classification, quick info, context menus).
- Requiring project-file mutation parity in v1 (add generated files, custom project-item metadata) unless explicitly scoped later.
- Shipping watch mode in initial CI-focused release unless scope decision reverses `Q-0003`.

# 3. Upstream Architecture Summary
Repository layout and key projects (inventory in `.ai/findings/F-0001-solution-and-project-inventory.md`):
- `origin/src/Typewriter`: VS extension host and generation orchestration.
- `origin/src/CodeModel`: template-facing settings and model abstractions.
- `origin/src/Metadata`: metadata interfaces.
- `origin/src/Roslyn`: Roslyn-backed metadata implementation.
- `origin/src/Tests`: parity signals for metadata/rendering behavior.

Runtime entrypoint and lifecycle:
- Visual Studio package startup in `origin/src/Typewriter/VisualStudio/ExtensionPackage.cs` (`InitializeAsync`, event wiring, VS service resolution), documented in `.ai/findings/F-0002-extension-entrypoint-and-host-lifecycle.md`.

Generation pipeline:
- Event-driven queue model in `GenerationController` + `EventQueue` + `TemplateController` (`.ai/findings/F-0003-generation-pipeline-orchestration.md`).
- Two-phase template processing: compile pre-pass (`TemplateCodeParser`) and runtime parser expansion (`Parser`, `SingleFileParser`) in `.ai/findings/F-0004-template-discovery-and-execution.md`.

Configuration model:
- Template `Settings` API plus VS options page flags (`RenderOnSave`, `TrackSourceFiles`, `AddGeneratedFilesToProject`) in `.ai/findings/F-0006-configuration-sources-and-precedence.md`.

Semantic model extraction:
- Roslyn symbols via `VisualStudioWorkspace` (`RoslynMetadataProvider`) with parity-critical metadata behavior in `.ai/findings/F-0007-semantic-model-extraction-and-fidelity.md`.

# 4. Visual Studio Dependency Map (with replacement plan)
Dependency inventory:

| Dependency Surface | Upstream location | Classification | CLI replacement |
|---|---|---|---|
| VSIX packaging and VSSDK build targets | `origin/src/Typewriter/Typewriter.csproj`, `origin/src/Typewriter/source.extension.vsixmanifest` | Hard | SDK-style CLI projects + `dotnet tool` packaging |
| `EnvDTE` solution/project/file traversal | `origin/src/Typewriter/Generation/Controllers/SolutionExtensions.cs`, `origin/src/Typewriter/CodeModel/Configuration/ProjectHelpers.cs` | Hard | `ProjectGraph` + evaluated items + graph identity mapping |
| VS services and runtime events (`IVsSolution*`, running doc table, track docs) | `origin/src/Typewriter/Generation/Controllers/SolutionMonitor.cs` | Hard | Explicit command execution; optional later file watcher |
| UI-thread/JTF orchestration | `origin/src/Typewriter/VisualStudio/ExtensionPackage.cs`, `EventQueue.cs` | Hard | Plain async pipeline with deterministic ordering |
| Output window / status bar / Error List | `origin/src/Typewriter/VisualStudio/Log.cs`, `ErrorList.cs` | Soft | Structured console logging, verbosity levels, machine-readable diagnostics |
| Template editor MEF/language services | `origin/src/Typewriter/TemplateEditor/*`, `VisualStudio/LanguageService.cs` | Soft | Out of CLI scope |
| COM/registry and Windows shell integration | `origin/src/Typewriter/VisualStudio/ContextMenu/RenderTemplate.cs`, `Template.cs`, `ExtensionPackage.cs` | Hard/Soft mix | Remove COM/registry dependencies; use cross-platform filesystem behavior |
| Project-item mutation (`AddFromFile`, `CustomToolNamespace`, DTE source control checkout) | `origin/src/Typewriter/Generation/Template.cs` | Accidental/Soft | Defer or explicit optional scope; mitigation via SDK-style glob includes and VCS workflow |

Detailed evidence: `.ai/findings/F-0009-vs-sdk-vsix-dependency-cluster.md`, `.ai/findings/F-0010-envdte-and-solution-object-model-cluster.md`, `.ai/findings/F-0011-vs-services-events-and-threading-cluster.md`, `.ai/findings/F-0012-mef-editor-language-service-cluster.md`, `.ai/findings/F-0013-com-registry-and-windows-assumptions-cluster.md`.

# 5. MSBuild & Project Loading Design (sln, slnx, csproj)
Inputs:
- `typewriter-cli generate <input>` where `<input>` is `.csproj`, `.sln`, or `.slnx`.
- Input path is resolved to absolute path before loading; unknown extensions are argument errors (exit code `2`).

Loading strategy decision:
- Decision: hybrid loading (see `.ai/decisions/D-0003-project-loading-strategy.md`).
- Stage A: `ProjectGraph` for deterministic traversal, target selection, and project relationship handling.
- Stage B: Roslyn semantic loading for parity-level metadata fidelity used by templates.
- Evidence: `.ai/prototypes/PR-0001-msbuild-loading-spike.md` (graph support for `.csproj`/`.sln`/`.slnx`, multi-target behavior, `global.json` and restore outcomes).

Restore and SDK resolution:
- `global.json` is honored by the .NET SDK/MSBuild process; incompatible SDK pins fail load pipeline with exit code `3`.
- MSBuild discovery:
1. Prefer SDK-coherent host process defaults.
2. Use `MSBuildLocator` when explicit `--msbuild-path` is provided or detection requires override.
3. Log resolved SDK/MSBuild version at detailed verbosity.
- Restore behavior:
1. Default: no implicit restore.
2. `--restore` triggers restore before graph/semantic load.
3. Missing assets (for example `NETSDK1004`) map to exit code `3` with actionable message.
- CI determinism:
1. Recommend pinned SDK via `global.json`.
2. Restore and generation run with explicit configuration/framework where needed.

Multi-targeting and configuration:
- Default `Configuration`: inferred (prefer `Debug` when not supplied, with clear logging).
- `--framework <TFM>` sets global property `TargetFramework` to constrain graph and semantic load.
- Without `--framework` on multi-target projects, tool evaluates all target graph nodes and renders deterministically per template/file policy.
- `--runtime <RID>` sets `RuntimeIdentifier` global property for evaluation consistency where relevant.

# 6. CLI UX Spec (commands, flags, exit codes)
Commands:
- `typewriter-cli generate [options] <input>`

Flags:
- `--configuration <Debug|Release>` default inferred.
- `--framework <TFM>` optional.
- `--runtime <RID>` optional.
- `--msbuild-path <path>` optional explicit MSBuild override.
- `--restore` optional pre-load restore.
- `--output <dir>` optional output root override.
- `--verbosity <quiet|normal|detailed>` default `normal`.
- `--fail-on-warnings` optional; warnings elevate outcome to exit code `1`.

Exit code contract:
- `0`: success (generation complete, no elevated warnings).
- `1`: generation/template/semantic errors or warnings elevated by `--fail-on-warnings`.
- `2`: invalid args/input resolution errors.
- `3`: restore/load/build/SDK/MSBuild errors.

Error output contract:
- Human-readable single-line summary on stderr.
- Detailed diagnostics with file path, line/column, template/source context on `--verbosity detailed`.

# 7. Feature Parity Matrix (link to `.ai/parity/`)
Canonical matrix:
- `.ai/parity/P-0001-feature-matrix.md`

Current parity posture:
- Core generation, parser semantics, metadata model, output naming/writing: planned identical parity.
- Partial parity candidates: project include-by-name ambiguity (`Q-0001`), trigger/watch behavior (`Q-0003`), project mutation/source mapping (`Q-0002`), combined partial render callback mapping (`Q-0004`).
- Explicit non-planned VS-only features: editor MEF/language services, registry icon integration, DTE source-control checkout.

Mitigations for intentional gaps:
- CLI diagnostics clearly call out unsupported VS-only behaviors.
- Documentation provides migration alternatives (SDK-style wildcard includes, explicit pipeline steps, external VCS workflow).

# 8. Target Architecture (modules, APIs, boundaries)
Proposed solution layout:
- `src/Typewriter.Cli`
- `src/Typewriter.Application`
- `src/Typewriter.Loading.MSBuild`
- `src/Typewriter.Metadata.Roslyn`
- `src/Typewriter.Generation`
- `src/Typewriter.Configuration`
- `src/Typewriter.Diagnostics`
- `tests/Typewriter.UnitTests`
- `tests/Typewriter.IntegrationTests`
- `tests/Typewriter.GoldenTests`

Module responsibilities:
- `Typewriter.Cli`: command parsing, process exit mapping.
- `Typewriter.Application`: orchestration pipeline (`GenerateCommandHandler`).
- `Typewriter.Loading.MSBuild`: input resolution, graph creation, restore coordination.
- `Typewriter.Metadata.Roslyn`: compilation/document/symbol extraction.
- `Typewriter.Generation`: template discovery, compile pre-pass, runtime parser execution, output planning.
- `Typewriter.Configuration`: merge of CLI args, template settings, defaults.
- `Typewriter.Diagnostics`: logger abstraction + structured diagnostics DTOs.

Key interfaces:
- `IInputResolver`
- `IProjectGraphLoader`
- `ISemanticModelProvider`
- `ITemplateDiscoveryService`
- `ITemplateCompiler`
- `ITemplateRenderer`
- `IOutputPathStrategy`
- `IOutputWriter`
- `IDiagnosticSink`
- `IRestoreCoordinator`

Data flow:
1. Parse command/options.
2. Resolve and validate input.
3. Optionally restore.
4. Build project graph with global properties.
5. Build/load Roslyn semantic context for selected projects/TFM.
6. Discover and compile templates.
7. Render per-file/single-file outputs.
8. Apply write policy (collision, unchanged skip, BOM, output override).
9. Emit diagnostics and deterministic exit code.

# 9. Implementation Phases (milestones + acceptance criteria)
Phase 0 - Repo bootstrap and contracts:
- Scope: create solution skeleton, module boundaries, diagnostic/error contracts, test project scaffolding.
- Tasks: define shared abstractions and DTOs; wire command surface and no-op pipeline; enforce `net10.0` TFM baseline in all new projects.
- Acceptance tests: CLI returns exit code `2` on invalid input, `0` on dry valid no-op fixture.
- Done when: baseline build/test passes on one OS.

Phase 1 - CLI skeleton, config parsing, diagnostics:
- Scope: implement `generate` command parsing, verbosity, exit mapping.
- Tasks: option model, validation, standardized diagnostic sink, fail-on-warnings handling.
- Acceptance tests: flag parsing matrix; deterministic exit code tests for synthetic failures.
- Done when: contract-level tests pass and output format is stable.

Phase 2 - MSBuild loader for `.csproj`:
- Scope: implement graph loading for project input and optional restore.
- Tasks: `IInputResolver`, `IProjectGraphLoader`, restore coordinator, SDK/MSBuild detection logging.
- Acceptance tests: fixture `.csproj` loads with and without `--framework`; `--no-restore` missing-assets case maps to exit `3`.
- Done when: `.csproj` integration tests pass on Windows + Linux.

Phase 3 - `.sln` support:
- Scope: load traversal/order for legacy solution format.
- Tasks: solution input path pipeline, project selection, deterministic template order.
- Acceptance tests: `.sln` fixture with multiple projects/references generates expected outputs.
- Done when: `.sln` parity fixture passes cross-platform.

Phase 4 - `.slnx` support:
- Scope: solutionx input parity with `.sln`.
- Tasks: `.slnx` parser/load path in graph pipeline; diagnostics for unsupported SDK edge cases.
- Acceptance tests: same fixture set as phase 3 in `.slnx` form produces identical outputs.
- Done when: `.sln` and `.slnx` golden outputs match.

Phase 5 - Roslyn semantic model parity:
- Scope: implement metadata provider equivalents for classes, interfaces, enums, records, nullability, tuples, task unwrap, partial modes.
- Tasks: port/adapt metadata mapping logic and caching strategy.
- Acceptance tests: migrated metadata tests mirroring upstream `src/Tests/CodeModel` and `src/Tests/Metadata/Roslyn`.
- Done when: parity-critical metadata tests pass.

Phase 6 - Template execution and output parity:
- Scope: template compile pre-pass and parser runtime semantics, plus output path/write rules.
- Tasks: `#reference`/`${}`/lambda rewrite, item filter behavior, single-file mode, collision naming, unchanged write skip, BOM behavior.
- Acceptance tests: golden rendering suites including single-file, partial/combined mode, diagnostics snapshots.
- Done when: parity matrix rows tagged identical have passing tests.

Phase 7 - Parity gaps and performance hardening:
- Scope: resolve/decide partial parity items and optimize large-solution behavior.
- Tasks: implement final policy for `IncludeProject(name)` ambiguity; decide project mutation scope; add batching/caching/perf counters.
- Acceptance tests: large fixture runtime threshold checks and open-question closure tests.
- Done when: open parity gap decisions are documented and tested.

Phase 8 - Packaging and CI release readiness:
- Scope: tool packaging, install/invoke validation, multi-OS workflows.
- Tasks: NuGet tool packaging metadata, publish workflow, end-to-end smoke tests.
- Acceptance tests: `dotnet tool install --local` + `typewriter-cli generate` smoke on Linux/macOS/Windows.
- Done when: release candidate pipeline is green on full matrix.

# 10. Testing Strategy (unit/integration/golden tests)
Unit tests:
- Parser/token/filter semantics.
- Output naming/collision/unchanged-write/BOM rules.
- Exit code and diagnostics mapping.
- Option precedence and configuration merge behavior.

Integration tests:
- Real fixtures for `.csproj`, `.sln`, `.slnx`.
- Restore/no-restore scenarios.
- Multi-target fixtures with and without explicit `--framework`.
- Include-project/reference/include-all graph behavior.

Golden tests:
- Input C# + template fixtures with expected generated files.
- Metadata-sensitive outputs (nullable, records, generics, partials, attributes, tuples/tasks).
- Diagnostic snapshots for compile and parse failures.

Cross-platform matrix:
- Windows latest, Ubuntu latest, macOS latest.
- Pinned SDK `10.0.x` via `global.json` in test fixtures for deterministic behavior.

# 11. CI/CD Plan (restore/build/generate verification)
Workflow stages:
1. Checkout + .NET 10 SDK setup honoring repo `global.json`.
2. Restore solution and test projects.
3. Build all projects in Release.
4. Run unit tests.
5. Run integration/golden tests.
6. Run end-to-end CLI smoke generation on fixture inputs (`.csproj`, `.sln`, `.slnx`).
7. Pack tool package.
8. On release tag: publish NuGet package and attach build artifacts.

Caching strategy:
- NuGet package cache keyed by OS + SDK version + lockfiles.
- Optional test fixture cache only for immutable assets.

Verification gates:
- Fail pipeline on any generation diff in golden tests.
- Fail pipeline if diagnostics contract snapshot changes without explicit approval.

# 12. Risk Register (top risks + mitigations)
| Risk | Impact | Likelihood | Mitigation | Tracking |
|---|---|---|---|---|
| Semantic fidelity regression in metadata extraction | High | Medium | Preserve Roslyn-centric model provider; port parity tests early; golden regression suites | `.ai/risks/R-0001-semantic-fidelity-regression-risk.md` |
| Load/restore nondeterminism due SDK/workload state | High | Medium | Explicit staged load pipeline, `--restore` behavior, SDK/MSBuild diagnostics, CI SDK pinning | `.ai/risks/R-0002-msbuild-load-and-restore-determinism-risk.md` |
| Project mutation parity gap affects users expecting project-file updates | Medium | Medium | Scope decision + documented mitigation; optional future explicit flag | `.ai/risks/R-0003-project-mutation-parity-gap-risk.md` |
| Large graph and multi-target solutions cause slow generation | Medium | Medium | Caching, framework filtering, profiling fixtures, measured thresholds in CI | `.ai/risks/R-0004-large-solution-performance-risk.md` |

# 13. Open Questions (must link to `.ai/questions/`)
- Include-project resolution policy for duplicate names: `.ai/questions/Q-0001-include-project-resolution-policy.md`
- Project mutation parity scope in v1: `.ai/questions/Q-0002-project-mutation-parity-scope.md`
- Watch mode in or out of v1: `.ai/questions/Q-0003-watch-mode-vs-one-shot-scope.md`
- Deterministic CLI equivalent for `requestRender` in combined partial mode: `.ai/questions/Q-0004-partial-rendering-request-render-equivalence.md`

# 14. Appendix (key references to upstream files/symbols)
Key upstream references:
- Host/runtime entrypoint: `origin/src/Typewriter/VisualStudio/ExtensionPackage.cs`
- Generation orchestration: `origin/src/Typewriter/Generation/Controllers/GenerationController.cs`, `origin/src/Typewriter/Generation/Controllers/EventQueue.cs`, `origin/src/Typewriter/Generation/Controllers/TemplateController.cs`
- Template execution: `origin/src/Typewriter/Generation/TemplateCodeParser.cs`, `origin/src/Typewriter/Generation/Parser.cs`, `origin/src/Typewriter/Generation/SingleFileParser.cs`, `origin/src/Typewriter/Generation/Template.cs`
- Settings/config: `origin/src/CodeModel/Configuration/Settings.cs`, `origin/src/Typewriter/CodeModel/Configuration/SettingsImpl.cs`, `origin/src/Typewriter/VisualStudio/TypewriterOptionsPage.cs`
- Semantic metadata: `origin/src/Roslyn/RoslynMetadataProvider.cs`, `origin/src/Roslyn/RoslynFileMetadata.cs`, `origin/src/Roslyn/RoslynClassMetadata.cs`, `origin/src/Roslyn/RoslynTypeMetadata.cs`
- Diagnostics surfaces: `origin/src/Typewriter/VisualStudio/Log.cs`, `origin/src/Typewriter/VisualStudio/ErrorList.cs`

Supporting analysis artifacts:
- Findings index: `.ai/00_INDEX.md`
- Full findings set: `.ai/findings/F-0001-solution-and-project-inventory.md` through `.ai/findings/F-0015-buildalyzer-and-msbuild-usage-gap.md`
- Loading spike: `.ai/prototypes/PR-0001-msbuild-loading-spike.md`
- Decisions: `.ai/decisions/D-0001-target-framework.md`, `.ai/decisions/D-0002-packaging-strategy.md`, `.ai/decisions/D-0003-project-loading-strategy.md`
- Parity matrix: `.ai/parity/P-0001-feature-matrix.md`
