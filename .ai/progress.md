# Progress Tracker

> Last touched: 2026-02-26 by Claude

## Current State

- **Active milestone**: M0 - Repo bootstrap and packaging skeleton
- **Status**: In progress
- **Blocker**: None
- **Next step**: Begin M1 - Core reuse extraction (CodeModel/Metadata)

## Milestone Map

| Milestone | Name | Status | Notes |
|-----------|------|--------|-------|
| M0 | Repo bootstrap and packaging skeleton | In progress | Directory.Build.props, global.json, project graph, test projects done |
| M1 | Core reuse extraction (CodeModel/Metadata) | Not started | |
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
