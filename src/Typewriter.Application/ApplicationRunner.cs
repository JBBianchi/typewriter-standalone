using System.Text.RegularExpressions;
using Typewriter.Application.Diagnostics;
using Typewriter.Application.Loading;
using Typewriter.Application.Performance;
using Typewriter.CodeModel.Configuration;
using Typewriter.CodeModel.Implementation;
using Typewriter.Generation;
using Typewriter.Generation.Output;
using Typewriter.Generation.Performance;
using Typewriter.Metadata.Roslyn;

namespace Typewriter.Application;

/// <summary>
/// Orchestrates the generate pipeline: input resolution → restore → project graph → workspace load → generation.
/// </summary>
public sealed class ApplicationRunner
{
    private static readonly StringComparer PathComparer = OperatingSystem.IsWindows()
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;

    private readonly IInputResolver _inputResolver;
    private readonly IRestoreService _restoreService;
    private readonly IProjectGraphService _projectGraphService;
    private readonly IRoslynWorkspaceService _roslynWorkspaceService;
    private readonly IOutputWriter _outputWriter;
    private readonly IOutputPathPolicy _outputPathPolicy;
    private readonly InvocationCache _cache;

    /// <summary>
    /// Initializes a new <see cref="ApplicationRunner"/> with the required loading and generation services.
    /// </summary>
    /// <param name="inputResolver">Resolves and validates the input project or solution path.</param>
    /// <param name="restoreService">Checks and performs NuGet restore when needed.</param>
    /// <param name="projectGraphService">Builds the topological load plan from MSBuild project graph.</param>
    /// <param name="roslynWorkspaceService">Opens projects in a Roslyn workspace and returns compilations.</param>
    /// <param name="outputWriter">Writer for persisting generated output to disk.</param>
    /// <param name="outputPathPolicy">Policy for resolving output paths with collision avoidance.</param>
    /// <param name="cache">
    /// Per-invocation cache for compiled template assemblies and Roslyn compilations.
    /// Shared with <see cref="RoslynWorkspaceService"/> and <see cref="Compiler"/> to avoid
    /// redundant work on repeated calls within the same process.
    /// </param>
    public ApplicationRunner(
        IInputResolver inputResolver,
        IRestoreService restoreService,
        IProjectGraphService projectGraphService,
        IRoslynWorkspaceService roslynWorkspaceService,
        IOutputWriter outputWriter,
        IOutputPathPolicy outputPathPolicy,
        InvocationCache cache)
    {
        _inputResolver = inputResolver;
        _restoreService = restoreService;
        _projectGraphService = projectGraphService;
        _roslynWorkspaceService = roslynWorkspaceService;
        _outputWriter = outputWriter;
        _outputPathPolicy = outputPathPolicy;
        _cache = cache;
    }

