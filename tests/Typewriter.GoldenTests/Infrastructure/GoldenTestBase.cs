using Typewriter.Application;
using Typewriter.Application.Diagnostics;
using Typewriter.Generation.Output;
using Typewriter.Loading.MSBuild;
using Xunit;

namespace Typewriter.GoldenTests.Infrastructure;

/// <summary>
/// Base class for golden tests. Provides shared infrastructure for MSBuild registration,
/// fixture path resolution, generation execution, and baseline comparison.
/// </summary>
public abstract class GoldenTestBase : IAsyncLifetime
{
    private static readonly MsBuildLocatorService Locator = new();
    private static readonly object LocatorLock = new();
    private static bool _locatorRegistered;

    /// <summary>
    /// Resolves an absolute path to a file under the <c>tests/</c> directory.
    /// </summary>
    protected static string TestsPath(params string[] segments)
    {
        var parts = new[] { AppContext.BaseDirectory, "..", "..", "..", "..", ".." , "tests" };
        return Path.GetFullPath(Path.Combine([.. parts, .. segments]));
    }

    /// <summary>
    /// Resolves an absolute path to a fixture directory.
    /// </summary>
    protected static string FixturePath(params string[] segments) =>
        TestsPath(["fixtures", .. segments]);

    /// <summary>
    /// Resolves an absolute path to a baseline directory.
    /// </summary>
    protected static string BaselinePath(params string[] segments) =>
        TestsPath(["baselines", .. segments]);

    /// <inheritdoc />
    public Task InitializeAsync()
    {
        EnsureMsBuildRegistered();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DisposeAsync() => Task.CompletedTask;

    private static void EnsureMsBuildRegistered()
    {
        if (_locatorRegistered) return;
        lock (LocatorLock)
        {
            if (_locatorRegistered) return;
            var reporter = new TestDiagnosticReporter();
            Locator.EnsureRegistered(reporter);
            _locatorRegistered = true;
        }
    }

    /// <summary>
    /// Runs the generation pipeline for a fixture and returns captured outputs.
    /// </summary>
    /// <param name="templatePaths">Absolute paths to <c>.tst</c> template files.</param>
    /// <param name="projectPath">Absolute path to the <c>.csproj</c> file, or <c>null</c> when using a solution.</param>
    /// <param name="solutionPath">Absolute path to the <c>.sln</c> file, or <c>null</c> when using a project.</param>
    /// <param name="restore">Whether to run <c>dotnet restore</c> before loading.</param>
    /// <returns>A dictionary of output file paths to their generated content.</returns>
    protected static async Task<GoldenTestResult> RunGenerationAsync(
        IReadOnlyList<string> templatePaths,
        string? projectPath = null,
        string? solutionPath = null,
        bool restore = false)
    {
        var reporter = new TestDiagnosticReporter();
        var capturingWriter = new CapturingOutputWriter();

        var inputResolver = new InputResolver();
        var restoreService = new RestoreService();
        var solutionFallbackService = new SolutionFallbackService();
        var projectGraphService = new ProjectGraphService(Locator, solutionFallbackService);
        var roslynWorkspaceService = new RoslynWorkspaceService();
        var outputPathPolicy = new OutputPathPolicy();

        var runner = new ApplicationRunner(
            inputResolver,
            restoreService,
            projectGraphService,
            roslynWorkspaceService,
            capturingWriter,
            outputPathPolicy);

        var options = new GenerateCommandOptions(
            Templates: templatePaths,
            Solution: solutionPath,
            Project: projectPath,
            Framework: null,
            Configuration: null,
            Runtime: null,
            Restore: restore,
            Output: null,
            Verbosity: "normal",
            FailOnWarnings: false);

        var exitCode = await runner.RunAsync(options, reporter);

        return new GoldenTestResult(exitCode, capturingWriter.Outputs, reporter);
    }

    /// <summary>
    /// Compares all generated output files against committed baselines.
    /// Fails on content mismatch, missing baseline files, or unexpected extra files.
    /// </summary>
    /// <param name="result">The generation result containing captured outputs.</param>
    /// <param name="baselineDir">Absolute path to the baseline directory for this fixture.</param>
    protected static void AssertMatchesBaselines(GoldenTestResult result, string baselineDir)
    {
        Assert.Equal(0, result.ExitCode);

        var baselineFiles = Directory.Exists(baselineDir)
            ? Directory.GetFiles(baselineDir, "*.ts")
                .ToDictionary(f => Path.GetFileName(f), f => f, StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var generatedFiles = result.Outputs
            .ToDictionary(
                kvp => Path.GetFileName(kvp.Key),
                kvp => kvp.Value,
                StringComparer.OrdinalIgnoreCase);

        // Check for missing baseline files (generated but no baseline).
        var unexpectedFiles = generatedFiles.Keys.Except(baselineFiles.Keys, StringComparer.OrdinalIgnoreCase).ToList();
        Assert.True(
            unexpectedFiles.Count == 0,
            $"Unexpected generated files with no baseline: {string.Join(", ", unexpectedFiles)}");

        // Check for missing generated files (baseline exists but not generated).
        var missingFiles = baselineFiles.Keys.Except(generatedFiles.Keys, StringComparer.OrdinalIgnoreCase).ToList();
        Assert.True(
            missingFiles.Count == 0,
            $"Missing generated files that have baselines: {string.Join(", ", missingFiles)}");

        // Compare content of each file.
        foreach (var (fileName, baselinePath) in baselineFiles)
        {
            Assert.True(
                generatedFiles.ContainsKey(fileName),
                $"Generated output missing for baseline file: {fileName}");

            var expectedContent = NormalizeLineEndings(File.ReadAllText(baselinePath));
            var actualContent = NormalizeLineEndings(generatedFiles[fileName]);

            Assert.True(
                string.Equals(expectedContent, actualContent, StringComparison.Ordinal),
                $"Content mismatch for '{fileName}'.\n" +
                $"--- Expected (baseline) ---\n{expectedContent}\n" +
                $"--- Actual (generated) ---\n{actualContent}");
        }
    }

    /// <summary>
    /// Normalizes line endings to LF for cross-platform comparison.
    /// </summary>
    private static string NormalizeLineEndings(string text) =>
        text.Replace("\r\n", "\n").Replace("\r", "\n");
}

/// <summary>
/// Holds the result of a golden test generation run.
/// </summary>
/// <param name="ExitCode">The exit code returned by <see cref="ApplicationRunner.RunAsync"/>.</param>
/// <param name="Outputs">Captured output files keyed by absolute path.</param>
/// <param name="Reporter">The diagnostic reporter with collected messages.</param>
public record GoldenTestResult(
    int ExitCode,
    IReadOnlyDictionary<string, string> Outputs,
    TestDiagnosticReporter Reporter);
