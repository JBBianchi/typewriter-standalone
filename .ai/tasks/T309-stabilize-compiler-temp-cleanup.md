# T309: Stabilize Compiler Temp Cleanup Test Isolation
- Milestone: Post
- Status: Done
- Agent: Codex (Executor)
- Started: 2026-03-05
- Completed: 2026-03-05

## Objective
Fix flaky `CompilerCleanupIntegrationTests` failures caused by transient leftover directories in `%TEMP%/Typewriter` during parallel test-project execution.

## Approach
Identify all `Compiler` construction sites in tests, ensure each instance is disposed deterministically, and verify with integration + full-suite commands.

## Journey
### 2026-03-05
- Reproduced context from user-provided failing assertion (`Expected at most 3 leftover temp subdirectories, but found 5`).
- Verified `ApplicationRunner` already disposes `Compiler` in a `finally` block.
- Found three undisposed `Compiler` instances in `tests/Typewriter.UnitTests/Generation/TemplateEngineTests.cs`.
- Fixed undisposed test compilers by switching to `using var` in `TemplateEngineTests`.
- Added temp root override support in `src/Typewriter.Generation/Compiler.cs` via `TYPEWRITER_TEMP_DIRECTORY` for deterministic test isolation.
- Updated `tests/Typewriter.IntegrationTests/CompilerCleanupIntegrationTests.cs` to run against an isolated temp root and restore environment state in `finally`.
- Added unit test `Constructor_UsesOverrideTempDirectory_WhenEnvironmentVariableSet` in `tests/Typewriter.UnitTests/Generation/CompilerTests.cs`.
- Verification results:
  - `dotnet restore Typewriter.Cli.slnx` succeeded.
  - `dotnet build Typewriter.Cli.slnx -c Release` succeeded.
  - `dotnet test Typewriter.Cli.slnx -c Release` succeeded (`204` unit, `16` integration, `6` golden, `3` performance).

## Outcome
- Flaky temp cleanup assertion is stabilized under parallel test-project execution.
- Compiler temp directory behavior remains unchanged by default and is now overrideable for isolated harnesses.

## Follow-ups
- If flakiness persists, consider moving compiler temp root to per-test-run directory for stronger isolation.
