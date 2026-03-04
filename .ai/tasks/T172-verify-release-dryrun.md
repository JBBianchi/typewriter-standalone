# T172: Verify release dry-run locally (M8)
- Milestone: M8
- Status: Done
- Agent: Executor (Claude)
- Started: 2026-03-04
- Completed: 2026-03-04

## Objective
Verify the end-to-end release dry-run locally: `dotnet pack` → local tool install → `typewriter-cli generate` smoke test.

## Approach
Run the three-step release flow manually, then capture the exact commands in a reusable `eng/verify-release-dryrun.sh` script.

## Journey
### 2026-03-04
1. **Toolchain preflight**: .NET SDK 10.0.100-rc.2.25502.107 available; ICU missing → set `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1`.
2. **dotnet pack -c Release**: Succeeded. Produced `src/Typewriter.Cli/bin/Release/Typewriter.Cli.0.0.0-preview.0.102.nupkg` (~11.9 MB). MinVer computed version from commit count (no git tag → `0.0.0-preview.0.102`). MINVER1008 deprecation warning for `MinVerDefaultPreReleasePhase` noted.
3. **dotnet tool install --local**: Initial attempt with package ID `typewriter-cli` failed — correct package ID is `Typewriter.Cli` (project name), while `typewriter-cli` is the `ToolCommandName`. Installed successfully with `--add-source ./src/Typewriter.Cli/bin/Release Typewriter.Cli --version 0.0.0-preview.0.102`.
4. **typewriter-cli generate**: First attempt with glob `**/*.tst` failed with TW3001 — CLI expects explicit file paths, not globs. Fixed by passing explicit `.tst` paths: `tests/fixtures/simple/SimpleProject/Enums.tst` and `Interfaces.tst`. Exit code 0, generated `UserRole.ts` and `UserModel.ts`.
5. **Verification script**: Created `eng/verify-release-dryrun.sh` with all three steps, version extraction from nupkg filename, output verification, and cleanup trap. Ran end-to-end successfully.

## Exact commands that passed
```bash
# 1. Pack
dotnet pack -c Release

# 2. Install local tool
dotnet tool install --local \
    --add-source src/Typewriter.Cli/bin/Release \
    Typewriter.Cli \
    --create-manifest-if-needed \
    --version "0.0.0-preview.0.102"

# 3. Restore fixture and smoke test
dotnet restore tests/fixtures/simple/SimpleProject/SimpleProject.csproj
dotnet typewriter-cli generate \
    tests/fixtures/simple/SimpleProject/Enums.tst \
    tests/fixtures/simple/SimpleProject/Interfaces.tst \
    --project tests/fixtures/simple/SimpleProject/SimpleProject.csproj
# Exit code: 0
# Generated: UserRole.ts, UserModel.ts
```

## Outcome
- All three acceptance criteria met:
  - `dotnet pack` succeeds → `Typewriter.Cli.0.0.0-preview.0.102.nupkg`
  - Local tool install succeeds
  - `typewriter-cli generate` exits with code 0, generates expected `.ts` files
- Reusable script: `eng/verify-release-dryrun.sh`
- All 179/179 tests pass (159 unit + 13 integration + 6 golden + 1 perf)

## Follow-ups
- MINVER1008: Consider updating `MinVerDefaultPreReleasePhase` → `MinVerDefaultPreReleaseIdentifiers` in `eng/versioning.props`
- CLI does not support glob expansion for template arguments — users must pass explicit paths
