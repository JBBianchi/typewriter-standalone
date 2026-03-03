using NSubstitute;
using Typewriter.Application.Diagnostics;
using Typewriter.Application.Loading;
using Typewriter.Loading.MSBuild;
using Xunit;

namespace Typewriter.IntegrationTests.Loading;

/// <summary>
/// Integration tests for ProjectGraphService .sln and .slnx loading,
/// including the ISolutionFallbackService fallback path for .slnx.
/// </summary>
public class SolutionIntegrationTests
{
    private sealed class CapturingReporter : IDiagnosticReporter
    {
        private readonly List<DiagnosticMessage> _messages = [];
        private int _warningCount;
        private int _errorCount;

        public void Report(DiagnosticMessage message)
        {
            _messages.Add(message);
            if (message.Severity == DiagnosticSeverity.Warning) _warningCount++;
            else if (message.Severity == DiagnosticSeverity.Error) _errorCount++;
        }

        public IReadOnlyList<DiagnosticMessage> Messages => _messages;
        public int WarningCount => _warningCount;
        public int ErrorCount => _errorCount;
    }

    private static string FixturePath(string relativePath) =>
        Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "tests", "fixtures",
            relativePath));

    [Fact]
    public async Task Sln_HappyPath_ProducesValidProjectLoadPlan()
    {
        // Arrange
        var slnPath = FixturePath("solution-sln/SolutionSln.sln");

        var locator = new MsBuildLocatorService();
        var restoreService = new RestoreService();
        var fallbackService = new SolutionFallbackService();
        var graphService = new ProjectGraphService(locator, fallbackService);
        var reporter = new CapturingReporter();

        locator.EnsureRegistered(reporter);

        // Ensure projects are restored
        foreach (var proj in new[] { "solution-sln/ProjectA/ProjectA.csproj", "solution-sln/ProjectB/ProjectB.csproj" })
        {
            var projPath = FixturePath(proj);
            if (!await restoreService.CheckAssetsAsync(projPath))
                await restoreService.RestoreAsync(projPath, reporter);
        }

        // Act
        var input = new ResolvedInput(slnPath, Path.GetDirectoryName(slnPath));
        var plan = await graphService.BuildPlanAsync(input, null, null, null, reporter);

        // Assert
        Assert.NotNull(plan);
        Assert.Equal(2, plan.Targets.Count);
        Assert.All(plan.Targets, t => Assert.Equal("net10.0", t.TargetFramework));
    }

    [Fact]
    public async Task Slnx_HappyPath_ProducesValidProjectLoadPlan()
    {
        // Arrange
        var slnxPath = FixturePath("solution-slnx/SolutionSlnx.slnx");

        var locator = new MsBuildLocatorService();
        var restoreService = new RestoreService();
        var fallbackService = new SolutionFallbackService();
        var graphService = new ProjectGraphService(locator, fallbackService);
        var reporter = new CapturingReporter();

        locator.EnsureRegistered(reporter);

        // Ensure projects are restored
        foreach (var proj in new[] { "solution-slnx/ProjectA/ProjectA.csproj", "solution-slnx/ProjectB/ProjectB.csproj" })
        {
            var projPath = FixturePath(proj);
            if (!await restoreService.CheckAssetsAsync(projPath))
                await restoreService.RestoreAsync(projPath, reporter);
        }

        // Act
        var input = new ResolvedInput(slnxPath, Path.GetDirectoryName(slnxPath));
        var plan = await graphService.BuildPlanAsync(input, null, null, null, reporter);

        // Assert: either ProjectGraph handled .slnx natively, or the fallback kicked in
        Assert.NotNull(plan);
        Assert.Equal(2, plan.Targets.Count);
        Assert.All(plan.Targets, t => Assert.Equal("net10.0", t.TargetFramework));
    }

    [Fact]
    public async Task Sln_ProjectGraphFailure_EmitsTW2110_NoFallback()
    {
        // Arrange: use a non-existent .sln path to force ProjectGraph failure
        var nonExistentSln = Path.Combine(Path.GetTempPath(), "nonexistent_" + Guid.NewGuid() + ".sln");
        var fallback = Substitute.For<ISolutionFallbackService>();
        var locator = new MsBuildLocatorService();
        var reporter = new CapturingReporter();
        locator.EnsureRegistered(reporter);

        var graphService = new ProjectGraphService(locator, fallback);
        var input = new ResolvedInput(nonExistentSln, Path.GetTempPath());

        // Act
        var plan = await graphService.BuildPlanAsync(input, null, null, null, reporter);

        // Assert
        Assert.Null(plan);
        Assert.Contains(reporter.Messages, m => m.Code == DiagnosticCode.TW2110);
        // Fallback must NOT be called for .sln
        await fallback.DidNotReceive().ListProjectPathsAsync(
            Arg.Any<string>(), Arg.Any<IDiagnosticReporter>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Slnx_ProjectGraphFailure_FallbackCalled_NullWhenFallbackReturnsNull()
    {
        // Arrange: use a non-existent .slnx path to force ProjectGraph failure
        var nonExistentSlnx = Path.Combine(Path.GetTempPath(), "nonexistent_" + Guid.NewGuid() + ".slnx");
        var fallback = Substitute.For<ISolutionFallbackService>();
        fallback.ListProjectPathsAsync(Arg.Any<string>(), Arg.Any<IDiagnosticReporter>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<string>?>(null));

        var locator = new MsBuildLocatorService();
        var reporter = new CapturingReporter();
        locator.EnsureRegistered(reporter);

        var graphService = new ProjectGraphService(locator, fallback);
        var input = new ResolvedInput(nonExistentSlnx, Path.GetTempPath());

        // Act
        var plan = await graphService.BuildPlanAsync(input, null, null, null, reporter);

        // Assert
        Assert.Null(plan);
        Assert.Contains(reporter.Messages, m => m.Code == DiagnosticCode.TW2110);
        // Fallback must have been called for .slnx
        await fallback.Received(1).ListProjectPathsAsync(
            nonExistentSlnx, Arg.Any<IDiagnosticReporter>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Slnx_ProjectGraphFailure_FallbackProducesValidPlan()
    {
        // Arrange: use a non-existent .slnx path to force ProjectGraph failure,
        // but mock the fallback to return real .csproj paths so the second ProjectGraph succeeds.
        var nonExistentSlnx = Path.Combine(Path.GetTempPath(), "nonexistent_" + Guid.NewGuid() + ".slnx");
        var projectAPath = FixturePath("solution-slnx/ProjectA/ProjectA.csproj");
        var projectBPath = FixturePath("solution-slnx/ProjectB/ProjectB.csproj");

        var fallback = Substitute.For<ISolutionFallbackService>();
        fallback.ListProjectPathsAsync(Arg.Any<string>(), Arg.Any<IDiagnosticReporter>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<string>?>(new[] { projectAPath, projectBPath }));

        var locator = new MsBuildLocatorService();
        var restoreService = new RestoreService();
        var reporter = new CapturingReporter();
        locator.EnsureRegistered(reporter);

        // Ensure projects are restored so assets exist
        foreach (var projPath in new[] { projectAPath, projectBPath })
        {
            if (!await restoreService.CheckAssetsAsync(projPath))
                await restoreService.RestoreAsync(projPath, reporter);
        }

        var graphService = new ProjectGraphService(locator, fallback);
        var input = new ResolvedInput(nonExistentSlnx, Path.GetTempPath());

        // Act
        var plan = await graphService.BuildPlanAsync(input, null, null, null, reporter);

        // Assert: TW2110 is emitted for initial failure, but fallback produces a valid plan
        Assert.Contains(reporter.Messages, m => m.Code == DiagnosticCode.TW2110);
        Assert.NotNull(plan);
        Assert.Equal(2, plan.Targets.Count);
        Assert.All(plan.Targets, t => Assert.Equal("net10.0", t.TargetFramework));
    }
}
