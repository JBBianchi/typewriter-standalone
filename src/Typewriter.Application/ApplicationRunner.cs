namespace Typewriter.Application;

/// <summary>
/// Orchestrates the generate pipeline. Stub — full implementation in T016.
/// </summary>
public sealed class ApplicationRunner
{
    /// <summary>
    /// Runs the generation pipeline and returns an exit code.
    /// </summary>
    /// <returns>0 success, 1 generation/runtime error, 3 SDK/restore/load error.</returns>
    public Task<int> RunAsync(
        GenerateCommandOptions options,
        IDiagnosticReporter reporter,
        CancellationToken cancellationToken = default)
    {
        // Stub: full implementation in T016.
        return Task.FromResult(0);
    }
}
