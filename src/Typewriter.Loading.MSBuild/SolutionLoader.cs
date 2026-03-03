using System.Xml.Linq;
using Typewriter.Application.Diagnostics;
using Typewriter.Application.Loading;
using Typewriter.Application.Orchestration;

namespace Typewriter.Loading.MSBuild;

/// <summary>
/// Orchestrates solution loading: tries <see cref="IProjectGraphService"/> first; on failure
/// emits <c>TW2110</c> and falls back to <see cref="ISolutionFallbackService"/> to enumerate
/// projects and produce a <see cref="ProjectLoadPlan"/> from the listed paths.
/// </summary>
public sealed class SolutionLoader
{
    private readonly IProjectGraphService _graphService;
    private readonly ISolutionFallbackService _fallbackService;

    public SolutionLoader(IProjectGraphService graphService, ISolutionFallbackService fallbackService)
    {
        _graphService = graphService;
        _fallbackService = fallbackService;
    }

    public async Task<ProjectLoadPlan?> BuildPlanAsync(
        ResolvedInput input,
        string? framework,
        string? configuration,
        string? runtime,
        IDiagnosticReporter reporter,
        CancellationToken ct = default)
    {
        try
        {
            return await _graphService.BuildPlanAsync(input, framework, configuration, runtime, reporter, ct);
        }
        catch (Exception ex)
        {
            reporter.Report(new DiagnosticMessage(
                DiagnosticSeverity.Error,
                DiagnosticCode.TW2110,
                $"ProjectGraph failed to load solution '{input.ProjectPath}': {ex.Message}"));

            var projectPaths = await _fallbackService.ListProjectPathsAsync(input.ProjectPath, reporter, ct);
            if (projectPaths == null || projectPaths.Count == 0)
                return null;

            var globalProperties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Configuration"] = configuration ?? "Release"
            };
            if (runtime != null)
                globalProperties["RuntimeIdentifier"] = runtime;

            var targets = new List<LoadTarget>(projectPaths.Count);
            for (int i = 0; i < projectPaths.Count; i++)
            {
                var path = projectPaths[i];
                var name = Path.GetFileNameWithoutExtension(path);
                var tfm = ReadTargetFramework(path) ?? string.Empty;
                targets.Add(new LoadTarget(path, name, tfm, configuration, runtime, i));
            }

            return new ProjectLoadPlan(input.ProjectPath, input.SolutionDirectory, targets, globalProperties);
        }
    }

    private static string? ReadTargetFramework(string projectPath)
    {
        try
        {
            var doc = XDocument.Load(projectPath);
            return doc.Descendants("TargetFramework").FirstOrDefault()?.Value
                ?? doc.Descendants("TargetFrameworks").FirstOrDefault()?.Value?.Split(';')[0];
        }
        catch
        {
            return null;
        }
    }
}
