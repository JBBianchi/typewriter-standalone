# Progress Tracker

> Last touched: 2026-03-02 by Claude (Executor, T011)

## Current State

- **Active milestone**: M1 - Core reuse extraction (CodeModel/Metadata)
- **Status**: Done
- **Blocker**: None
- **Next step**: Begin M2 (CLI contract, diagnostics, configuration precedence)

## Milestone Map

| Milestone | Name | Status | Notes |
|-----------|------|--------|-------|
| M0 | Repo bootstrap and packaging skeleton | Done | All acceptance criteria verified: build, test, pack, tool install |
| M1 | Core reuse extraction (CodeModel/Metadata) | Done | All acceptance criteria verified: build 0 errors, CodeModel 102/102, TypeMapping 71/71, zero VS refs |
| M2 | CLI contract, diagnostics, and configuration precedence | Not started | |
| M3 | MSBuild loading: `.csproj` and restore pipeline | Not started | |
| M4 | MSBuild loading: `.sln` and `.slnx` | Not started | |
| M5 | Semantic model extraction parity | Not started | |
| M6 | Template execution and output management | Not started | |
| M7 | Golden parity and fixture repos | Not started | |
| M8 | CI pipelines and release readiness | Not started | |
| M9 | Performance and caching hardening | Not started | |

## Active Tasks

