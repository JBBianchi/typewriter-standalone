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
        // Suppress SDK logo and telemetry output so they don't contaminate stdout
        // and avoid a deadlock when both stdout and stderr buffers fill concurrently.
        psi.EnvironmentVariables["DOTNET_NOLOGO"] = "1";
        psi.EnvironmentVariables["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";

        using var process = new Process { StartInfo = psi };
        process.Start();

        // Read stdout and stderr concurrently to prevent pipe-buffer deadlocks.
        var stdoutTask = process.StandardOutput.ReadToEndAsync(ct);
        var stderrTask = process.StandardError.ReadToEndAsync(ct);
        await Task.WhenAll(stdoutTask, stderrTask);
        var stdout = stdoutTask.Result;
        var stderr = stderrTask.Result;
        await process.WaitForExitAsync(ct);

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
