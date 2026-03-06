# T310: Implement template glob pattern expansion
- Milestone: Post
- Status: Done
- Agent: Codex (Executor)
- Started: 2026-03-05
- Completed: 2026-03-05

## Objective
Support real template glob expansion in `typewriter-cli generate` so patterns like `**/*.tst` are resolved to concrete template files with deterministic ordering and actionable diagnostics.

## Approach
Add template-pattern resolution in `Typewriter.Application` before template existence checks. Expand wildcard patterns against the current working directory, normalize absolute paths, deduplicate, and sort deterministically. Keep literal-path behavior and map failures to existing `TW` diagnostics. Update unit tests in `tests/Typewriter.UnitTests/Cli`.

## Journey
### 2026-03-05 (attempt 1)
- Reproduced analysis from user report: `Program.cs` advertises glob patterns, but `ApplicationRunner` validates templates via `File.Exists` on the raw argument list.
- Confirmed `TW2202` is currently raised for any Roslyn error diagnostics during workspace compilation and is independent from template path resolution.
- Began implementation plan for a resolver that expands globs, preserves deterministic order, and keeps existing exit-code behavior.

### 2026-03-05 (attempt 2)
- Implemented template argument resolution in `src/Typewriter.Application/ApplicationRunner.cs`:
  - wildcard detection (`*`, `?`) for glob patterns,
  - recursive `**` directory matching,
  - deterministic traversal/sorting and duplicate removal,
  - literal-path validation preserved,
  - diagnostics: `TW3001` when no file matches, `TW1001` on malformed/invalid pattern input.
- Wired rendering loop to use resolved template file list instead of raw CLI args.
- Added CLI unit tests in `tests/Typewriter.UnitTests/Cli/CliContractTests.cs`:
  - `Generate_GlobPattern_ResolvesTemplatesAndReturns0`,
  - `Generate_GlobPattern_NoMatches_Returns1WithTW3001`.
- Local sandbox verification caveat: full `dotnet restore/build/test` commands failed due environment-specific SDK/restore issues; user-provided local run confirmed the updated `CliContractTests` pass (12/12).

## Outcome
- Real glob support is now implemented for template arguments in `typewriter-cli generate`.
- `**/*.tst` and other wildcard patterns are expanded to concrete template files at runtime.
- Matching is deterministic and de-duplicated before generation.
- Existing literal-path behavior and diagnostic/exit-code semantics are preserved.

## Follow-ups
- Consider surfacing underlying Roslyn compile diagnostics (not only summary `TW2202`) for faster troubleshooting in external repos.
