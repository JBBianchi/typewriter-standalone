using Typewriter.Application.Diagnostics;
using Typewriter.Application.Orchestration;

namespace Typewriter.Loading.MSBuild;

public interface IProjectGraphService
{
    Task<ProjectLoadPlan?> BuildPlanAsync(
        ResolvedInput input,
        string? framework,
        string? configuration,
        string? runtime,
        IDiagnosticReporter reporter,
        CancellationToken ct = default);
}
