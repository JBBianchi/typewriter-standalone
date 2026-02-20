# Analysis Index

- Date: 2026-02-19
- Scope: Typewriter CLI spin-off analysis and planning (no implementation)

## Findings
- [F-0001](findings/F-0001-solution-and-project-inventory.md) Inventory of upstream solution/projects and framework baselines.
- [F-0002](findings/F-0002-extension-entrypoint-and-host-lifecycle.md) VS package entrypoint and runtime orchestration model.
- [F-0003](findings/F-0003-generation-pipeline-orchestration.md) Event-queued generation controller flow and trigger handling.
- [F-0004](findings/F-0004-template-discovery-and-execution.md) Template discovery/compile/parse execution model.
- [F-0005](findings/F-0005-output-path-and-file-io-rules.md) Output path, write, collision, and long-path behavior.
- [F-0006](findings/F-0006-configuration-sources-and-precedence.md) Configuration sources and effective precedence surfaces.
- [F-0007](findings/F-0007-semantic-model-extraction-and-fidelity.md) Roslyn symbol extraction model and fidelity constraints.
- [F-0008](findings/F-0008-diagnostics-and-error-surface.md) Diagnostics surfaces and error reporting behavior.
- [F-0009](findings/F-0009-vs-sdk-vsix-dependency-cluster.md) VS SDK/VSIX dependency cluster classification and replacements.
- [F-0010](findings/F-0010-envdte-and-solution-object-model-cluster.md) EnvDTE project/file model dependency cluster.
- [F-0011](findings/F-0011-vs-services-events-and-threading-cluster.md) VS services/events/JTF dependency cluster.
- [F-0012](findings/F-0012-mef-editor-language-service-cluster.md) MEF editor/language-service dependency cluster.
- [F-0013](findings/F-0013-com-registry-and-windows-assumptions-cluster.md) COM/registry/Windows-only assumptions cluster.
- [F-0014](findings/F-0014-tests-and-parity-signals.md) Test-derived parity signals and coverage requirements.
- [F-0015](findings/F-0015-buildalyzer-and-msbuild-usage-gap.md) Buildalyzer/MSBuild runtime usage gap in upstream.

## Decisions
- [D-0001](decisions/D-0001-target-framework.md) Adopt `net10.0` as CLI target framework baseline.
- [D-0002](decisions/D-0002-packaging-strategy.md) Package as `dotnet tool` primary distribution.
- [D-0003](decisions/D-0003-project-loading-strategy.md) Use hybrid loading: `ProjectGraph` + Roslyn semantic loading.

## Questions
- [Q-0001](questions/Q-0001-include-project-resolution-policy.md) Policy for resolving `IncludeProject(name)` in graph-based CLI.
- [Q-0002](questions/Q-0002-project-mutation-parity-scope.md) Scope decision for project file mutation parity.
- [Q-0003](questions/Q-0003-watch-mode-vs-one-shot-scope.md) Whether watch mode is in v1 scope or deferred.
- [Q-0004](questions/Q-0004-partial-rendering-request-render-equivalence.md) CLI equivalent of partial rendering `requestRender` callback.

## Prototypes
- [PR-0001](prototypes/PR-0001-msbuild-loading-spike.md) MSBuild loading spike for `.csproj`/`.sln`/`.slnx`, multi-targeting, restore/global.json behavior.

## Risks
- [R-0001](risks/R-0001-semantic-fidelity-regression-risk.md) Roslyn metadata fidelity regression risk.
- [R-0002](risks/R-0002-msbuild-load-and-restore-determinism-risk.md) SDK/restore/load determinism risk.
- [R-0003](risks/R-0003-project-mutation-parity-gap-risk.md) Project-mutation parity gap risk.
- [R-0004](risks/R-0004-large-solution-performance-risk.md) Large-solution performance/memory risk.

## Current Status
- inventory: completed
- dependency map: completed
- parity matrix: completed
- ready for plan: yes
