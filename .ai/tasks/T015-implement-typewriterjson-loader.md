# T015: Implement typewriter.json loader and precedence merge
- Milestone: M2
- Status: Done
- Agent: Claude (Executor)
- Started: 2026-03-02
- Completed: 2026-03-02

## Objective
Create the configuration loading and merging layer in `src/Typewriter.Application/Configuration/`:
a POCO matching `typewriter.json`, an upward-walking loader, and a precedence-merge resolver
that produces `GenerateCommandOptions`.

## Approach
1. `TypewriterConfig.cs` — nullable POCO with all fields from the `typewriter.json` schema.
2. `TypewriterConfigLoader.cs` — static loader using `System.Text.Json`; walks upward from a
   start directory, stops at first `typewriter.json` found or at `.git` boundary.
3. `GenerateCommandOptions.cs` — converted from sealed class to immutable positional record;
   added `static Merge(TypewriterConfig? config, …)` with CLI > config > default precedence.
4. `Program.cs` — updated to call `GenerateCommandOptions.Merge(null, …)` (config integration
   deferred to T016 ApplicationRunner).
5. `ConfigurationPrecedenceTests.cs` — 8 tests covering all merge branches and loader scenarios.

## Journey
### 2026-03-02
- Created `src/Typewriter.Application/Configuration/` directory.
- Wrote `TypewriterConfig.cs` with 9 nullable properties matching the schema.
- Wrote `TypewriterConfigLoader.cs`; walks `DirectoryInfo.Parent` chain, checks for
  `typewriter.json` first, then `.git` boundary, then continues upward.
- Rewrote `GenerateCommandOptions.cs` as a positional record; `Verbosity` promoted from
  `string?` to `string` (defaults to `"normal"`). Added `Merge()` static factory.
- Updated `Program.cs` handler to call `GenerateCommandOptions.Merge(config: null, …)`.
- Added 8 tests in `tests/Typewriter.UnitTests/Configuration/ConfigurationPrecedenceTests.cs`.
- `dotnet build -c Release` → 0 errors, 0 warnings.
- `dotnet test -c Release` → 124/124 passed (UnitTests), all other suites green.

## Outcome
All files created, build clean, all tests green.
- `src/Typewriter.Application/Configuration/TypewriterConfig.cs`
- `src/Typewriter.Application/Configuration/TypewriterConfigLoader.cs`
- `src/Typewriter.Application/GenerateCommandOptions.cs` (updated)
- `src/Typewriter.Cli/Program.cs` (updated)
- `tests/Typewriter.UnitTests/Configuration/ConfigurationPrecedenceTests.cs`

## Follow-ups
- T016: Wire `TypewriterConfigLoader.Load()` into `ApplicationRunner` pipeline.
