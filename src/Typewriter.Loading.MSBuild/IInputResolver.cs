using Typewriter.Application.Diagnostics;

namespace Typewriter.Loading.MSBuild;

public interface IInputResolver
{
    Task<ResolvedInput?> ResolveAsync(string projectPath, IDiagnosticReporter reporter, CancellationToken ct = default);
}
