using Typewriter.Application.Diagnostics;

namespace Typewriter.Loading.MSBuild;

public sealed class InputResolver : IInputResolver
{
    public Task<ResolvedInput?> ResolveAsync(
        string projectPath,
        IDiagnosticReporter reporter,
        CancellationToken ct = default)
    {
        // Expand ~ and environment variables, then resolve to an absolute path.
        string expanded = projectPath;
        if (expanded.StartsWith("~", StringComparison.Ordinal))
        {
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            expanded = home + expanded.Substring(1);
        }

        expanded = Environment.ExpandEnvironmentVariables(expanded);
        string resolvedPath = Path.GetFullPath(expanded);

        if (!File.Exists(resolvedPath))
        {
            reporter.Report(new DiagnosticMessage(
                DiagnosticSeverity.Error,
                DiagnosticCode.TW2002,
                $"Project file not found: '{resolvedPath}'",
                File: resolvedPath));
            return Task.FromResult<ResolvedInput?>(null);
        }

        string? solutionDirectory = Path.GetDirectoryName(resolvedPath);
        return Task.FromResult<ResolvedInput?>(new ResolvedInput(resolvedPath, solutionDirectory));
    }
}
