# D-0002: Packaging Strategy

- ID: D-0002
- Title: Packaging strategy for `typewriter-cli`
- Date: 2026-02-19

## Context
Choose distribution approach for developer machines and CI pipelines with minimal bootstrap complexity.

## Evidence
- Product requirement calls out global tool install target and CI usage.
- CLI is planned as modern cross-platform .NET (`D-0001` = `net10.0`).
- Upstream packaging is VSIX-specific and not reusable for CLI:
  - `origin/src/Typewriter/source.extension.vsixmanifest:44`
  - `origin/src/Typewriter/Typewriter.csproj:21`
  - `origin/src/Typewriter/Typewriter.csproj:255`

## Conclusion
Primary packaging target is a `dotnet tool` package (`typewriter-cli`) for both global and local tool-manifest installs.

## Impact
- Standard install flows:
  - `dotnet tool install -g typewriter-cli`
  - `dotnet tool install --local typewriter-cli`
- Works uniformly across Linux/macOS/Windows CI agents that have .NET SDK/runtime.
- Establishes .NET 10 as the minimum runtime/tooling requirement for tool execution.
- Optional secondary packaging (self-contained archives) is deferred and non-blocking.

## Next steps
- Structure solution to produce a tool package in early implementation phase.
- Add CI validation for tool install + invocation on supported OS matrix.
