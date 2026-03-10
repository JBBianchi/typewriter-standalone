# T319: Fix release pipeline (`setup-dotnet` + RID restore for publish-exe)
- Milestone: Post
- Status: Done
- Agent: Codex (Executor)
- Started: 2026-03-10
- Completed: 2026-03-10

## Objective
Fix workflow failures in `publish-exe` by ensuring RID-specific restore before `publish --no-restore`, and remove unsupported `actions/setup-dotnet@v4` inputs.

## Approach
Update `.github/workflows/release.yml` and `.github/workflows/ci.yml` to resolve SDK setup via `global.json`, remove `include-prerelease`, and adjust `publish-exe` restore to target each RID explicitly. Keep artifact formats, matrix RIDs, and publish/release channels unchanged.

## Journey
### 2026-03-10 - Root-cause confirmation
- Re-read workflow files and mapped failures to current YAML:
  - `setup-dotnet@v4` still used unsupported `include-prerelease`.
  - `publish-exe` restored solution without RID but published with `-r <rid>` and `--no-restore`, causing `NETSDK1047` (`project.assets.json` target missing for `net10.0/<rid>`).
- Confirmed all affected spots in `.github/workflows/` with targeted search.

### 2026-03-10 - Implementation
- Applied workflow changes:
  - switched `setup-dotnet` to `global-json-file: global.json` in:
    - `release.yml` build-test job
    - `release.yml` publish-exe job
    - `ci.yml` build job
  - added `strategy.fail-fast: false` in `release.yml` `publish-exe`.
  - replaced `publish-exe` restore command with:
    - `dotnet restore src/Typewriter.Cli/Typewriter.Cli.csproj -r ${{ matrix.rid }}`
- Left unchanged:
  - RID matrix
  - publish command shape (still `--no-restore`)
  - NuGet tool publish path
  - GitHub release artifact attachment behavior

## Outcome
Implemented:
- `.github/workflows/ci.yml`
- `.github/workflows/release.yml`
- `.ai/progress.md`
- `.ai/tasks/T319-fix-release-pipeline-rid-restore.md`

Verification attempts:
- `dotnet restore Typewriter.Cli.slnx -v minimal` -> failed (no actionable stderr in minimal output).
- `dotnet build Typewriter.Cli.slnx -c Release --no-restore` -> failed (`Build FAILED`, `0 Error(s)`).
- `dotnet test Typewriter.Cli.slnx -c Release --no-build` -> failed (no actionable stderr output).
- `dotnet pack src/Typewriter.Cli/Typewriter.Cli.csproj -c Release --no-build` -> failed (no actionable stderr output).
- Diagnostic capture run (`dotnet restore ... -v diag`) confirmed same sandbox failure point as prior sessions:
  - `_FilterRestoreGraphProjectInputItems` failure on `Typewriter.Cli.slnx`
  - `Build FAILED` with `0 Error(s)`

## Follow-ups
- Run required verification:
  - `dotnet restore`
  - `dotnet build -c Release`
  - `dotnet test -c Release`
  - `dotnet pack -c Release`
- Validate updated workflows via CI/tag run (outside sandbox).
