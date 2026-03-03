using Typewriter.Application.Diagnostics;
using Typewriter.Application.Loading;
using Typewriter.Application.Orchestration;
using Typewriter.Loading.MSBuild;
using Xunit;

namespace Typewriter.IntegrationTests.Loading;

public class SolutionLoaderTests
{
    private sealed class FakeDiagnosticReporter : IDiagnosticReporter
    {
        private readonly List<DiagnosticMessage> _messages = [];

        public void Report(DiagnosticMessage message) => _messages.Add(message);

        public int WarningCount => _messages.Count(m => m.Severity == DiagnosticSeverity.Warning);
        public int ErrorCount => _messages.Count(m => m.Severity == DiagnosticSeverity.Error);
        public bool HasCode(string code) => _messages.Any(m => m.Code == code);
    }

    /// <summary>Stub IProjectGraphService that always throws to simulate ProjectGraph failure.</summary>
    private sealed class ThrowingProjectGraphService : IProjectGraphService
    {
        public Task<ProjectLoadPlan?> BuildPlanAsync(
            ResolvedInput input,
            string? framework,
            string? configuration,
            string? runtime,
            IDiagnosticReporter reporter,
            CancellationToken ct = default) =>
            throw new InvalidOperationException("Simulated ProjectGraph failure");
    }

    /// <summary>Stub ISolutionFallbackService that returns a pre-configured list of project paths.</summary>
    private sealed class StubSolutionFallbackService : ISolutionFallbackService
    {
        private readonly IReadOnlyList<string> _paths;
        public bool WasCalled { get; private set; }

        public StubSolutionFallbackService(IReadOnlyList<string> paths) => _paths = paths;

        public Task<IReadOnlyList<string>?> ListProjectPathsAsync(
            string slnxPath, IDiagnosticReporter reporter, CancellationToken ct)
        {
            WasCalled = true;
            return Task.FromResult<IReadOnlyList<string>?>(_paths);
        }
    }

