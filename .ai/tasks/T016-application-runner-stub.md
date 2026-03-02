# T016: ApplicationRunner validation stub and exit-code mapping
- Milestone: M2
- Status: Done
- Agent: Claude (Executor)
- Started: 2026-03-02
- Completed: 2026-03-02

## Objective
Implement the M2 `ApplicationRunner` stub: validate CLI inputs (templates, solution/project), emit TW1002 diagnostic for missing solution/project, honour `--fail-on-warnings`, and return the correct exit code (0/1/2/3) per the CLI contract.

## Approach
1. Rewrote `ApplicationRunner.RunAsync()` with ordered validation guards returning exit codes 0/1/2.
2. Deleted the unused `Placeholder.cs` class from `Typewriter.Application`.
3. Added `CliContractTests.cs` in `tests/Typewriter.UnitTests/Cli/` with a `FakeDiagnosticReporter` helper and two fact tests covering exit codes 1 and 2.

Key decision: validation order is (a) empty-templates check → exit 2 before any reporter call; (b) missing solution/project → emit TW1002 error, exit 2; (c) `FailOnWarnings && reporter.WarningCount > 0` → exit 1; (d) success → exit 0.

## Journey
### 2026-03-02
- `ApplicationRunner.cs` was a stub returning `Task.FromResult(0)` unconditionally (from T013).
- Added guards in order: empty templates, missing solution/project (with TW1002 report), fail-on-warnings.
- `Placeholder.cs` still present from initial project scaffold — deleted it (only had a comment).
- Added `tests/Typewriter.UnitTests/Cli/CliContractTests.cs`:
  - `FakeDiagnosticReporter` with seeded counts lets tests bypass real console output.
  - `Generate_InvalidArgs_Returns2`: empty templates + no solution/project → 2.
  - `Generate_WarningsWithFailFlag_Returns1`: 1 pre-seeded warning + failOnWarnings → 1.
- `dotnet build -c Release` → 0 errors, 0 warnings.
- `dotnet test -c Release` → 129/129 passed.

## Outcome
Files changed:
- `src/Typewriter.Application/ApplicationRunner.cs` — rewritten with validation guards
- `src/Typewriter.Application/Placeholder.cs` — deleted
- `tests/Typewriter.UnitTests/Cli/CliContractTests.cs` — new (2 tests)

Build: 0 errors, 0 warnings. Tests: 129/129 passed.

## Follow-ups
- M3: Wire `TypewriterConfigLoader.Load()` and full pipeline (load → metadata → render → write) into `ApplicationRunner`.
