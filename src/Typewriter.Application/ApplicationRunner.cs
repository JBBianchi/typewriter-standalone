using Typewriter.Application.Diagnostics;
using Typewriter.Application.Loading;
using Typewriter.CodeModel.Configuration;
using Typewriter.CodeModel.Implementation;
using Typewriter.Generation;
using Typewriter.Generation.Output;
using Typewriter.Metadata.Roslyn;

namespace Typewriter.Application;

/// <summary>
/// Orchestrates the generate pipeline: input resolution → restore → project graph → workspace load → generation.
/// </summary>
public sealed class ApplicationRunner
{
    private readonly IInputResolver _inputResolver;
    private readonly IRestoreService _restoreService;
    private readonly IProjectGraphService _projectGraphService;
    private readonly IRoslynWorkspaceService _roslynWorkspaceService;
    private readonly IOutputWriter _outputWriter;
    private readonly IOutputPathPolicy _outputPathPolicy;

    /// <summary>
    /// Initializes a new <see cref="ApplicationRunner"/> with the required loading and generation services.
    /// </summary>
    /// <param name="inputResolver">Resolves and validates the input project or solution path.</param>
    /// <param name="restoreService">Checks and performs NuGet restore when needed.</param>
    /// <param name="projectGraphService">Builds the topological load plan from MSBuild project graph.</param>
    /// <param name="roslynWorkspaceService">Opens projects in a Roslyn workspace and returns compilations.</param>
    /// <param name="outputWriter">Writer for persisting generated output to disk.</param>
    /// <param name="outputPathPolicy">Policy for resolving output paths with collision avoidance.</param>
    public ApplicationRunner(
        IInputResolver inputResolver,
        IRestoreService restoreService,
        IProjectGraphService projectGraphService,
        IRoslynWorkspaceService roslynWorkspaceService,
        IOutputWriter outputWriter,
        IOutputPathPolicy outputPathPolicy)
    {
        _inputResolver = inputResolver;
        _restoreService = restoreService;
        _projectGraphService = projectGraphService;
        _roslynWorkspaceService = roslynWorkspaceService;
        _outputWriter = outputWriter;
        _outputPathPolicy = outputPathPolicy;
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

        // 3. Resolve the input path to an absolute, validated file path.
        var projectArg = string.IsNullOrWhiteSpace(options.Project) ? options.Solution! : options.Project;
        var resolvedInput = await _inputResolver.ResolveAsync(projectArg, reporter, cancellationToken);
        if (resolvedInput is null)
            return 2;

        // 4. Check restore assets; run restore if requested.
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

        // 7. Validate that all template files exist before execution.
        foreach (var templatePath in options.Templates)
        {
            if (!File.Exists(templatePath))
            {
                reporter.Report(new DiagnosticMessage(
                    DiagnosticSeverity.Error,
                    DiagnosticCode.TW3001,
                    $"Template file not found: '{templatePath}'."));
                return 1;
            }
        }

        // 8. Execute templates against loaded metadata.
        var metadataProvider = new RoslynMetadataProvider(workspaceResult);
        var solutionFullName = resolvedInput.SolutionDirectory ?? resolvedInput.ProjectPath;
        var hasGenerationErrors = false;

        foreach (var templatePath in options.Templates)
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
                    _outputWriter,
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

        if (hasGenerationErrors)
            return 1;

        // 9. Elevate warnings to errors if --fail-on-warnings was specified.
        if (options.FailOnWarnings && reporter.WarningCount > 0)
            return 1;

        return 0;
    }
}
