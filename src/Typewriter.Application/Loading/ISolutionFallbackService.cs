using Typewriter.Application.Diagnostics;

namespace Typewriter.Application.Loading;

public interface ISolutionFallbackService
{
    Task<IReadOnlyList<string>?> ListProjectPathsAsync(string slnxPath, IDiagnosticReporter reporter, CancellationToken ct);
}
