# T281: Ignore Non-CSharp Projects In Graph Targets
- Milestone: Post
- Status: Done
- Agent: Codex (Executor)
- Started: 2026-03-05
- Completed: 2026-03-05

## Objective
Prevent false `TW2003` failures when loading mixed solutions that include non-C# project types such as `.dcproj` and `.esproj`.

## Approach
Update `ProjectGraphService` to filter unsupported project file types from load targets before TFM selection and assets checks. Add integration coverage under `tests/Typewriter.IntegrationTests/Loading`.

## Journey
### 2026-03-05
- Reproduced from user report: solution restore/build succeeds but load-plan stage emits `TW2003` for `.dcproj` and `.esproj`.
- Traced failure path to `ProjectGraphService.BuildPlanCoreAsync` where every graph node is treated as restore-assets eligible and loadable.
- Implemented filtering in `src/Typewriter.Loading.MSBuild/ProjectGraphService.cs`: only `.csproj` graph nodes are transformed into `LoadTarget` entries.
- Added mixed-project fixture under `tests/fixtures/solution-mixed/` containing one `.csproj`, one `.esproj`, and one `.dcproj`.
- Added integration regression test `Sln_MixedProjectTypes_SkipsNonCSharpTargets` in `tests/Typewriter.IntegrationTests/Loading/SolutionIntegrationTests.cs`.
- Ran mandatory verification outside sandbox:
  - `dotnet restore Typewriter.Cli.slnx`
  - `dotnet build Typewriter.Cli.slnx -c Release`
  - `dotnet test Typewriter.Cli.slnx -c Release`
  - Result: all succeeded; integration tests now 15/15 passing.

## Outcome
- False `TW2003` diagnostics for non-C# solution projects are eliminated.
- Mixed solutions now proceed with deterministic C#-only load targets.
- Regression test coverage added to lock behavior.

## Follow-ups
- Consider introducing an explicit informational diagnostic for skipped non-C# project types if users need visibility in detailed verbosity.
