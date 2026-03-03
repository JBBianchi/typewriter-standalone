using Typewriter.Application.Diagnostics;
using Typewriter.Application.Orchestration;

namespace Typewriter.Application.Loading;

/// <summary>
/// Application-layer abstraction for opening projects in a Roslyn MSBuildWorkspace.
/// Implementations live in <c>Typewriter.Loading.MSBuild</c>; this interface keeps the
/// Application layer free of MSBuild-specific types.
/// </summary>
public interface IRoslynWorkspaceService
{
    /// <summary>
    /// Opens each project described by <paramref name="plan"/> in a Roslyn workspace and returns
    /// the resulting projects paired with their compilations in topological order.
    /// </summary>
    /// <param name="plan">The load plan produced by the MSBuild project-graph step.</param>
    /// <param name="reporter">Receives any diagnostic events emitted during workspace loading.</param>
    /// <param name="ct">Token used to cancel the operation.</param>
    /// <returns>
    /// A populated <see cref="WorkspaceLoadResult"/> on success, or <c>null</c> when a fatal
    /// workspace failure prevents any projects from being loaded.
    /// </returns>
    Task<WorkspaceLoadResult?> LoadAsync(ProjectLoadPlan plan, IDiagnosticReporter reporter, CancellationToken ct);
}
