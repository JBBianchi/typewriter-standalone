# T011: Run M1 Acceptance Criteria
- Milestone: M1
- Status: Done
- Agent: Claude (Executor)
- Started: 2026-03-02
- Completed: 2026-03-02

## Objective
Execute and verify all M1 acceptance criteria: build, CodeModel tests, TypeMapping tests, and confirm no VS dependencies.

## Approach
Run the three required commands against the workspace and verify exit codes + output. Also grep source trees for forbidden references.

## Journey
### 2026-03-02
- .NET SDK 10.0.103 not pre-installed in agent environment; installed to `/tmp/dotnet` using the dotnet-install script with `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1` (no ICU libs on system).
- `dotnet restore` — all projects already up-to-date.
- `dotnet build -c Release` — **Build succeeded. 0 Warning(s). 0 Error(s).** All 11 projects compiled (7 src + 4 tests).
- `dotnet test tests/Typewriter.UnitTests --filter "FullyQualifiedName~CodeModel" -c Release` — **Passed! Failed: 0, Passed: 102, Skipped: 0**.
- `dotnet test tests/Typewriter.UnitTests --filter "FullyQualifiedName~TypeMapping" -c Release` — **Passed! Failed: 0, Passed: 71, Skipped: 0**.
- Grepped `src/Typewriter.Metadata`, `src/Typewriter.CodeModel`, `src/Typewriter.Metadata.Roslyn` for `EnvDTE`, `Microsoft.VisualStudio.*`, package references — **zero actual code references**. The single hit in `Settings.cs` was a documentation comment only.
- `origin/` verified unchanged (clean working tree, no diff).

## Outcome
All M1 acceptance criteria met:
- `dotnet build -c Release`: exit 0, 0 errors, 0 warnings
- CodeModel tests: 102/102 passed
- TypeMapping tests: 71/71 passed
- Zero VS/EnvDTE references in target projects (code or packages)
- `origin/` unchanged
- Runs on Linux (net10.0, no OS-specific code paths)

## Follow-ups
- None. M1 gate passed; M2 can begin.