| Task | Milestone | Agent | Status | Detail |
|------|-----------|-------|--------|--------|
| #8 Create Directory.Build.props | M0 | Executor | Done | Shared TFM, nullable, implicit usings, warnings-as-errors |
| #18 Create global.json with .NET 10 SDK pin | M0 | Executor | Done | SDK 10.0.100, rollForward: latestFeature |
| #19 Create source project .csproj files with dependency graph | M0 | Executor | Done | 7 projects, placeholder classes, full dependency graph |
| #20 Create test project .csproj files with xUnit | M0 | Executor | Done | 4 test projects, xUnit packages, placeholder tests |
| #21 Create Typewriter.Cli.slnx solution file | M0 | Executor | Done | .slnx with all 11 projects in src/tests folders |
| #22 Create .github/workflows/ci.yml | M0 | Executor | Done | Cross-platform CI: restore, build, test, pack on 3 OS matrix |
| #23 Verify M0 acceptance criteria | M0 | Executor | Done | Build/test/pack/tool-install verified; .gitignore added; SDK warnings suppressed |
| #24 Update .ai/progress.md for M0 | M0 | Executor | Done | Progress tracker created with all §14 sections; M0 status documented |
| T001 M1 compat checklist audit | M1 | Executor | Done | [T001-m1-compat-checklist.md](.ai/tasks/T001-m1-compat-checklist.md) — 77/81 files clean, 4 VS-coupled confirmed |
| T002 Port metadata interfaces (#35) | M1 | Executor | Done | [T002-port-metadata-interfaces.md](.ai/tasks/T002-port-metadata-interfaces.md) — 19 I*.cs files in `src/Typewriter.Metadata/Interfaces/` |
| T003 Port IMetadataProvider (#36) | M1 | Executor | Done | [T003-port-imetadataprovider-interface.md](.ai/tasks/T003-port-imetadataprovider-interface.md) — `IMetadataProvider.cs` + minimal `Settings` stub in `src/Typewriter.Metadata/` |
| T004 Port CodeModel collection impls (#37) | M1 | Executor | Done | [T004-port-codemodel-collection-implem.md](.ai/tasks/T004-port-codemodel-collection-implem.md) — 42 abstract types + 19 CollectionImpl files in `src/Typewriter.CodeModel/` |
| T005 Port CodeModel impl files (#38) | M1 | Executor | Done | [T005-port-codemodel-implementation-files.md](.ai/tasks/T005-port-codemodel-implementation-files.md) — Helpers.cs + 19 Implementation files in `src/Typewriter.CodeModel/`; Settings stub extended |
| T006 Port Helpers.cs type-mapping (#39) | M1 | Executor | Done | [T006-port-helpers-type-mapping.md](.ai/tasks/T006-port-helpers-type-mapping.md) — `src/Typewriter.CodeModel/Helpers.cs` already in place from T005; verified all acceptance criteria |
| T007 Stub/rewrite VS-coupled config (#40) | M1 | Executor | Done | [T007-stubrewrite-vs-coupled-config.md](.ai/tasks/T007-stubrewrite-vs-coupled-config.md) — `SettingsImpl.cs` + `ProjectHelpers.cs` created; `Settings.cs` expanded; zero VS refs |
| T008 Port Roslyn metadata wrappers (#41) | M1 | Executor | Done | [T008-port-roslyn-metadata-wrappers.md](.ai/tasks/T008-port-roslyn-metadata-wrappers.md) — 19 files in `src/Typewriter.Metadata.Roslyn/`; `PartialRenderingMode` moved to `Typewriter.Metadata`; minimal `RoslynFileMetadata` stub |
| T009 Add NuGet refs to Typewriter.Metadata.Roslyn (#42) | M1 | Executor | Done | [T009-add-nuget-references-to-typewriter-metadata-roslyn.md](.ai/tasks/T009-add-nuget-references-to-typewriter-metadata-roslyn.md) — refs already present from T008; no file changes needed |
| T010 Add/update unit tests for M1 ported code (#43) | M1 | Executor | Done | [T010-addupdate-unit-tests-for-m1.md](.ai/tasks/T010-addupdate-unit-tests-for-m1.md) — TypeMappingTests (CamelCase, GetTypeScriptName, GetOriginalName, IsPrimitive), CollectionTests (ItemCollectionImpl, ClassCollectionImpl, EnumCollectionImpl, FieldCollectionImpl), RoslynExtensionsTests (GetName, GetFullName, GetNamespace, GetFullTypeName); added Microsoft.CodeAnalysis.CSharp package ref to test project |
| T011 Run M1 acceptance criteria (#44) | M1 | Executor | Done | [T011-run-m1-acceptance-criteria.md](.ai/tasks/T011-run-m1-acceptance-criteria.md) — build 0 errors; CodeModel 102/102; TypeMapping 71/71; zero VS refs confirmed; origin/ unchanged |

## Decisions

| ID | Decision | Date | Context |
|----|----------|------|---------|
| D-0001 | Target framework: `net10.0` everywhere | 2026-02-19 | See `_archive/.ai_CLAUDE/decisions/D-0001-target-framework.md` |
| D-0002 | Primary distribution: `dotnet tool` | 2026-02-19 | See `_archive/.ai_CLAUDE/decisions/D-0002-packaging-strategy.md` |
| D-0003 | Loading architecture: `ProjectGraph` + Roslyn workspace hybrid | 2026-02-19 | See `_archive/.ai_CLAUDE/decisions/D-0003-project-loading-strategy.md` |

## Open Questions

| ID | Question | Raised | Status | Target |
|----|----------|--------|--------|--------|
| Q2 | How is upstream `requestRender` callback mirrored in batch mode? | 2026-02-19 | Design notes in `_archive/Q2-request-render-batch-mode-resolution-notes.md` | M5 |
| Q3 | Should v1 mutate project files? | 2026-02-19 | Default no; revisit post-v1 | M8 |
| Q4 | Are source-generated symbols visible in workspace pipeline? | 2026-02-19 | Open — needs generator fixture | M5 |
| Q5 | Is `.slnx` fallback needed in practice? | 2026-02-19 | Open — needs force-failure simulation | M4 |
| Q6 | Watch mode in v1 or post-v1? | 2026-02-19 | Deferred unless required for release | M9 |

Note: Q1 (`IncludeProject(name)` ambiguity) was resolved — see `_archive/Q1-include-project-name-ambiguity-decision.md`.

## Roadblocks Log

| Date | Description | Resolution | Milestone |
|------|-------------|------------|-----------|
| | | | |

## Patterns & Conventions

- **Milestone details**: Always reference `DETAILED_IMPLEMENTATION_PLAN.md` by milestone ID (M0-M9) for scope, tasks, and acceptance criteria
- **Upstream reference**: `origin/` is read-only; consult for behavior parity but never modify
- **Reuse policy**: "lift first, rewrite last" — copy upstream logic before considering a rewrite (see Section 4 of plan)
- **Diagnostic codes**: `TWxxxx` format, MSBuild-compatible, ranges defined in README.md
- **Archive convention**: Analysis-phase records in `_archive/.ai_CLAUDE/` and `_archive/.ai_CODEX/` use D/F/Q/R/PR/P-xxxx IDs
