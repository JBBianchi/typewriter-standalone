using System.Diagnostics;
using Typewriter.Application.Diagnostics;
using Typewriter.Application.Loading;

namespace Typewriter.Loading.MSBuild;

public sealed class SolutionFallbackService : ISolutionFallbackService
{
    public async Task<IReadOnlyList<string>?> ListProjectPathsAsync(string slnxPath, IDiagnosticReporter reporter, CancellationToken ct)
    {
        var solutionDir = Path.GetDirectoryName(Path.GetFullPath(slnxPath))!;

        var psi = new ProcessStartInfo("dotnet", $"sln \"{slnxPath}\" list")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        using var process = new Process { StartInfo = psi };
        process.Start();

        // Read stdout and stderr concurrently to avoid deadlock when both pipe buffers fill.
        var stdoutTask = process.StandardOutput.ReadToEndAsync(ct);
        var stderrTask = process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);
        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        if (process.ExitCode != 0)
        {
            reporter.Report(new DiagnosticMessage(
                DiagnosticSeverity.Warning,
                DiagnosticCode.TW2310,
                $"dotnet sln list failed for '{slnxPath}':{(string.IsNullOrWhiteSpace(stderr) ? string.Empty : $"\n{stderr.TrimEnd()}")}"));
            return null;
        }

        var paths = new List<string>();
        foreach (var line in stdout.Split('\n'))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("Project(s)", StringComparison.Ordinal) || trimmed.All(c => c == '-'))
                continue;

            paths.Add(Path.GetFullPath(trimmed, solutionDir));
        }

        return paths;
    }
}
