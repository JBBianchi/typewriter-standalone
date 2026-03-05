using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Typewriter.Application.Diagnostics;
using Typewriter.Application.Loading;
using Typewriter.Application.Orchestration;
using Typewriter.Generation.Performance;
using Typewriter.Metadata.Roslyn;

using RoslynDiagnosticSeverity = Microsoft.CodeAnalysis.DiagnosticSeverity;
using TwDiagnosticSeverity = Typewriter.Application.Diagnostics.DiagnosticSeverity;

namespace Typewriter.Loading.MSBuild;

/// <summary>
/// Opens each project from a <see cref="ProjectLoadPlan"/> in a Roslyn
/// <see cref="MSBuildWorkspace"/> and returns the projects paired with their compilations.
/// MSBuild registration must be completed by <see cref="IMsBuildLocatorService"/> before
/// calling <see cref="LoadAsync"/>.
/// </summary>
/// <remarks>
/// Uses <see cref="InvocationCache"/> to cache Roslyn <see cref="Compilation"/> objects
/// per project path, avoiding redundant compilation on repeated invocations within the
/// same process.
/// </remarks>
public sealed class RoslynWorkspaceService : IRoslynWorkspaceService
{
    private readonly InvocationCache _cache;

    /// <summary>
    /// Initializes a new <see cref="RoslynWorkspaceService"/> with the specified invocation cache.
    /// </summary>
    /// <param name="cache">
    /// The per-invocation cache used to store Roslyn compilations, keyed by normalized
    /// absolute project path.
    /// </param>
    public RoslynWorkspaceService(InvocationCache cache)
    {
        _cache = cache;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Creates a single <see cref="MSBuildWorkspace"/> for the entire plan, opens each
    /// <see cref="LoadTarget"/> in topological order, and collects workspace diagnostics
    /// emitted during each <c>OpenProjectAsync</c> call.  Compilation errors are reported
    /// via <paramref name="reporter"/> but do not prevent the entry from appearing in the
    /// result; a null compilation skips the entry entirely.  Returns <c>null</c> only when
    /// the workspace itself cannot be created.
    /// </remarks>
    public async Task<WorkspaceLoadResult?> LoadAsync(
        ProjectLoadPlan plan,
        IDiagnosticReporter reporter,
        CancellationToken ct)
    {
        var properties = new Dictionary<string, string>(plan.GlobalProperties);

        MSBuildWorkspace workspace;
        try
        {
            workspace = MSBuildWorkspace.Create(properties);
        }
        catch (Exception ex)
        {
            reporter.Report(new DiagnosticMessage(
                TwDiagnosticSeverity.Error,
                DiagnosticCode.TW2200,
                $"Failed to create MSBuildWorkspace: {ex.Message}"));
            return null;
        }

        // Workspace is intentionally not wrapped in a using block: Project objects returned
        // in WorkspaceLoadResult hold workspace references that callers may need for further
        // Roslyn queries.  For a single-invocation CLI process the GC reclaims the workspace
        // on exit.
        var entries = new List<(Project, Compilation)>();

        foreach (var loadTarget in plan.Targets)
        {
            ct.ThrowIfCancellationRequested();

            // Snapshot the diagnostic count so we can identify diagnostics added by this open.
            int diagCountBefore = workspace.Diagnostics.Count;

            Project project;
            try
            {
                project = await workspace.OpenProjectAsync(loadTarget.ProjectPath, cancellationToken: ct);
            }
            catch (Exception ex)
            {
                reporter.Report(new DiagnosticMessage(
                    TwDiagnosticSeverity.Error,
                    DiagnosticCode.TW2200,
                    $"Failed to open project '{loadTarget.ProjectPath}': {ex.Message}",
                    File: loadTarget.ProjectPath));
                continue;
            }

            // Emit workspace diagnostics collected during this project open.
            foreach (var workspaceDiag in workspace.Diagnostics.Skip(diagCountBefore))
            {
                var (severity, code) = workspaceDiag.Kind == WorkspaceDiagnosticKind.Failure
                    ? (TwDiagnosticSeverity.Error, DiagnosticCode.TW2200)
                    : (TwDiagnosticSeverity.Warning, DiagnosticCode.TW2201);
                reporter.Report(new DiagnosticMessage(
                    severity, code, workspaceDiag.Message, File: loadTarget.ProjectPath));
            }

            Compilation? compilation;
            try
            {
                compilation = _cache.GetOrAddCompilation(loadTarget.ProjectPath, _ =>
                    project.GetCompilationAsync(ct).GetAwaiter().GetResult()
                    ?? throw new InvalidOperationException(
                        $"Compilation was null for project '{loadTarget.ProjectName}'."));
            }
            catch (Exception ex)
            {
                reporter.Report(new DiagnosticMessage(
                    TwDiagnosticSeverity.Error,
                    DiagnosticCode.TW2202,
                    $"Compilation failed for project '{loadTarget.ProjectName}': {ex.Message}",
                    File: loadTarget.ProjectPath));
                continue;
            }

            if (compilation.GetDiagnostics(ct).Any(d => d.Severity == RoslynDiagnosticSeverity.Error))
            {
                reporter.Report(new DiagnosticMessage(
                    TwDiagnosticSeverity.Error,
                    DiagnosticCode.TW2202,
                    $"Compilation for project '{loadTarget.ProjectName}' contains error diagnostics.",
                    File: loadTarget.ProjectPath));
            }

            entries.Add((project, compilation));
        }

        return new WorkspaceLoadResult(entries);
    }
}