    private static string FixturePath(string relative) =>
        Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            relative));

    private static SolutionLoader BuildRealLoader()
    {
        var locator = new MsBuildLocatorService();
        var fallback = new SolutionFallbackService();
        var graphService = new ProjectGraphService(locator, fallback);
        return new SolutionLoader(graphService, fallback);
    }

    private static async Task EnsureRestoredAsync(string solutionPath, IDiagnosticReporter reporter)
    {
        // Run dotnet restore on the solution so all projects have project.assets.json.
        // Restore is idempotent; running it unconditionally keeps the test self-contained.
        var restoreService = new RestoreService();
        await restoreService.RestoreAsync(solutionPath, reporter);
    }

    [Fact]
    public async Task Sln_LoadsExpectedProjects()
    {
        // Arrange
        var slnPath = FixturePath("tests/fixtures/solution-sln/SolutionSln.sln");
        var reporter = new FakeDiagnosticReporter();

        // MSBuildLocator must be registered before any MSBuild type references are JIT-compiled.
        new MsBuildLocatorService().EnsureRegistered(reporter);
        await EnsureRestoredAsync(slnPath, reporter);

        var loader = BuildRealLoader();
        var input = new ResolvedInput(slnPath, Path.GetDirectoryName(slnPath));

        // Act
        var plan = await loader.BuildPlanAsync(input, null, null, null, reporter);

        // Assert
        Assert.NotNull(plan);
        Assert.Equal(2, plan.Targets.Count);
        var names = plan.Targets.Select(t => t.ProjectName).Order().ToArray();
        Assert.Equal(new[] { "ProjectA", "ProjectB" }, names);
    }

    [Fact]
    public async Task Slnx_LoadsExpectedProjects()
    {
        // Arrange
        var slnxPath = FixturePath("tests/fixtures/solution-slnx/SolutionSlnx.slnx");
        var reporter = new FakeDiagnosticReporter();

        new MsBuildLocatorService().EnsureRegistered(reporter);
        await EnsureRestoredAsync(slnxPath, reporter);

        var loader = BuildRealLoader();
        var input = new ResolvedInput(slnxPath, Path.GetDirectoryName(slnxPath));

        // Act
        var plan = await loader.BuildPlanAsync(input, null, null, null, reporter);

        // Assert
        Assert.NotNull(plan);
        Assert.Equal(2, plan.Targets.Count);
        var names = plan.Targets.Select(t => t.ProjectName).Order().ToArray();
        Assert.Equal(new[] { "ProjectA", "ProjectB" }, names);
    }

    [Fact]
    public async Task SlnAndSlnx_ProduceSameTraversalPlan()
    {
        // Arrange
        var slnPath = FixturePath("tests/fixtures/solution-sln/SolutionSln.sln");
        var slnxPath = FixturePath("tests/fixtures/solution-slnx/SolutionSlnx.slnx");
        var reporter = new FakeDiagnosticReporter();

        new MsBuildLocatorService().EnsureRegistered(reporter);
        await EnsureRestoredAsync(slnPath, reporter);
        await EnsureRestoredAsync(slnxPath, reporter);

        var slnLoader = BuildRealLoader();
        var slnxLoader = BuildRealLoader();

        var slnInput = new ResolvedInput(slnPath, Path.GetDirectoryName(slnPath));
        var slnxInput = new ResolvedInput(slnxPath, Path.GetDirectoryName(slnxPath));

        // Act
        var slnPlan = await slnLoader.BuildPlanAsync(slnInput, null, null, null, reporter);
        var slnxPlan = await slnxLoader.BuildPlanAsync(slnxInput, null, null, null, reporter);

        // Assert: both plans load the same set of projects in the same topological order.
        // Paths differ (separate fixture dirs) so compare by project name in traversal order.
        Assert.NotNull(slnPlan);
        Assert.NotNull(slnxPlan);
        Assert.Equal(slnPlan.Targets.Count, slnxPlan.Targets.Count);

        var slnNames = slnPlan.Targets
            .OrderBy(t => t.TraversalOrder)
            .Select(t => t.ProjectName)
            .ToArray();
        var slnxNames = slnxPlan.Targets
            .OrderBy(t => t.TraversalOrder)
            .Select(t => t.ProjectName)
            .ToArray();

        Assert.Equal(slnNames, slnxNames);
    }

    [Fact]
    public async Task Slnx_WhenGraphFails_UsesFallback()
    {
        // Arrange: use a throwing stub for IProjectGraphService to simulate ProjectGraph failure.
        var slnxPath = FixturePath("tests/fixtures/solution-slnx/SolutionSlnx.slnx");
        var projectAPath = FixturePath("tests/fixtures/solution-slnx/ProjectA/ProjectA.csproj");
        var projectBPath = FixturePath("tests/fixtures/solution-slnx/ProjectB/ProjectB.csproj");

        var throwingGraph = new ThrowingProjectGraphService();
        var stubFallback = new StubSolutionFallbackService([projectAPath, projectBPath]);
        var loader = new SolutionLoader(throwingGraph, stubFallback);
        var reporter = new FakeDiagnosticReporter();

        var input = new ResolvedInput(slnxPath, Path.GetDirectoryName(slnxPath));

        // Act
        var plan = await loader.BuildPlanAsync(input, null, null, null, reporter);

        // Assert: TW2110 emitted, fallback invoked, valid plan produced
        Assert.True(reporter.HasCode(DiagnosticCode.TW2110), "Expected TW2110 to be emitted when ProjectGraph fails");
        Assert.True(stubFallback.WasCalled, "Expected ISolutionFallbackService to be invoked as fallback");
        Assert.NotNull(plan);
        Assert.Equal(2, plan.Targets.Count);
        var names = plan.Targets.Select(t => t.ProjectName).Order().ToArray();
        Assert.Equal(new[] { "ProjectA", "ProjectB" }, names);
    }
}
