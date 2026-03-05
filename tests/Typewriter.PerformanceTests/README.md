# Performance Tests

Performance acceptance tests for the typewriter-cli pipeline, enforcing time and memory budgets against a large-solution fixture.

## Wall-Time Budget

**60 seconds** on a GitHub-hosted `ubuntu-latest` runner.

Rationale: the budget accounts for cold-start overhead (no warm NuGet/MSBuild caches) and the single-threaded Roslyn workspace load path. GitHub-hosted runners provide 2-core / 7 GB RAM machines; 60 s gives comfortable headroom over observed local times while catching genuine regressions.

The assertion lives in `LargeSolutionTests.LargeSolution_CompletesUnderThreshold`:

```csharp
Assert.True(
    stopwatch.Elapsed.TotalSeconds <= 60,
    $"Pipeline took {stopwatch.Elapsed.TotalSeconds:F2} s, exceeding the 60 s budget.");
```

## Memory Budget

**2 GB peak working set**.

Rationale: GitHub-hosted `ubuntu-latest` runners provide 7 GB RAM. A 2 GB ceiling ensures the CLI leaves ample room for the OS, MSBuild, and other CI processes. It also matches the memory profile of a typical developer laptop running multiple applications.

The assertion lives in `LargeSolutionTests.LargeSolution_PeakWorkingSet_UnderBudget`:

```csharp
Assert.True(
    peakWorkingSet <= 2L * 1024 * 1024 * 1024,
    $"Peak working set {peakWorkingSet / (1024.0 * 1024.0):F1} MB exceeds 2 GB budget.");
```

## How to Run

```bash
dotnet test tests/Typewriter.PerformanceTests/ -c Release --filter "Category=Performance"
```

Both tests are tagged with `[Trait("Category", "Performance")]`, so the filter selects them specifically. Run in `Release` configuration to measure representative performance.

## How to Update Budgets

1. Open `LargeSolutionTests.cs`.
2. Update the `Assert.True` threshold values:
   - Wall-time: change `60` in `LargeSolution_CompletesUnderThreshold`.
   - Memory: change `2L * 1024 * 1024 * 1024` in `LargeSolution_PeakWorkingSet_UnderBudget`.
3. Document the reason for the change in the commit message (e.g., new baseline after adding caching, accommodating a larger fixture).

## Large-Solution Fixture

The fixture lives at `tests/fixtures/large-solution/` and simulates a realistic multi-project solution:

- **25 projects** (`Project01` through `Project25`), each with a minimal `.csproj` targeting `net10.0` and a single `Class1.cs`.
- **1 solution file** (`LargeSolution.sln`) referencing all 25 projects with deterministic GUIDs.
- **5 `.tst` templates** spread across selected projects:
  - `Project03/Enums.tst` — enumerates enums.
  - `Project07/Interfaces.tst` — generates interfaces from `*Model` classes.
  - `Project12/Models.tst` — generates classes from all classes.
  - `Project18/Services.tst` — generates interfaces from `*Service` interfaces.
  - `Project22/AllTypes.tst` — lists all types (classes, interfaces, enums).

### Regenerating the Fixture

```bash
bash tests/fixtures/large-solution/generate.sh
```

The script is idempotent: it deletes all generated content (preserving `generate.sh` and `generate.ps1`) before recreating the projects, templates, and solution file. A PowerShell equivalent (`generate.ps1`) is also available for Windows.
