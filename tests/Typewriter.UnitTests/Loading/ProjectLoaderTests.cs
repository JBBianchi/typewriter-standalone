using RoslynCompilation = Microsoft.CodeAnalysis.Compilation;
using RoslynProject = Microsoft.CodeAnalysis.Project;
using NSubstitute;
using Typewriter.Application;
using Typewriter.Application.Diagnostics;
using Typewriter.Application.Loading;
using Typewriter.Application.Orchestration;
using Typewriter.Generation.Output;
using Typewriter.Generation.Performance;
using Typewriter.Metadata.Roslyn;
using Xunit;

namespace Typewriter.UnitTests.Loading;

public class ProjectLoaderTests : IDisposable
{
    private const string ProjectPath = "SimpleLib/SimpleLib.csproj";
    private readonly string _templatePath;

    public ProjectLoaderTests()
    {
        _templatePath = Path.Combine(Path.GetTempPath(), $"tw_test_{Guid.NewGuid():N}.tst");
        File.WriteAllText(_templatePath, "$Classes[$Name]");
    }

    public void Dispose()
    {
        try { File.Delete(_templatePath); } catch { /* best-effort cleanup */ }
    }

    private static ProjectLoadPlan ValidPlan(string projectPath) =>
        new ProjectLoadPlan(
            projectPath,
            null,
            [new LoadTarget(projectPath, "SimpleLib", "net10.0", null, null, 0)],
            new Dictionary<string, string>());

    private const string SolutionPath = "MySolution.sln";

    private GenerateCommandOptions MakeOptions(bool restore, string project = ProjectPath) =>
        GenerateCommandOptions.Merge(
            config: null,
            templates: [_templatePath],
            solution: null,
            project: project,
            framework: null,
            configuration: null,
            runtime: null,
            restore: restore,
            output: null,
            verbosity: null,
            failOnWarnings: false,
            dryRun:        false);

    [Fact]
    public async Task Csproj_LoadsWithoutRestore_WhenAssetsExist()
    {
        // Arrange
        var inputResolver = Substitute.For<IInputResolver>();
        var restoreService = Substitute.For<IRestoreService>();
        var graphService = Substitute.For<IProjectGraphService>();
        var roslynWorkspaceService = Substitute.For<IRoslynWorkspaceService>();
        var reporter = Substitute.For<IDiagnosticReporter>();

        inputResolver
            .ResolveAsync(Arg.Any<string>(), Arg.Any<IDiagnosticReporter>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ResolvedInput?>(new ResolvedInput(ProjectPath, null)));

        restoreService
            .CheckAssetsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        graphService
            .BuildPlanAsync(
                Arg.Any<ResolvedInput>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<IDiagnosticReporter>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ProjectLoadPlan?>(ValidPlan(ProjectPath)));

        roslynWorkspaceService
            .LoadAsync(Arg.Any<ProjectLoadPlan>(), Arg.Any<IDiagnosticReporter>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<WorkspaceLoadResult?>(new WorkspaceLoadResult([])));

        var runner = new ApplicationRunner(inputResolver, restoreService, graphService, roslynWorkspaceService, Substitute.For<IOutputWriter>(), Substitute.For<IOutputPathPolicy>(), new InvocationCache());

        // Act
        var exitCode = await runner.RunAsync(MakeOptions(restore: false), reporter);

        // Assert
        Assert.Equal(0, exitCode);
        await restoreService
            .DidNotReceive()
            .RestoreAsync(Arg.Any<string>(), Arg.Any<IDiagnosticReporter>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Csproj_MissingAssetsWithoutRestore_ReturnsTW2003()
    {
        // Arrange
        var inputResolver = Substitute.For<IInputResolver>();
        var restoreService = Substitute.For<IRestoreService>();
        var graphService = Substitute.For<IProjectGraphService>();
        var roslynWorkspaceService = Substitute.For<IRoslynWorkspaceService>();
        var reporter = Substitute.For<IDiagnosticReporter>();

        inputResolver
            .ResolveAsync(Arg.Any<string>(), Arg.Any<IDiagnosticReporter>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ResolvedInput?>(new ResolvedInput(ProjectPath, null)));

        restoreService
            .CheckAssetsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));

        var runner = new ApplicationRunner(inputResolver, restoreService, graphService, roslynWorkspaceService, Substitute.For<IOutputWriter>(), Substitute.For<IOutputPathPolicy>(), new InvocationCache());

        // Act
        var exitCode = await runner.RunAsync(MakeOptions(restore: false), reporter);

        // Assert
        Assert.Equal(3, exitCode);
        reporter.Received().Report(Arg.Is<DiagnosticMessage>(m => m.Code == DiagnosticCode.TW2003));
    }

