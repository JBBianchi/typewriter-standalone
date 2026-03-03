using Typewriter.Application.Diagnostics;
using Typewriter.Application.Loading;

namespace Typewriter.Loading.MSBuild;

public sealed class InputResolver : IInputResolver
{
    // Accepted input file extensions: .csproj, .sln, .slnx
    private static readonly HashSet<string> AcceptedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".csproj", ".sln", ".slnx" };

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

        string ext = Path.GetExtension(resolvedPath);
        if (!AcceptedExtensions.Contains(ext))
        {
            reporter.Report(new DiagnosticMessage(
                DiagnosticSeverity.Error,
                DiagnosticCode.TW2002,
                $"Unsupported file extension '{ext}'. Accepted: .csproj, .sln, .slnx",
                File: resolvedPath));
            return Task.FromResult<ResolvedInput?>(null);
        }

        if (!File.Exists(resolvedPath))
        {
            reporter.Report(new DiagnosticMessage(
                DiagnosticSeverity.Error,
                DiagnosticCode.TW2002,
                $"Input file not found: '{resolvedPath}'",
                File: resolvedPath));
            return Task.FromResult<ResolvedInput?>(null);
        }

        string? solutionDirectory = Path.GetDirectoryName(resolvedPath);
        return Task.FromResult<ResolvedInput?>(new ResolvedInput(resolvedPath, solutionDirectory));
    }
}
