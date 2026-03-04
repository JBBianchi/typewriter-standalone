using System.Runtime.CompilerServices;
using Microsoft.Build.Graph;
using Typewriter.Application.Diagnostics;
using Typewriter.Application.Loading;
using Typewriter.Application.Orchestration;

namespace Typewriter.Loading.MSBuild;

public sealed class ProjectGraphService : IProjectGraphService
{
    private readonly IMsBuildLocatorService _locatorService;
    private readonly ISolutionFallbackService _solutionFallbackService;

    public ProjectGraphService(IMsBuildLocatorService locatorService, ISolutionFallbackService solutionFallbackService)
    {
        _locatorService = locatorService;
        _solutionFallbackService = solutionFallbackService;
    }

    /// <inheritdoc />
    /// <remarks>
    /// MSBuild locator registration must complete before any method referencing
    /// <c>Microsoft.Build</c> types is JIT-compiled. This wrapper calls
    /// <see cref="IMsBuildLocatorService.EnsureRegistered"/> first, then delegates
    /// to <see cref="BuildPlanCoreAsync"/> so the JIT defers resolution of
    /// <see cref="ProjectGraph"/> and related types until the assembly-resolve
    /// handler is in place.
    /// </remarks>
    public async Task<ProjectLoadPlan?> BuildPlanAsync(
        ResolvedInput input,
        string? framework,
        string? configuration,
        string? runtime,
        IDiagnosticReporter reporter,
        CancellationToken ct = default)
    {
        _locatorService.EnsureRegistered(reporter);

        if (reporter.ErrorCount > 0)
            return null;

        return await BuildPlanCoreAsync(input, framework, configuration, runtime, reporter, ct);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private async Task<ProjectLoadPlan?> BuildPlanCoreAsync(
        ResolvedInput input,
        string? framework,
        string? configuration,
        string? runtime,
        IDiagnosticReporter reporter,
        CancellationToken ct)
    {
        var globalProperties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Configuration"] = configuration ?? "Release"
        };
        if (runtime != null)
            globalProperties["RuntimeIdentifier"] = runtime;

        var ext = Path.GetExtension(input.ProjectPath);
        var isSlnx = ext.Equals(".slnx", StringComparison.OrdinalIgnoreCase);
        var isSln = ext.Equals(".sln", StringComparison.OrdinalIgnoreCase);

        ProjectGraph? graph = null;
        try
        {
            graph = new ProjectGraph(input.ProjectPath, globalProperties);
        }
        catch (Exception ex)
        {
            var code = (isSln || isSlnx) ? DiagnosticCode.TW2110 : DiagnosticCode.TW2002;
            reporter.Report(new DiagnosticMessage(
                DiagnosticSeverity.Error,
                code,
                $"Failed to load project graph from '{input.ProjectPath}': {ex.Message}"));
        }

        if (graph == null)
        {
            if (!isSlnx)
                return null;

            // .slnx fallback: enumerate projects via SolutionFallbackService
            var fallbackPaths = await _solutionFallbackService.ListProjectPathsAsync(input.ProjectPath, reporter, ct);
            if (fallbackPaths == null || fallbackPaths.Count == 0)
                return null;

            var entryPoints = fallbackPaths
                .Select(p => new ProjectGraphEntryPoint(p, globalProperties));
            try
            {
                graph = new ProjectGraph(entryPoints);
            }
            catch
            {
                return null;
            }
        }

        var sortedNodes = TopologicalSort(graph);
        var targets = new List<LoadTarget>(sortedNodes.Count);
        var hasError = false;

        for (int i = 0; i < sortedNodes.Count; i++)
        {
            var node = sortedNodes[i];
            var projectPath = node.ProjectInstance.FullPath;
            var projectName = Path.GetFileNameWithoutExtension(projectPath);
            var tfms = GetTargetFrameworks(node);

            string selectedTfm;
            if (framework != null)
            {
                if (!tfms.Contains(framework, StringComparer.OrdinalIgnoreCase))
                {
                    reporter.Report(new DiagnosticMessage(
                        DiagnosticSeverity.Error,
                        DiagnosticCode.TW2002,
                        $"Project '{projectPath}' does not target framework '{framework}'."));
                    hasError = true;
                    continue;
                }
                selectedTfm = framework;
            }
            else
            {
                selectedTfm = tfms.Count > 0 ? tfms[0] : string.Empty;
                if (tfms.Count > 1)
                {
                    reporter.Report(new DiagnosticMessage(
                        DiagnosticSeverity.Info,
                        DiagnosticCode.TW2401,
                        $"Project '{projectPath}' targets multiple frameworks ({string.Join(", ", tfms)}); defaulting to '{selectedTfm}'. Use --framework to specify."));
                }
            }

            var dir = Path.GetDirectoryName(projectPath)!;
            var assetsFile = Path.Combine(dir, "obj", "project.assets.json");
            if (!File.Exists(assetsFile))
            {
                reporter.Report(new DiagnosticMessage(
                    DiagnosticSeverity.Error,
                    DiagnosticCode.TW2003,
                    $"Restore assets missing for '{projectPath}'. Run with --restore or run 'dotnet restore' manually."));
                hasError = true;
            }

            targets.Add(new LoadTarget(projectPath, projectName, selectedTfm, configuration, runtime, i));
        }

        if (hasError)
            return null;

        var plan = new ProjectLoadPlan(input.ProjectPath, input.SolutionDirectory, targets, globalProperties);
        return plan;
    }

    private static List<string> GetTargetFrameworks(ProjectGraphNode node)
    {
        var tfms = node.ProjectInstance.GetPropertyValue("TargetFrameworks");
        if (!string.IsNullOrWhiteSpace(tfms))
        {
            return tfms.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                       .ToList();
        }

        var tfm = node.ProjectInstance.GetPropertyValue("TargetFramework");
        if (!string.IsNullOrWhiteSpace(tfm))
            return [tfm];

        return [];
    }

    private static List<ProjectGraphNode> TopologicalSort(ProjectGraph graph)
    {
        // Kahn's algorithm: dependencies-first ordering.
        // In-degree = number of direct ProjectReferences (edges to dependencies).
        // Nodes with in-degree 0 have no unprocessed dependencies — start there.
        // Tie-breaker: sort by FullPath ascending for determinism.
        var inDegree = graph.ProjectNodes
            .ToDictionary(n => n, n => n.ProjectReferences.Count);

        var available = new SortedSet<ProjectGraphNode>(
            Comparer<ProjectGraphNode>.Create((a, b) =>
                StringComparer.Ordinal.Compare(a.ProjectInstance.FullPath, b.ProjectInstance.FullPath)));

        foreach (var (node, deg) in inDegree)
        {
            if (deg == 0)
                available.Add(node);
        }

        var result = new List<ProjectGraphNode>(graph.ProjectNodes.Count);

        while (available.Count > 0)
        {
            var node = available.Min!;
            available.Remove(node);
            result.Add(node);

            foreach (var dependant in node.ReferencingProjects
                         .OrderBy(n => n.ProjectInstance.FullPath, StringComparer.Ordinal))
            {
                inDegree[dependant]--;
                if (inDegree[dependant] == 0)
                    available.Add(dependant);
            }
        }

        return result;
    }
}
