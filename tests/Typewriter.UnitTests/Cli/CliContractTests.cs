using RoslynCompilation = Microsoft.CodeAnalysis.Compilation;
using RoslynProject = Microsoft.CodeAnalysis.Project;
using Typewriter.Application;
using Typewriter.Application.Diagnostics;
using Typewriter.Application.Loading;
using Typewriter.Application.Orchestration;
using Typewriter.Metadata.Roslyn;
using Xunit;

namespace Typewriter.UnitTests.Cli;

public class CliContractTests
{
    private sealed class FakeDiagnosticReporter : IDiagnosticReporter
    {
        private int _warningCount;
        private int _errorCount;

        public FakeDiagnosticReporter(int warningCount = 0, int errorCount = 0)
        {
            _warningCount = warningCount;
            _errorCount = errorCount;
        }

        public void Report(DiagnosticMessage message)
        {
            if (message.Severity == DiagnosticSeverity.Warning) _warningCount++;
            else if (message.Severity == DiagnosticSeverity.Error) _errorCount++;
        }

        public int WarningCount => _warningCount;
        public int ErrorCount => _errorCount;
    }

    /// <summary>Input resolver stub that always returns a successful resolved input.</summary>
    private sealed class StubInputResolver : IInputResolver
    {
        public Task<ResolvedInput?> ResolveAsync(string projectPath, IDiagnosticReporter reporter, CancellationToken ct = default)
            => Task.FromResult<ResolvedInput?>(new ResolvedInput(projectPath, null));
    }

    /// <summary>Restore service stub that reports assets as present.</summary>
    private sealed class StubRestoreService : IRestoreService
    {
        public Task<bool> CheckAssetsAsync(string projectPath, CancellationToken ct = default)
            => Task.FromResult(true);

        public Task<bool> RestoreAsync(string projectPath, IDiagnosticReporter reporter, CancellationToken ct = default)
            => Task.FromResult(true);
    }

    /// <summary>Project graph service stub that returns an empty but non-null load plan.</summary>
    private sealed class StubProjectGraphService : IProjectGraphService
    {
        public Task<ProjectLoadPlan?> BuildPlanAsync(
            ResolvedInput input,
            string? framework,
            string? configuration,
            string? runtime,
            IDiagnosticReporter reporter,
            CancellationToken ct = default)
        {
            var plan = new ProjectLoadPlan(input.ProjectPath, input.SolutionDirectory, [], new Dictionary<string, string>());
            return Task.FromResult<ProjectLoadPlan?>(plan);
        }
    }

    /// <summary>Roslyn workspace service stub that returns an empty but non-null workspace result.</summary>
    private sealed class StubRoslynWorkspaceService : IRoslynWorkspaceService
    {
        public Task<WorkspaceLoadResult?> LoadAsync(
            ProjectLoadPlan plan,
            IDiagnosticReporter reporter,
            CancellationToken ct = default)
            => Task.FromResult<WorkspaceLoadResult?>(new WorkspaceLoadResult([]));
    }

    private static ApplicationRunner CreateRunner()
        => new ApplicationRunner(
            new StubInputResolver(),
            new StubRestoreService(),
            new StubProjectGraphService(),
            new StubRoslynWorkspaceService());

    [Fact]
    public async Task Generate_InvalidArgs_Returns2()
    {
        var runner = CreateRunner();
        var reporter = new FakeDiagnosticReporter();

        // Empty templates + no solution/project → exit code 2
        var options = GenerateCommandOptions.Merge(
            config:        null,
            templates:     [],
            solution:      null,
            project:       null,
            framework:     null,
            configuration: null,
            runtime:       null,
            restore:       false,
            output:        null,
            verbosity:     null,
            failOnWarnings: false);

        var exitCode = await runner.RunAsync(options, reporter);

        Assert.Equal(2, exitCode);
    }

    [Fact]
    public async Task Generate_WarningsWithFailFlag_Returns1()
    {
        var runner = CreateRunner();
        // Pre-seed the reporter with 1 warning to simulate a prior warning being reported.
        var reporter = new FakeDiagnosticReporter(warningCount: 1);

        var options = GenerateCommandOptions.Merge(
            config:        null,
            templates:     ["tmpl.tst"],
            solution:      "my.sln",
            project:       null,
            framework:     null,
            configuration: null,
            runtime:       null,
            restore:       false,
            output:        null,
            verbosity:     null,
            failOnWarnings: true);

        var exitCode = await runner.RunAsync(options, reporter);

        Assert.Equal(1, exitCode);
    }
}
