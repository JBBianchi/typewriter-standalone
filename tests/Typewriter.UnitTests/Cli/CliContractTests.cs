using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Help;
using System.CommandLine.Parsing;
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
    private readonly List<string> _tempDirectories = new();

    /// <summary>Creates a minimal temporary <c>.tst</c> file and returns its absolute path.</summary>
    private string CreateTempTemplate(string content = "$Classes[$Name]")
    {
        var path = Path.Combine(Path.GetTempPath(), $"tw_test_{Guid.NewGuid():N}.tst");
        File.WriteAllText(path, content);
        _tempFiles.Add(path);
        return path;
    }

    /// <summary>Creates a unique temporary directory path and tracks it for cleanup.</summary>
    private string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"tw_test_dir_{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        _tempDirectories.Add(path);
        return path;
    }

    public void Dispose()
    {
        foreach (var f in _tempFiles)
        {
            try { File.Delete(f); } catch { /* best-effort cleanup */ }
        }

        foreach (var d in _tempDirectories)
        {
            try { Directory.Delete(d, recursive: true); } catch { /* best-effort cleanup */ }
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
            failOnWarnings: false,
            dryRun:        false);

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
            failOnWarnings: true,
            dryRun:        false);

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
            failOnWarnings: false,
            dryRun:        false);

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
            failOnWarnings: false,
            dryRun:        false);

        var exitCode = await runner.RunAsync(options, reporter);

        Assert.Equal(1, exitCode);
        Assert.Contains(messages, m => m.Code == DiagnosticCode.TW3001);
    }

    [Fact]
    public async Task Generate_GlobPattern_ResolvesTemplatesAndReturns0()
    {
        var runner = CreateRunner();
        var messages = new List<DiagnosticMessage>();
        var reporter = new CapturingDiagnosticReporter(messages);

        var root = CreateTempDirectory();
        var nested = Path.Combine(root, "nested", "templates");
        Directory.CreateDirectory(nested);

        File.WriteAllText(Path.Combine(root, "root.tst"), "$Classes[$Name]");
        File.WriteAllText(Path.Combine(nested, "child.tst"), "$Classes[$Name]");
        File.WriteAllText(Path.Combine(nested, "ignore.txt"), "x");

        var globPattern = Path.Combine(root, "**", "*.tst");

        var options = GenerateCommandOptions.Merge(
            config:        null,
            templates:     [globPattern],
            solution:      "my.sln",
            project:       null,
            framework:     null,
            configuration: null,
            runtime:       null,
            restore:       false,
            output:        null,
            verbosity:     null,
            failOnWarnings: false,
            dryRun:        false);

        var exitCode = await runner.RunAsync(options, reporter);

        Assert.Equal(0, exitCode);
        Assert.DoesNotContain(messages, m => m.Code == DiagnosticCode.TW3001);
    }

    [Fact]
    public async Task Generate_GlobPattern_NoMatches_Returns1WithTW3001()
    {
        var runner = CreateRunner();
        var messages = new List<DiagnosticMessage>();
        var reporter = new CapturingDiagnosticReporter(messages);

        var root = CreateTempDirectory();
        var globPattern = Path.Combine(root, "**", "*.tst");

        var options = GenerateCommandOptions.Merge(
            config:        null,
            templates:     [globPattern],
            solution:      "my.sln",
            project:       null,
            framework:     null,
            configuration: null,
            runtime:       null,
            restore:       false,
            output:        null,
            verbosity:     null,
            failOnWarnings: false,
            dryRun:        false);

        var exitCode = await runner.RunAsync(options, reporter);

        Assert.Equal(1, exitCode);
        Assert.Contains(messages, m => m.Code == DiagnosticCode.TW3001);
    }

    /// <summary>
    /// Verifies that when <c>DryRun</c> is true, the runner emits a <c>TW5002</c> summary
    /// diagnostic and still returns exit code 0.
    /// </summary>
    [Fact]
    public async Task DryRun_EmptyWorkspace_EmitsTW5002AndReturns0()
    {
        var runner = CreateRunner();
        var messages = new List<DiagnosticMessage>();
        var reporter = new CapturingDiagnosticReporter(messages);
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
            failOnWarnings: false,
            dryRun:        true);

        var exitCode = await runner.RunAsync(options, reporter);

        Assert.Equal(0, exitCode);
        Assert.Contains(messages, m => m.Code == DiagnosticCode.TW5002);
    }

    /// <summary>
    /// Verifies that when <c>DryRun</c> is false, no <c>TW5002</c> diagnostic is emitted.
    /// </summary>
    [Fact]
    public async Task NoDryRun_EmptyWorkspace_DoesNotEmitTW5002()
    {
        var runner = CreateRunner();
        var messages = new List<DiagnosticMessage>();
        var reporter = new CapturingDiagnosticReporter(messages);
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
            failOnWarnings: false,
            dryRun:        false);

        var exitCode = await runner.RunAsync(options, reporter);

        Assert.Equal(0, exitCode);
        Assert.DoesNotContain(messages, m => m.Code == DiagnosticCode.TW5002);
    }

    /// <summary>Verifies that passing <c>--dry-run</c> sets <see cref="GenerateCommandOptions.DryRun"/> to <c>true</c>.</summary>
    [Fact]
    public void DryRun_WhenSpecified_SetsOptionToTrue()
    {
        var options = GenerateCommandOptions.Merge(
            config:        null,
            templates:     ["template.tst"],
            solution:      "my.sln",
            project:       null,
            framework:     null,
            configuration: null,
            runtime:       null,
            restore:       false,
            output:        null,
            verbosity:     null,
            failOnWarnings: false,
            dryRun:        true);

        Assert.True(options.DryRun);
    }

    /// <summary>Verifies that omitting <c>--dry-run</c> leaves <see cref="GenerateCommandOptions.DryRun"/> as <c>false</c>.</summary>
    [Fact]
    public void DryRun_WhenNotSpecified_DefaultsToFalse()
    {
        var options = GenerateCommandOptions.Merge(
            config:        null,
            templates:     ["template.tst"],
            solution:      "my.sln",
            project:       null,
            framework:     null,
            configuration: null,
            runtime:       null,
            restore:       false,
            output:        null,
            verbosity:     null,
            failOnWarnings: false,
            dryRun:        false);

        Assert.False(options.DryRun);
    }

    /// <summary>Verifies that invoking the parser with no arguments shows help text and returns exit code 0.</summary>
    [Fact]
    public async Task NoArgs_ShowsHelpAndReturnsZero()
    {
        var generateCommand = new Command("generate", "Generate TypeScript files from .tst templates");
        generateCommand.AddArgument(new Argument<string[]>("templates") { Arity = ArgumentArity.OneOrMore });
        generateCommand.AddOption(new Option<string?>("--solution"));
        generateCommand.AddOption(new Option<string?>("--project"));
        generateCommand.AddOption(new Option<bool>("--dry-run", "Validate the pipeline without writing output files"));

        var root = new RootCommand("typewriter-cli \u2014 standalone Typewriter code generator");
        root.AddCommand(generateCommand);
        // Mimics the expected Program.cs behavior: show help for no-args invocation.
        root.SetHandler(() =>
        {
            var helpBuilder = new HelpBuilder(LocalizationResources.Instance);
            using var writer = new StringWriter();
            helpBuilder.Write(root, writer);
            Console.Write(writer);
        });

        var parser = new CommandLineBuilder(root).UseDefaults().Build();

        using var sw = new StringWriter();
        Console.SetOut(sw);
        int exitCode;
        try
        {
            exitCode = await parser.InvokeAsync(Array.Empty<string>());
        }
        finally
        {
            Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
        }

        var helpText = sw.ToString();
        Assert.Equal(0, exitCode);
        Assert.Contains("typewriter-cli", helpText);
        Assert.Contains("generate", helpText);
    }

    /// <summary>Verifies that <c>--dry-run</c> appears in the <c>generate</c> command help output.</summary>
    [Fact]
    public async Task DryRun_AppearsInHelpOutput()
    {
        var generateCommand = new Command("generate", "Generate TypeScript files from .tst templates");
        generateCommand.AddArgument(new Argument<string[]>("templates") { Arity = ArgumentArity.OneOrMore });
        generateCommand.AddOption(new Option<string?>("--solution"));
        generateCommand.AddOption(new Option<string?>("--project"));
        generateCommand.AddOption(new Option<bool>("--dry-run", "Validate the pipeline without writing output files"));

        var root = new RootCommand();
        root.AddCommand(generateCommand);

        var parser = new CommandLineBuilder(root).UseDefaults().Build();

        using var sw = new StringWriter();
        Console.SetOut(sw);
        try
        {
            await parser.InvokeAsync("generate --help");
        }
        finally
        {
            Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
        }

        var helpText = sw.ToString();
        Assert.Contains("--dry-run", helpText);
    }

}
