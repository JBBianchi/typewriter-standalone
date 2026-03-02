# T013: Implement generate command parser
- Milestone: M2
- Status: Done
- Agent: Claude (Executor)
- Started: 2026-03-02
- Completed: 2026-03-02

## Objective
Rewrite `src/Typewriter.Cli/Program.cs` to define the full `generate <templates>` subcommand using `System.CommandLine` 2.x prerelease, wiring all required options and delegating to `ApplicationRunner`.

## Approach
1. Added `System.CommandLine 2.0.0-beta4.22272.1` to `Typewriter.Cli.csproj`.
2. Created stubs in `Typewriter.Application` needed by the CLI:
   - `GenerateCommandOptions` — DTO carrying all parsed option values.
   - `IDiagnosticReporter` — interface for MSBuild-compatible diagnostic output.
   - `ApplicationRunner` — stub orchestrator (full impl deferred to T016).
3. Rewrote `Program.cs` using top-level statements with `CommandLineBuilder.UseDefaults()` and a pre-invoke parse-error intercept that maps errors to exit code 2.
4. Added `ConsoleDiagnosticReporter` in `Typewriter.Cli` as the console-backed `IDiagnosticReporter`.

## Journey
### 2026-03-02
- Explored codebase: `Program.cs` was a one-liner (`return 0;`); `Typewriter.Application` had only a placeholder class.
- Chose `System.CommandLine 2.0.0-beta4.22272.1` (prerelease 2.x, framework-independent, targets `netstandard2.0`).
- `ConsoleDiagnosticReporter` placed in `Typewriter.Cli` namespace. Initial build failed because top-level `Program.cs` (global namespace) couldn't find it — fixed by adding `using Typewriter.Cli;`.
- Final build: 0 errors, 0 warnings. All 103 unit tests pass.

## Outcome
Files changed:
- `src/Typewriter.Cli/Typewriter.Cli.csproj` — added `System.CommandLine` package ref
- `src/Typewriter.Cli/Program.cs` — full rewrite
- `src/Typewriter.Cli/ConsoleDiagnosticReporter.cs` — new
- `src/Typewriter.Application/GenerateCommandOptions.cs` — new
- `src/Typewriter.Application/IDiagnosticReporter.cs` — new
- `src/Typewriter.Application/ApplicationRunner.cs` — new (stub)

Build: `dotnet build -c Release` → 0 errors, 0 warnings.
Tests: 103/103 passed.

## Follow-ups
- T016: Full `ApplicationRunner` implementation (load → metadata → render → write pipeline).
- M2 remaining: `typewriter.json` loader/precedence merge; `--fail-on-warnings` behavior; `TW` code catalog.
