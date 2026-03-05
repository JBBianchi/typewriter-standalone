using System.Diagnostics;
using Typewriter.Application;
using Typewriter.Application.Diagnostics;
using Typewriter.Generation.Output;
using Typewriter.Generation.Performance;
using Typewriter.Loading.MSBuild;
using Xunit;
using Xunit.Abstractions;

namespace Typewriter.PerformanceTests;

/// <summary>
/// Performance acceptance tests that run the full end-to-end pipeline against the
/// large-solution fixture (25 projects, 5 templates) and assert time and memory budgets.
/// </summary>
public class LargeSolutionTests : IAsyncLifetime
{
    private static readonly MsBuildLocatorService Locator = new();
    private static readonly object LocatorLock = new();
    private static bool _locatorRegistered;

    private readonly ITestOutputHelper _output;

    public LargeSolutionTests(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <inheritdoc />
    public Task InitializeAsync()
    {
        EnsureMsBuildRegistered();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>
    /// Verifies that the full end-to-end pipeline completes within the 60-second budget
    /// on a GitHub-hosted ubuntu runner.
    /// </summary>
    [Fact]
    [Trait("Category", "Performance")]
    public async Task LargeSolution_CompletesUnderThreshold()
    {
        var solutionPath = FixturePath("large-solution", "LargeSolution.sln");
        var templatePaths = GetLargeSolutionTemplatePaths();

        var stopwatch = Stopwatch.StartNew();
        var exitCode = await RunPipelineAsync(solutionPath, templatePaths);
        stopwatch.Stop();

        _output.WriteLine($"Elapsed: {stopwatch.Elapsed.TotalSeconds:F2} s");

        Assert.Equal(0, exitCode);
        Assert.True(
            stopwatch.Elapsed.TotalSeconds <= 60,
            $"Pipeline took {stopwatch.Elapsed.TotalSeconds:F2} s, exceeding the 60 s budget.");
    }

    /// <summary>
    /// Verifies that peak working set stays under the 2 GB memory budget after running
    /// the full end-to-end pipeline.
    /// </summary>
    [Fact]
    [Trait("Category", "Performance")]
    public async Task LargeSolution_PeakWorkingSet_UnderBudget()
    {
        var solutionPath = FixturePath("large-solution", "LargeSolution.sln");
        var templatePaths = GetLargeSolutionTemplatePaths();

        var exitCode = await RunPipelineAsync(solutionPath, templatePaths);

        var peakWorkingSet = Process.GetCurrentProcess().PeakWorkingSet64;
        _output.WriteLine($"Peak working set: {peakWorkingSet / (1024.0 * 1024.0):F1} MB");

        Assert.Equal(0, exitCode);
        Assert.True(
            peakWorkingSet <= 2L * 1024 * 1024 * 1024,
            $"Peak working set {peakWorkingSet / (1024.0 * 1024.0):F1} MB exceeds 2 GB budget.");
    }

    /// <summary>
    /// Returns absolute paths to all .tst template files in the large-solution fixture.
    /// </summary>
    private static IReadOnlyList<string> GetLargeSolutionTemplatePaths()
    {
        var fixtureDir = FixturePath("large-solution");
        return Directory.GetFiles(fixtureDir, "*.tst", SearchOption.AllDirectories);
    }

    /// <summary>
    /// Runs the full end-to-end pipeline against the given solution and templates.
    /// </summary>
    private static async Task<int> RunPipelineAsync(string solutionPath, IReadOnlyList<string> templatePaths)
    {
        var reporter = new SilentDiagnosticReporter();
        var writer = new NullOutputWriter();
        var cache = new InvocationCache();
        var inputResolver = new InputResolver();
        var restoreService = new RestoreService();
        var solutionFallbackService = new SolutionFallbackService();
        var projectGraphService = new ProjectGraphService(Locator, solutionFallbackService);
        var roslynWorkspaceService = new RoslynWorkspaceService(cache);
        var outputPathPolicy = new OutputPathPolicy();

        var runner = new ApplicationRunner(
            inputResolver,
            restoreService,
            projectGraphService,
            roslynWorkspaceService,
            writer,
            outputPathPolicy,
            cache);

        var options = new GenerateCommandOptions(
            Templates: templatePaths,
            Solution: solutionPath,
            Project: null,
            Framework: null,
            Configuration: null,
            Runtime: null,
            Restore: true,
            Output: null,
            Verbosity: "normal",
            FailOnWarnings: false);

        return await runner.RunAsync(options, reporter);
    }

    /// <summary>
    /// Resolves an absolute path to a fixture directory under <c>tests/fixtures/</c>.
    /// </summary>
    private static string FixturePath(params string[] segments)
    {
        var parts = new[] { AppContext.BaseDirectory, "..", "..", "..", "..", "..", "tests", "fixtures" };
        return Path.GetFullPath(Path.Combine([.. parts, .. segments]));
    }

    private static void EnsureMsBuildRegistered()
    {
        if (_locatorRegistered) return;
        lock (LocatorLock)
        {
            if (_locatorRegistered) return;
            var reporter = new SilentDiagnosticReporter();
            Locator.EnsureRegistered(reporter);
            _locatorRegistered = true;
        }
    }

    /// <summary>
    /// A diagnostic reporter that silently discards all messages.
    /// </summary>
    private sealed class SilentDiagnosticReporter : IDiagnosticReporter
    {
        private int _warningCount;
        private int _errorCount;

        /// <inheritdoc />
        public void Report(DiagnosticMessage message)
        {
            if (message.Severity == DiagnosticSeverity.Warning)
                Interlocked.Increment(ref _warningCount);
            else if (message.Severity == DiagnosticSeverity.Error)
                Interlocked.Increment(ref _errorCount);
        }

        /// <inheritdoc />
        public int WarningCount => _warningCount;

        /// <inheritdoc />
        public int ErrorCount => _errorCount;
    }

    /// <summary>
    /// An output writer that discards all output, used when only timing/memory is measured.
    /// </summary>
    private sealed class NullOutputWriter : IOutputWriter
    {
        /// <inheritdoc />
        public Task WriteAsync(string filePath, string content, bool addBom, CancellationToken ct) =>
            Task.CompletedTask;
    }
}
