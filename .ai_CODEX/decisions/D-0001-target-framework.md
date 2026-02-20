# D-0001: Target Framework

- ID: D-0001
- Title: Target framework for `typewriter-cli`
- Date: 2026-02-19

## Context
Select the runtime baseline for a cross-platform CLI replacing a VSIX/.NET Framework extension.

## Evidence
- Upstream core projects target `.NET Framework v4.7.2`:
  - `origin/src/Typewriter/Typewriter.csproj:18`
  - `origin/src/CodeModel/Typewriter.CodeModel.csproj:12`
  - `origin/src/Metadata/Typewriter.Metadata.csproj:12`
  - `origin/src/Roslyn/Typewriter.Metadata.Roslyn.csproj:12`
- Upstream host is Visual Studio-specific (`AsyncPackage`, VS SDK, VSIX), not cross-platform CLI:
  - `origin/src/Typewriter/VisualStudio/ExtensionPackage.cs:28`
  - `origin/src/Typewriter/Typewriter.csproj:240`
  - `origin/src/Typewriter/source.extension.vsixmanifest:18`
- CLI requirements require Linux/macOS/Windows CI and modern .NET.
- Spike environment confirms modern SDK toolchain behavior on SDK `10.0.103` with `.slnx` support and project graph loading:
  - `.ai/prototypes/PR-0001-msbuild-loading-spike.md`

## Conclusion
Use `net10.0` as the primary target framework for `typewriter-cli`.

## Impact
- Aligns the implementation baseline with the explicit project requirement to prefer .NET 10.
- Enables modern cross-platform runtime APIs and current MSBuild/Roslyn packages in a single TFM baseline.
- Requires CI agents and developer machines to install .NET 10 SDK/runtime; compatibility fallback can be evaluated later as an explicit parity/business decision.

## Next steps
- Implement CLI projects as SDK-style `net10.0` projects.
- Pin repository SDK selection to `10.0.x` via `global.json` during implementation.
- Validate package/runtime behavior on Linux/macOS/Windows in CI.
