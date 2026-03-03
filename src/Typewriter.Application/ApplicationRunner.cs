using Typewriter.Application.Diagnostics;
using Typewriter.Application.Loading;

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

    /// <param name="inputResolver">Resolves and validates the input project or solution path.</param>
    /// <param name="restoreService">Checks and runs <c>dotnet restore</c> when needed.</param>
    /// <param name="projectGraphService">Builds the topological <see cref="ProjectLoadPlan"/> via MSBuild ProjectGraph.</param>
    /// <param name="roslynWorkspaceService">Opens each project in a Roslyn MSBuildWorkspace and retrieves compilations.</param>
    public ApplicationRunner(
        IInputResolver inputResolver,
        IRestoreService restoreService,
        IProjectGraphService projectGraphService,
        IRoslynWorkspaceService roslynWorkspaceService)
    {
        _inputResolver = inputResolver;
        _restoreService = restoreService;
        _projectGraphService = projectGraphService;
        _roslynWorkspaceService = roslynWorkspaceService;
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

        // 6. Load the Roslyn workspace for semantic model extraction.
        var workspaceResult = await _roslynWorkspaceService.LoadAsync(plan, reporter, cancellationToken);
        if (workspaceResult is null)
            return 3;

        // 7. Elevate warnings to errors if --fail-on-warnings was specified.
        if (options.FailOnWarnings && reporter.WarningCount > 0)
            return 1;

        return 0;
    }
}
