# Performance Tests

Performance acceptance tests for the typewriter-cli pipeline. These tests run the
full end-to-end pipeline against the large-solution fixture (25 projects, 5 templates)
and assert time and memory budgets defined in AGENTS.md section 11.

## Why are they excluded from CI?

All tests in this project are tagged with `[Trait("Category", "Performance")]`.
The standard CI matrix (`ci.yml`) filters them out with `--filter "Category!=Performance"`
to keep PR feedback fast and deterministic across all three OS runners.

A dedicated `performance` job in `ci.yml` runs them on `ubuntu-latest` on pushes to
`main` and on manual `workflow_dispatch` triggers.

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
# Run only performance tests
dotnet test tests/Typewriter.PerformanceTests/ -c Release --filter "Category=Performance"

# Run all tests including performance
dotnet test Typewriter.Cli.slnx -c Release
```

Both tests are tagged with `[Trait("Category", "Performance")]`, so the filter selects them specifically. Run in `Release` configuration to measure representative performance.

## Running performance tests in CI

Trigger the workflow manually via the GitHub Actions UI or the CLI:

```bash
gh workflow run ci.yml
```

The `performance` job will execute on `ubuntu-latest`.

## How to Update Budgets

1. Open `LargeSolutionTests.cs`.
2. Update the `Assert.True` threshold values:
   - Wall-time: change `60` in `LargeSolution_CompletesUnderThreshold`.
   - Memory: change `2L * 1024 * 1024 * 1024` in `LargeSolution_PeakWorkingSet_UnderBudget`.
3. Document the reason for the change in the commit message (e.g., new baseline after adding caching, accommodating a larger fixture).

## Test Inventory

| Test | Budget | What it measures |
|------|--------|------------------|
| `LargeSolution_CompletesUnderThreshold` | 60 s | End-to-end pipeline wall-clock time |
| `LargeSolution_PeakWorkingSet_UnderBudget` | 2 GB | Peak process working set after pipeline run |

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