    /// <summary>
    /// Validates inputs and runs the loading pipeline. Returns an exit code.
    /// </summary>
    /// <returns>
    /// 0 — success;
    /// 1 — warnings elevated to errors (<see cref="GenerateCommandOptions.FailOnWarnings"/> is true and warnings exist);
    /// 2 — argument/input errors (empty templates, missing solution/project, file not found);
    /// 3 — SDK/restore/load/build errors.
    /// </returns>
    public async Task<int> RunAsync(
        GenerateCommandOptions options,
        IDiagnosticReporter reporter,
        CancellationToken cancellationToken = default)
    {
        // 1. Validate templates argument.
        if (options.Templates == null || options.Templates.Count == 0)
            return 2;

        // 2. Validate solution/project argument.
        if (string.IsNullOrWhiteSpace(options.Solution) && string.IsNullOrWhiteSpace(options.Project))
        {
            reporter.Report(new DiagnosticMessage(
                DiagnosticSeverity.Error,
                DiagnosticCode.TW1002,
                "Either --solution or --project must be provided."));
            return 2;
        }

        var timer = new StageTimer();

        // --- Stage: load (resolve + restore + graph building) ---
        timer.StartStage("load");

        // 3. Resolve the input path to an absolute, validated file path.
        var projectArg = string.IsNullOrWhiteSpace(options.Project) ? options.Solution! : options.Project;
        var resolvedInput = await _inputResolver.ResolveAsync(projectArg, reporter, cancellationToken);
        if (resolvedInput is null)
            return 2;

        // Bind the cache scope to the resolved entry-point so that compilation cache keys
        // include the scope and cannot be served to a different --project/--solution run
        // (AGENTS.md §11.1 — scope isolation).
        _cache.SetScope(resolvedInput.ProjectPath);

        // 4. Check restore assets; run restore if requested.
        // For solution inputs (.sln/.slnx), skip the early CheckAssetsAsync call — the per-project
        // check in ProjectGraphService.BuildPlanAsync already handles per-project assets validation.
        var isSolution = resolvedInput.ProjectPath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase)
            || resolvedInput.ProjectPath.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase);

        if (isSolution)
        {
            if (options.Restore)
            {
                var restoreOk = await _restoreService.RestoreAsync(resolvedInput.ProjectPath, reporter, cancellationToken);
                if (!restoreOk)
                    return 3;
            }
        }
        else
        {
            var assetsPresent = await _restoreService.CheckAssetsAsync(resolvedInput.ProjectPath, cancellationToken);
            if (!assetsPresent)
            {
                if (!options.Restore)
                {
                    reporter.Report(new DiagnosticMessage(
                        DiagnosticSeverity.Error,
                        DiagnosticCode.TW2003,
                        $"Restore assets missing for '{resolvedInput.ProjectPath}'. Run with --restore or run 'dotnet restore' manually."));
                    return 3;
                }

                var restoreOk = await _restoreService.RestoreAsync(resolvedInput.ProjectPath, reporter, cancellationToken);
                if (!restoreOk)
                    return 3;
            }
        }

        // 5. Build the project graph / load plan.
        var plan = await _projectGraphService.BuildPlanAsync(
            resolvedInput,
            options.Framework,
            options.Configuration,
            options.Runtime,
            reporter,
            cancellationToken);

        if (plan is null)
            return 3;

        // --- Stage: metadata (workspace open + compile) ---
        timer.StartStage("metadata");

        // 6. Load projects into a Roslyn workspace and obtain compilations.
        var workspaceResult = await _roslynWorkspaceService.LoadAsync(plan, reporter, cancellationToken);
        if (workspaceResult is null)
        {
            reporter.Report(new DiagnosticMessage(
                DiagnosticSeverity.Error,
                DiagnosticCode.TW2200,
                "Workspace load failed; see preceding diagnostics for details."));
            return 3;
        }

        // --- Stage: render (template validation + compilation) ---
        timer.StartStage("render");

        // 7. Resolve template arguments (literal files and glob patterns) to concrete paths.
        var resolvedTemplatePaths = ResolveTemplatePaths(options.Templates, reporter);
        if (resolvedTemplatePaths is null)
            return 1;

        // --- Stage: write (template execution + output writing) ---
        timer.StartStage("write");

        // 8. Execute templates against loaded metadata.
        // When dry-run mode is active, wrap the real writer so that file I/O is suppressed
        // while the rest of the pipeline (load → metadata → render) executes normally.
        DryRunOutputWriter? dryRunWriter = null;
        IOutputWriter effectiveWriter = _outputWriter;
        if (options.DryRun)
        {
            dryRunWriter = new DryRunOutputWriter(_outputWriter, filePath =>
            {
                reporter.Report(new DiagnosticMessage(
                    DiagnosticSeverity.Info,
                    DiagnosticCode.TW5001,
                    $"Dry-run: would write '{filePath}'."));
            });
            effectiveWriter = dryRunWriter;
        }

        var metadataProvider = new RoslynMetadataProvider(workspaceResult);
        var solutionFullName = resolvedInput.SolutionDirectory ?? resolvedInput.ProjectPath;
        var compiler = new Compiler(_cache);
        var hasGenerationErrors = false;

        try
        {
            foreach (var templatePath in resolvedTemplatePaths)
            {
                try
                {
                    // Pre-check: enumerate source files with a lightweight settings instance.
                    // GetFiles yields all .cs documents regardless of Settings, so this is safe.
                    // Avoids triggering template compilation when the workspace has no source files.
                    var probeSettings = new SettingsImpl(templatePath, solutionFullName);
                    var sourceFiles = metadataProvider.GetFiles(probeSettings, null).ToList();

                    if (sourceFiles.Count == 0)
                        continue;

                    var template = new Template(
                        templatePath,
                        solutionFullName,
                        _outputPathPolicy,
                        effectiveWriter,
                        compiler,
                        error =>
                        {
                            reporter.Report(new DiagnosticMessage(
                                DiagnosticSeverity.Error,
                                DiagnosticCode.TW3001,
                                error));
                            hasGenerationErrors = true;
                        });

                    if (template.Settings.IsSingleFileMode)
                    {
                        var files = sourceFiles
                            .Select(m => new FileImpl(m, template.Settings))
                            .ToArray();

                        if (!template.RenderFile(files))
                            hasGenerationErrors = true;
                    }
                    else
                    {
                        foreach (var fileMetadata in sourceFiles)
                        {
                            var file = new FileImpl(fileMetadata, template.Settings);

                            if (!template.RenderFile(file))
                                hasGenerationErrors = true;

                            if (template.HasCompileException)
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    reporter.Report(new DiagnosticMessage(
                        DiagnosticSeverity.Error,
                        DiagnosticCode.TW3002,
                        $"Template execution failed for '{templatePath}': {ex.Message}"));
                    hasGenerationErrors = true;
                }
            }

            timer.StopStage();
        }
        finally
        {
            compiler.Dispose();
        }

        // Emit dry-run summary when applicable.
        if (dryRunWriter is not null)
        {
            reporter.Report(new DiagnosticMessage(
                DiagnosticSeverity.Info,
                DiagnosticCode.TW5002,
                $"Dry-run complete: {dryRunWriter.FileCount} file(s) would have been written."));
        }

        // Emit stage timings at detailed verbosity or above.
        if (string.Equals(options.Verbosity, "detailed", StringComparison.OrdinalIgnoreCase)
            || string.Equals(options.Verbosity, "diagnostic", StringComparison.OrdinalIgnoreCase))
        {
            timer.Report(reporter, DiagnosticSeverity.Info);
        }

        if (hasGenerationErrors)
            return 1;

        // 9. Elevate warnings to errors if --fail-on-warnings was specified.
        if (options.FailOnWarnings && reporter.WarningCount > 0)
            return 1;

        return 0;
    }

    private static IReadOnlyList<string>? ResolveTemplatePaths(
        IReadOnlyList<string> templates,
        IDiagnosticReporter reporter)
    {
        var resolved = new List<string>();
        var seen = new HashSet<string>(PathComparer);

        foreach (var templateArg in templates)
        {
            if (string.IsNullOrWhiteSpace(templateArg))
            {
                reporter.Report(new DiagnosticMessage(
                    DiagnosticSeverity.Error,
                    DiagnosticCode.TW1001,
                    "Invalid template pattern: value cannot be empty."));
                return null;
            }

            if (ContainsGlobPattern(templateArg))
            {
                IReadOnlyList<string> matches;
                try
                {
                    matches = ExpandTemplateGlob(templateArg);
                }
                catch (Exception ex)
                {
                    reporter.Report(new DiagnosticMessage(
                        DiagnosticSeverity.Error,
                        DiagnosticCode.TW1001,
                        $"Invalid template pattern: '{templateArg}'. {ex.Message}"));
                    return null;
                }

                if (matches.Count == 0)
                {
                    reporter.Report(new DiagnosticMessage(
                        DiagnosticSeverity.Error,
                        DiagnosticCode.TW3001,
                        $"Template file not found: '{templateArg}'."));
                    return null;
                }

                foreach (var match in matches)
                {
                    if (seen.Add(match))
                        resolved.Add(match);
                }
            }
            else
            {
                string fullPath;
                try
                {
                    fullPath = Path.GetFullPath(templateArg);
                }
                catch (Exception ex)
                {
                    reporter.Report(new DiagnosticMessage(
                        DiagnosticSeverity.Error,
                        DiagnosticCode.TW1001,
                        $"Invalid template pattern: '{templateArg}'. {ex.Message}"));
                    return null;
                }

                if (!File.Exists(fullPath))
                {
                    reporter.Report(new DiagnosticMessage(
                        DiagnosticSeverity.Error,
                        DiagnosticCode.TW3001,
                        $"Template file not found: '{templateArg}'."));
                    return null;
                }

                if (seen.Add(fullPath))
                    resolved.Add(fullPath);
            }
        }

        return resolved;
    }

    private static bool ContainsGlobPattern(string path)
        => path.IndexOfAny(['*', '?']) >= 0;

    private static IReadOnlyList<string> ExpandTemplateGlob(string pattern)
    {
        var searchRoot = GetGlobSearchRoot(pattern, out var segments);
        if (!Directory.Exists(searchRoot))
            return [];

        var matches = new List<string>();
        CollectGlobMatches(searchRoot, segments, 0, matches);

        return matches
            .Distinct(PathComparer)
            .OrderBy(static path => path, PathComparer)
            .ToList();
    }

    private static string GetGlobSearchRoot(string pattern, out string[] remainingSegments)
    {
        var isRooted = Path.IsPathRooted(pattern);
        var basePath = isRooted
            ? Path.GetPathRoot(pattern) ?? throw new ArgumentException("Unable to determine root path.")
            : Environment.CurrentDirectory;

        var relativePattern = isRooted
            ? pattern[(Path.GetPathRoot(pattern)?.Length ?? 0)..]
            : pattern;

        var allSegments = relativePattern
            .Split(['\\', '/'], StringSplitOptions.RemoveEmptyEntries);

        var literalPrefixCount = 0;
        while (literalPrefixCount < allSegments.Length && !ContainsGlobPattern(allSegments[literalPrefixCount]))
        {
            literalPrefixCount++;
        }

        var root = basePath;
        for (var i = 0; i < literalPrefixCount; i++)
        {
            root = Path.Combine(root, allSegments[i]);
        }

        remainingSegments = allSegments[literalPrefixCount..];
        return Path.GetFullPath(root);
    }

    private static void CollectGlobMatches(
        string currentDirectory,
        string[] segments,
        int index,
        List<string> matches)
    {
        if (index >= segments.Length)
            return;

        var segment = segments[index];
        var isLast = index == segments.Length - 1;

        if (segment == "**")
        {
            if (isLast)
            {
                foreach (var file in EnumerateFilesDeterministic(currentDirectory, SearchOption.AllDirectories))
                {
                    matches.Add(Path.GetFullPath(file));
                }

                return;
            }

            // "**" can match zero or more directory segments.
            CollectGlobMatches(currentDirectory, segments, index + 1, matches);
            foreach (var subDirectory in EnumerateDirectoriesDeterministic(currentDirectory))
            {
                CollectGlobMatches(subDirectory, segments, index, matches);
            }

            return;
        }

        if (isLast)
        {
            foreach (var file in EnumerateFilesDeterministic(currentDirectory, SearchOption.TopDirectoryOnly))
            {
                if (IsSegmentMatch(Path.GetFileName(file), segment))
                {
                    matches.Add(Path.GetFullPath(file));
                }
            }

            return;
        }

        foreach (var subDirectory in EnumerateDirectoriesDeterministic(currentDirectory))
        {
            if (IsSegmentMatch(Path.GetFileName(subDirectory), segment))
            {
                CollectGlobMatches(subDirectory, segments, index + 1, matches);
            }
        }
    }

    private static IEnumerable<string> EnumerateDirectoriesDeterministic(string directory)
    {
        try
        {
            return Directory
                .EnumerateDirectories(directory)
                .OrderBy(static path => path, PathComparer)
                .ToArray();
        }
        catch
        {
            return [];
        }
    }

    private static IEnumerable<string> EnumerateFilesDeterministic(string directory, SearchOption option)
    {
        try
        {
            return Directory
                .EnumerateFiles(directory, "*", option)
                .OrderBy(static path => path, PathComparer)
                .ToArray();
        }
        catch
        {
            return [];
        }
    }

    private static bool IsSegmentMatch(string value, string wildcardPattern)
    {
        var regexPattern = "^" + Regex.Escape(wildcardPattern)
            .Replace(@"\*", ".*")
            .Replace(@"\?", ".") + "$";
        var options = RegexOptions.CultureInvariant;

        if (OperatingSystem.IsWindows())
            options |= RegexOptions.IgnoreCase;

        return Regex.IsMatch(value, regexPattern, options);
    }
}
