# PR-0001: MSBuild Loading Spike

- ID: PR-0001
- Title: Empirical spike for `.csproj`, `.sln`, `.slnx`, global.json, multi-targeting, and restore behavior
- Date: 2026-02-19

## Context
We must choose a project-loading strategy for a cross-platform CLI that supports `.csproj`, `.sln`, `.slnx`, SDK resolution, multi-targeting, and CI restore behavior.

## Evidence
- Spike artifacts:
  - `.ai/prototypes/spike-msbuild/SpikeSolution.slnx:1`
  - `.ai/prototypes/spike-msbuild/SpikeSolutionLegacy.sln:2`
  - `.ai/prototypes/spike-msbuild/MultiLib/MultiLib.csproj:4`
  - `.ai/prototypes/spike-msbuild/global.json:1`
  - `.ai/prototypes/spike-msbuild/GraphProbe/Program.cs:11`
- `dotnet new sln -h` output shows:
  - `--format <sln|slnx>`
  - default format `slnx`.
- `dotnet sln -h` output shows `migrate` command (`.sln` -> `.slnx`).
- `dotnet sln SpikeSolution.slnx list` and `dotnet sln SpikeSolutionLegacy.sln list` both succeeded and listed projects.
- `dotnet build SpikeSolution.slnx` and `dotnet build SpikeSolutionLegacy.sln` succeeded in initial single-project setup.
- `global.json` resolution behavior:
  - with an invalid SDK version (`99.0.100`), `dotnet --version` and build commands failed with “A compatible .NET SDK was not found.”
  - with pinned installed SDK (`10.0.103`) in `.ai/prototypes/spike-msbuild/global.json`, commands resolved and executed.
- Restore behavior:
  - `dotnet build ... --no-restore` on non-restored project fails with `NETSDK1004` (missing `obj/project.assets.json`).
- `ProjectGraph` probe:
  - `GraphProbe` successfully loaded:
    - `.csproj`
    - `.sln`
    - `.slnx`
  - with explicit MSBuild environment globals (`MSBuildSDKsPath`, `MSBuildExtensionsPath`, `MSBUILD_EXE_PATH`) in `GraphProbe/Program.cs`.
  - multi-target `TargetFrameworks=<tfm1>;<tfm2>` produced multiple graph nodes by default and a single node when `TargetFramework` global property was specified.
- Environment caveat observed:
  - On this machine, multi-target `dotnet build` occasionally fails with MSBuild SDK resolver issue `MSB4276` related to workload locator SDKs; graph loading still worked.

## Conclusion
- `.slnx` is a first-class solution format in the tested SDK/CLI toolchain.
- `ProjectGraph` can load `.csproj`, `.sln`, and `.slnx` and exposes multi-target expansion behavior suitable for deterministic traversal.
- global.json and restore state directly impact load/build success and must be explicit in CLI design.

## Impact
- Strong support for using `ProjectGraph` as traversal/entry loading foundation.
- CLI should support:
  - explicit framework selection (`--framework`) by setting `TargetFramework` global property,
  - optional restore (`--restore`) and clear failure messaging when assets are missing,
  - SDK resolution transparency and diagnostics when global.json is incompatible.
- Cross-platform caution: build/load errors from SDK/workload resolver must map to exit code `3` with actionable diagnostics.

## Next steps
- Finalize decision `D-0003` using this spike.
- Build loader abstraction that can:
  - resolve input type (`.csproj`/`.sln`/`.slnx`),
  - build a deterministic project graph,
  - apply global properties (`Configuration`, `TargetFramework`, `RuntimeIdentifier`),
  - optionally run restore before semantic loading.