    [Fact]
    public async Task Csproj_WithRestore_LoadsAfterRestore()
    {
        // Arrange
        var inputResolver = Substitute.For<IInputResolver>();
        var restoreService = Substitute.For<IRestoreService>();
        var graphService = Substitute.For<IProjectGraphService>();
        var roslynWorkspaceService = Substitute.For<IRoslynWorkspaceService>();
        var reporter = Substitute.For<IDiagnosticReporter>();

        inputResolver
            .ResolveAsync(Arg.Any<string>(), Arg.Any<IDiagnosticReporter>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ResolvedInput?>(new ResolvedInput(ProjectPath, null)));

        restoreService
            .CheckAssetsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));

        restoreService
            .RestoreAsync(Arg.Any<string>(), Arg.Any<IDiagnosticReporter>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        graphService
            .BuildPlanAsync(
                Arg.Any<ResolvedInput>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<IDiagnosticReporter>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ProjectLoadPlan?>(ValidPlan(ProjectPath)));

        roslynWorkspaceService
            .LoadAsync(Arg.Any<ProjectLoadPlan>(), Arg.Any<IDiagnosticReporter>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<WorkspaceLoadResult?>(new WorkspaceLoadResult([])));

        var runner = new ApplicationRunner(inputResolver, restoreService, graphService, roslynWorkspaceService, Substitute.For<IOutputWriter>(), Substitute.For<IOutputPathPolicy>(), new InvocationCache());

        // Act
        var exitCode = await runner.RunAsync(MakeOptions(restore: true), reporter);

        // Assert
        Assert.Equal(0, exitCode);
        await restoreService
            .Received(1)
            .RestoreAsync(Arg.Any<string>(), Arg.Any<IDiagnosticReporter>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Solution_SkipsCheckAssets_DoesNotTriggerTW2003()
    {
        // Arrange
        var inputResolver = Substitute.For<IInputResolver>();
        var restoreService = Substitute.For<IRestoreService>();
        var graphService = Substitute.For<IProjectGraphService>();
        var roslynWorkspaceService = Substitute.For<IRoslynWorkspaceService>();
        var reporter = Substitute.For<IDiagnosticReporter>();

        inputResolver
            .ResolveAsync(Arg.Any<string>(), Arg.Any<IDiagnosticReporter>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ResolvedInput?>(new ResolvedInput(SolutionPath, null)));

        graphService
            .BuildPlanAsync(
                Arg.Any<ResolvedInput>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<IDiagnosticReporter>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ProjectLoadPlan?>(ValidPlan(ProjectPath)));

        roslynWorkspaceService
            .LoadAsync(Arg.Any<ProjectLoadPlan>(), Arg.Any<IDiagnosticReporter>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<WorkspaceLoadResult?>(new WorkspaceLoadResult([])));

        var runner = new ApplicationRunner(inputResolver, restoreService, graphService, roslynWorkspaceService, Substitute.For<IOutputWriter>(), Substitute.For<IOutputPathPolicy>(), new InvocationCache());

        // Act
        var exitCode = await runner.RunAsync(MakeOptions(restore: false, project: SolutionPath), reporter);

        // Assert
        Assert.Equal(0, exitCode);
        await restoreService
            .DidNotReceive()
            .CheckAssetsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        reporter.DidNotReceive().Report(Arg.Is<DiagnosticMessage>(m => m.Code == DiagnosticCode.TW2003));
    }

    [Fact]
    public async Task Solution_WithRestore_CallsRestoreAsync()
    {
        // Arrange
        var inputResolver = Substitute.For<IInputResolver>();
        var restoreService = Substitute.For<IRestoreService>();
        var graphService = Substitute.For<IProjectGraphService>();
        var roslynWorkspaceService = Substitute.For<IRoslynWorkspaceService>();
        var reporter = Substitute.For<IDiagnosticReporter>();

        inputResolver
            .ResolveAsync(Arg.Any<string>(), Arg.Any<IDiagnosticReporter>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ResolvedInput?>(new ResolvedInput(SolutionPath, null)));

        restoreService
            .RestoreAsync(Arg.Any<string>(), Arg.Any<IDiagnosticReporter>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        graphService
            .BuildPlanAsync(
                Arg.Any<ResolvedInput>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<IDiagnosticReporter>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ProjectLoadPlan?>(ValidPlan(ProjectPath)));

        roslynWorkspaceService
            .LoadAsync(Arg.Any<ProjectLoadPlan>(), Arg.Any<IDiagnosticReporter>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<WorkspaceLoadResult?>(new WorkspaceLoadResult([])));

        var runner = new ApplicationRunner(inputResolver, restoreService, graphService, roslynWorkspaceService, Substitute.For<IOutputWriter>(), Substitute.For<IOutputPathPolicy>(), new InvocationCache());

        // Act
        var exitCode = await runner.RunAsync(MakeOptions(restore: true, project: SolutionPath), reporter);

        // Assert
        Assert.Equal(0, exitCode);
        await restoreService
            .Received(1)
            .RestoreAsync(SolutionPath, Arg.Any<IDiagnosticReporter>(), Arg.Any<CancellationToken>());
        await restoreService
            .DidNotReceive()
            .CheckAssetsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
