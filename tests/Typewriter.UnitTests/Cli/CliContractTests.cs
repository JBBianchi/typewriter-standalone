using RoslynCompilation = Microsoft.CodeAnalysis.Compilation;
using RoslynProject = Microsoft.CodeAnalysis.Project;
using Typewriter.Application;
using Typewriter.Application.Diagnostics;
using Typewriter.Application.Loading;
using Typewriter.Application.Orchestration;
using Typewriter.Generation.Output;
using Typewriter.Generation.Performance;
using Typewriter.Metadata.Roslyn;
using Xunit;

namespace Typewriter.UnitTests.Cli;

public class CliContractTests : IDisposable
{
    private readonly List<string> _tempFiles = new();

    /// <summary>Creates a minimal temporary <c>.tst</c> file and returns its absolute path.</summary>
    private string CreateTempTemplate(string content = "$Classes[$Name]")
    {
        var path = Path.Combine(Path.GetTempPath(), $"tw_test_{Guid.NewGuid():N}.tst");
        File.WriteAllText(path, content);
        _tempFiles.Add(path);
        return path;
    }

    public void Dispose()
    {
        foreach (var f in _tempFiles)
        {
            try { File.Delete(f); } catch { /* best-effort cleanup */ }
        }
    }

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

    /// <summary>Diagnostic reporter that captures messages for assertion.</summary>
    private sealed class CapturingDiagnosticReporter : IDiagnosticReporter
    {
        private readonly List<DiagnosticMessage> _messages;

        public CapturingDiagnosticReporter(List<DiagnosticMessage> messages)
        {
            _messages = messages;
        }

        public void Report(DiagnosticMessage message) => _messages.Add(message);

        public int WarningCount => _messages.Count(m => m.Severity == DiagnosticSeverity.Warning);
        public int ErrorCount => _messages.Count(m => m.Severity == DiagnosticSeverity.Error);
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

    /// <summary>Output writer stub that records written files without touching disk.</summary>
    private sealed class StubOutputWriter : IOutputWriter
    {
        public Task WriteAsync(string filePath, string content, bool addBom, CancellationToken ct)
            => Task.CompletedTask;
    }

    /// <summary>Output path policy stub that returns a deterministic path.</summary>
    private sealed class StubOutputPathPolicy : IOutputPathPolicy
    {
        public string Resolve(string templatePath, string sourceCsPath, int collisionIndex = 0)
            => Path.ChangeExtension(sourceCsPath, ".ts");
    }

    private static ApplicationRunner CreateRunner()
        => new ApplicationRunner(
            new StubInputResolver(),
            new StubRestoreService(),
            new StubProjectGraphService(),
            new StubRoslynWorkspaceService(),
            new StubOutputWriter(),
            new StubOutputPathPolicy(),
            new InvocationCache());

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
        var templatePath = CreateTempTemplate();

        var options = GenerateCommandOptions.Merge(
            config:        null,
            templates:     [templatePath],
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

    [Fact]
    public async Task Generate_EmptyWorkspace_Returns0()
    {
        // An empty workspace (no .cs files) means no metadata to render → still succeeds.
        var runner = CreateRunner();
        var reporter = new FakeDiagnosticReporter();
        var templatePath = CreateTempTemplate();

        var options = GenerateCommandOptions.Merge(
            config:        null,
            templates:     [templatePath],
            solution:      "my.sln",
            project:       null,
            framework:     null,
            configuration: null,
            runtime:       null,
            restore:       false,
            output:        null,
            verbosity:     null,
            failOnWarnings: false);

        var exitCode = await runner.RunAsync(options, reporter);

        Assert.Equal(0, exitCode);
        Assert.Equal(0, reporter.ErrorCount);
    }

    [Fact]
    public async Task Generate_NonExistentTemplate_Returns1WithTW3001()
    {
        // A non-existent template file is caught by the file-existence check → TW3001.
        var runner = CreateRunner();
        var messages = new List<DiagnosticMessage>();
        var reporter = new CapturingDiagnosticReporter(messages);

        var options = GenerateCommandOptions.Merge(
            config:        null,
            templates:     ["/nonexistent/path/template.tst"],
            solution:      "my.sln",
            project:       null,
            framework:     null,
            configuration: null,
            runtime:       null,
            restore:       false,
            output:        null,
            verbosity:     null,
            failOnWarnings: false);

        var exitCode = await runner.RunAsync(options, reporter);

        Assert.Equal(1, exitCode);
        Assert.Contains(messages, m => m.Code == DiagnosticCode.TW3001);
    }
}
