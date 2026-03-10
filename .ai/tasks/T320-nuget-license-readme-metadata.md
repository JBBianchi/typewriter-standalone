# T320: Add NuGet package license/readme metadata and Apache 2.0 LICENSE file
- Milestone: Post
- Status: Done
- Agent: Codex (Executor)
- Started: 2026-03-10
- Completed: 2026-03-10

## Objective
Remove NuGet publish warnings about missing package license/readme by adding proper package metadata and packed assets for `Typewriter.Cli`.

## Approach
Add an Apache 2.0 `LICENSE` file at repo root, update `src/Typewriter.Cli/Typewriter.Cli.csproj` with `PackageLicenseFile` and `PackageReadmeFile`, and include root `LICENSE`/`README.md` in the package. Then run required verification commands.

## Journey
### 2026-03-10 - Task setup and scope
- Confirmed warning context: package currently emits missing license/readme warnings during NuGet publish.
- Verified current `Typewriter.Cli.csproj` has no `PackageLicense*` or `PackageReadmeFile` metadata.
- Verified repo root had no `LICENSE` file.
- Registered T320 in `.ai/progress.md` before implementation per AGENTS update protocol.

### 2026-03-10 - Implementation
- Updated `src/Typewriter.Cli/Typewriter.Cli.csproj`:
  - added `PackageLicenseFile` set to `LICENSE`
  - added `PackageReadmeFile` set to `README.md`
  - added packed linked `None` items for repo-root `LICENSE` and `README.md` into package root
- Added new root `LICENSE` file containing Apache License 2.0 text.

### 2026-03-10 - Verification attempts
- Initial `dotnet restore` failed before execution with first-time-use path permissions (`UnauthorizedAccessException` under `C:\Users\CodexSandboxOffline\.dotnet`).
- Retried required commands with `DOTNET_CLI_HOME` set to repo-local `.dotnet-cli-home`.
- Ran required verification commands:
  - `dotnet restore`
  - `dotnet build -c Release`
  - `dotnet test -c Release`
  - `dotnet pack -c Release`
- Result: all failed in this sandbox with the existing non-actionable MSBuild behavior (`Build FAILED`, `0 Error(s)`).
- Captured logs:
  - `artifacts/t320-restore.log` (diagnostic output shows restore project-walk failure)
  - `artifacts/t320-build.log`
  - `artifacts/t320-test.log`
  - `artifacts/t320-pack.log`

## Outcome
Implemented:
- `src/Typewriter.Cli/Typewriter.Cli.csproj`
- `LICENSE`
- `.ai/progress.md`
- `.ai/tasks/T320-nuget-license-readme-metadata.md`

Verification:
- Mandatory commands were executed but could not be validated in this sandbox due the known MSBuild failure mode.

## Follow-ups
- Re-run required verification commands in stable local/CI environment.
- Confirm NuGet publish warning output no longer reports missing license/readme.
