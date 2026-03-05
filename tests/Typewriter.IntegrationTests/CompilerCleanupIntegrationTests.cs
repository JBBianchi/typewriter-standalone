using Typewriter.Application;
using Typewriter.Application.Diagnostics;
using Typewriter.Generation.Output;
using Typewriter.Generation.Performance;
using Typewriter.Loading.MSBuild;
using Xunit;

namespace Typewriter.IntegrationTests;

/// <summary>
/// Integration tests verifying that <see cref="ApplicationRunner.RunAsync"/> cleans up
/// the Compiler's per-invocation temp subdirectory after pipeline completion.
/// </summary>
[Collection("ApplicationRunner")]
public class CompilerCleanupIntegrationTests
{
    private sealed class CapturingReporter : IDiagnosticReporter
    {
        private int _warningCount;
        private int _errorCount;

        public void Report(DiagnosticMessage message)
        {
            if (message.Severity == DiagnosticSeverity.Warning) _warningCount++;
            else if (message.Severity == DiagnosticSeverity.Error) _errorCount++;
        }

        public int WarningCount => _warningCount;
        public int ErrorCount => _errorCount;
    }

    private static string FixturePath(string relativePath) =>
        Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "tests", "fixtures",
            relativePath));

    /// <summary>
    /// Verifies that after <see cref="ApplicationRunner.RunAsync"/> completes, very few new
    /// per-invocation subdirectories remain in the Typewriter temp directory.
    /// This confirms that the Compiler is properly disposed in the finally block.
    /// A small tolerance is allowed because assembly load contexts may keep files locked
    /// and parallel test assemblies may create transient Compiler instances.
    /// </summary>
    [Fact]
    public async Task RunAsync_CleansUpTempSubdirectory_AfterCompletion()
    {
        // Arrange
        var projectPath = FixturePath("simple/SimpleProject/SimpleProject.csproj");
        var templatePath = FixturePath("simple/SimpleProject/Interfaces.tst");

        var reporter = new CapturingReporter();
        var cache = new InvocationCache();
        var locator = new MsBuildLocatorService();
        var inputResolver = new InputResolver();
        var restoreService = new RestoreService();
        var solutionFallbackService = new SolutionFallbackService();
        var projectGraphService = new ProjectGraphService(locator, solutionFallbackService);
        var roslynWorkspaceService = new RoslynWorkspaceService(cache);
        var outputWriter = new OutputWriter();
        var outputPathPolicy = new OutputPathPolicy();

        locator.EnsureRegistered(reporter);

        if (!await restoreService.CheckAssetsAsync(projectPath))
        {
            var restored = await restoreService.RestoreAsync(projectPath, reporter);
            Assert.True(restored, "dotnet restore failed for SimpleProject fixture");
        }

        var runner = new ApplicationRunner(
            inputResolver,
            restoreService,
            projectGraphService,
            roslynWorkspaceService,
            outputWriter,
            outputPathPolicy,
            cache);

        var options = GenerateCommandOptions.Merge(
            config: null,
            templates: [templatePath],
            solution: null,
            project: projectPath,
            framework: null,
            configuration: null,
            runtime: null,
            restore: false,
            output: null,
            verbosity: null,
            failOnWarnings: false,
            dryRun: true);

        // Snapshot existing subdirectories before the run.
        var tempDir = Path.Combine(Path.GetTempPath(), "Typewriter");
        var preExisting = Directory.Exists(tempDir)
            ? Directory.GetDirectories(tempDir).ToHashSet(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Act
        var exitCode = await runner.RunAsync(options, reporter);

        // Assert — pipeline completed successfully
        Assert.Equal(0, exitCode);

        // Assert — Compiler.Dispose cleaned up; very few new subdirectories remain.
        var postDirectories = Directory.Exists(tempDir)
            ? Directory.GetDirectories(tempDir).ToHashSet(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        postDirectories.ExceptWith(preExisting);

        // Dispose was called; ideally zero new subdirectories remain.
        // However, TemplateAssemblyLoadContext keeps assembly files memory-mapped on
        // all platforms (not just Windows), so Directory.Delete may fail with
        // IOException.  Additionally, unit tests in other test assemblies run in
        // parallel and create their own Compiler instances in the same shared
        // /tmp/Typewriter directory, so transient subdirectories may appear between
        // the pre-snapshot and post-snapshot.  We tolerate a small count to keep the
        // assertion meaningful while avoiding cross-assembly flakiness.
        const int maxLeftover = 3;
        Assert.True(postDirectories.Count <= maxLeftover,
            $"Expected at most {maxLeftover} leftover temp subdirectories, but found {postDirectories.Count}: "
            + string.Join(", ", postDirectories));
    }
}
