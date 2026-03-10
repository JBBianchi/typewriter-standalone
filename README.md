# Typewriter Standalone CLI

<!-- badges -->

## What is this?

This repository ports [Typewriter](https://github.com/AdaskoTheBeAsT/Typewriter) to a standalone .NET 10 CLI (`typewriter-cli`). Typewriter generates TypeScript files from C# source code using `.tst` template files, enabling strongly-typed client code that stays in sync with server models.

The original Typewriter runs as a Visual Studio extension (VSIX), which ties it to the Windows IDE at design time. This project removes that runtime dependency entirely, producing a cross-platform CLI that can run on Linux, macOS, and Windows.

The primary goal is CI/CD integration: teams can invoke `typewriter-cli` in build pipelines without requiring Visual Studio, while keeping full feature parity with the upstream extension.

## Planned CLI usage

```
typewriter-cli generate <templates> [--solution <path> | --project <path>] [options]
```

### Flags

| Flag | Description |
|------|-------------|
| `<templates>` | Glob(s) for template files, e.g. `"**/*.tst"` |
| `--solution <path>` | `.sln` or `.slnx` input |
| `--project <path>` | `.csproj` input |
| `--framework <TFM>` | Target framework to use |
| `--configuration <Debug\|Release>` | Build configuration |
| `--runtime <RID>` | Runtime identifier |
| `--restore` | Run restore before loading |
| `--output <dir>` | Output directory override |
| `--fail-on-warnings` | Treat warnings as errors (exit code 1) |
| `--msbuild-path <path>` | Explicit MSBuild instance path |
| `--verbosity quiet\|normal\|detailed` | Diagnostic verbosity level |

### Exit codes

| Code | Meaning |
|------|---------|
| `0` | Success |
| `1` | Generation/runtime/template errors; warnings elevated by `--fail-on-warnings` |
| `2` | Invalid arguments or inputs |
| `3` | Restore/load/build/SDK errors |

### Examples

```bash
# Common usage
typewriter-cli generate "**/*.tst" --solution ./MyApp.slnx --framework net10.0

# CI usage
dotnet restore ./MyApp.slnx
typewriter-cli generate "**/*.tst" --solution ./MyApp.slnx \
  --configuration Release --framework net10.0 \
  --verbosity normal --fail-on-warnings
```

### Diagnostic codes

Diagnostics use MSBuild-compatible format with stable `TWxxxx` codes:

| Range | Category |
|-------|----------|
| `TW1xxx` | Argument, input, and template discovery |
| `TW2xxx` | SDK, restore, MSBuild, graph, and workspace loading |
| `TW3xxx` | Template compile, parse, and runtime |
| `TW4xxx` | Output path and write |
| `TW9xxx` | Parity gate or internal contract violations |

## Project status

> **Planning / pre-implementation** — analysis is complete; implementation has not started. This is **not yet a working tool**.

The full roadmap, architecture, and phased delivery plan are documented in [`DETAILED_IMPLEMENTATION_PLAN.md`](DETAILED_IMPLEMENTATION_PLAN.md).

## Repository layout

| Path | Description |
|------|-------------|
| `origin/` | Vendored upstream Typewriter source (read-only reference) |
| `_archive/` | Analysis documents — findings, decisions, risks, parity matrix |
| `DETAILED_IMPLEMENTATION_PLAN.md` | Merged implementation plan with full architecture and CLI spec |
| `AGENTS.md` | Coding-agent guidelines and conventions |

## Key design decisions

- **`net10.0` target** — single target framework everywhere; best long-term baseline.
- **`dotnet tool` packaging** — CI-native distribution with lowest operational friction.
- **`ProjectGraph` + Roslyn workspace hybrid loading** — graph-first traversal for deterministic project ordering, Roslyn workspace for semantic models.
- **Template syntax unchanged** — `#reference`, `${ }`, filters, single-file mode, and output rules remain identical to upstream.
- **MSBuild-compatible diagnostics** — stable `TWxxxx` error codes parseable by CI systems.
- **Parity gates in CI** — golden output diffs, diagnostic snapshots, and metadata parity suites block release without cross-platform verification.

## Upstream reference

This project is derived from [AdaskoTheBeAsT/Typewriter](https://github.com/AdaskoTheBeAsT/Typewriter) (originally by Fredrik Hagnelius).

Reuse policy: **"lift first, rewrite last"** — upstream generation and CodeModel logic is reused as the default strategy; rewriting occurs only where Visual Studio coupling makes reuse unsafe.

## Release

Releases are triggered by pushing a version tag that matches `v*.*.*`:

```bash
git tag v1.0.0
git push --tags
```

The release workflow runs:

1. **Test matrix** - build and test across Windows, Linux, and macOS.
2. **Parity gate** - verify golden output and diagnostic stability.
3. **NuGet publish** - pack and push the `typewriter-cli` tool package to NuGet.
4. **Executable publish** - publish framework-dependent single-file executables for `win-x64`, `win-arm64`, `linux-x64`, `linux-arm64`, `osx-x64`, and `osx-arm64`, then attach versioned `.zip` assets to the GitHub release.

### Release artifacts

Tag releases now produce two distribution channels:

1. **NuGet tool package** (`TypewriterCli.<version>.nupkg`)
2. **GitHub release executable archives** (`typewriter-cli-v<version>-<rid>.zip`)

Install/use examples:

```bash
# NuGet tool (global or local tool flow)
dotnet tool install --global TypewriterCli --version <version>
typewriter-cli generate <templates> --project <path-to-csproj>
```

```bash
# Executable archive flow
# 1) Download typewriter-cli-v<version>-<rid>.zip from the GitHub release.
# 2) Extract it.
# 3) Run the binary directly:
#    Windows: Typewriter.Cli.exe
#    Linux/macOS: ./Typewriter.Cli
```

### Configuring the NuGet API key

The workflow requires a `NUGET_API_KEY` secret to publish packages. Set it in your GitHub repository under **Settings → Secrets and variables → Actions → New repository secret**.

> **Never hardcode the API key in workflow files or source code.** Always use the GitHub Actions secret mechanism to provide it at runtime.

## Contributing

See [`AGENTS.md`](AGENTS.md) for coding-agent conventions and project guidelines.

## License

The upstream Typewriter project is licensed under the [Apache License 2.0](origin/LICENSE). This derivative project aligns with the same license terms.

