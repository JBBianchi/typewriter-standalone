using Typewriter.Application;
using Typewriter.Application.Diagnostics;
using Typewriter.Generation.Output;
using Typewriter.Generation.Performance;
using Typewriter.Loading.MSBuild;
using Xunit;

namespace Typewriter.IntegrationTests;

[Collection("ApplicationRunner")]
public class IncludeProjectIntegrationTests : IDisposable
{
    private sealed class CapturingOutputWriter : IOutputWriter
    {
        private readonly Dictionary<string, string> _outputs = new(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyDictionary<string, string> Outputs => _outputs;

        public Task WriteAsync(string path, string content, bool writeBom, CancellationToken ct = default)
        {
            _outputs[path] = content;
            return Task.CompletedTask;
        }
    }

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

    private readonly string _tempDir;

    public IncludeProjectIntegrationTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "tw-includeproject-tests-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (!Directory.Exists(_tempDir))
        {
            return;
        }

        try
        {
            Directory.Delete(_tempDir, recursive: true);
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static string FixturePath(string relativePath) =>
        Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "tests", "fixtures",
            relativePath));

    [Fact]
    public async Task IncludeProject_FiltersGenerationToNamedProject()
    {
        var solutionPath = FixturePath("multi-project/MultiProject.sln");
        var templatePath = Path.Combine(_tempDir, "IncludeApiOnly.tst");
        await File.WriteAllTextAsync(templatePath, """
            ${
                public Template(Settings settings)
                {
                    settings.IncludeProject("ApiLib");
                }
            }

            $Classes(*)[
            export class $Name {}
            ]
            """);

        var reporter = new CapturingReporter();
        var capturingWriter = new CapturingOutputWriter();

        var cache = new InvocationCache();
        var locator = new MsBuildLocatorService();
        var inputResolver = new InputResolver();
        var restoreService = new RestoreService();
        var solutionFallbackService = new SolutionFallbackService();
        var projectGraphService = new ProjectGraphService(locator, solutionFallbackService);
        var roslynWorkspaceService = new RoslynWorkspaceService(cache);
        var outputPathPolicy = new OutputPathPolicy();

        locator.EnsureRegistered(reporter);

        if (!await restoreService.CheckAssetsAsync(FixturePath("multi-project/ApiLib/ApiLib.csproj")))
        {
            var restored = await restoreService.RestoreAsync(solutionPath, reporter);
            Assert.True(restored, "dotnet restore failed for multi-project fixture");
        }

        var runner = new ApplicationRunner(
            inputResolver,
            restoreService,
            projectGraphService,
            roslynWorkspaceService,
            capturingWriter,
            outputPathPolicy,
            cache);

        var options = GenerateCommandOptions.Merge(
            config: null,
            templates: [templatePath],
            solution: solutionPath,
            project: null,
            framework: null,
            configuration: null,
            runtime: null,
            restore: false,
            output: null,
            verbosity: null,
            failOnWarnings: false,
            dryRun: false);

        var exitCode = await runner.RunAsync(options, reporter);

        Assert.Equal(0, exitCode);
        Assert.DoesNotContain(reporter.Messages, m => m.Code == DiagnosticCode.TW1201 || m.Code == DiagnosticCode.TW1202);

        var generatedFiles = capturingWriter.Outputs.Keys
            .Select(path => Path.GetFileName(path)!)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.Equal(["OrderEntity.ts", "UserEntity.ts"], generatedFiles);
    }

    [Fact]
    public async Task IncludeProject_FiltersGenerationByAssemblyNameAlias()
    {
        var projectDir = Path.Combine(_tempDir, "alias-project");
        Directory.CreateDirectory(projectDir);

        var projectPath = Path.Combine(projectDir, "AliasProject.csproj");
        await File.WriteAllTextAsync(projectPath, """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <AssemblyName>Agencr.Platform.Modules.Agents.Integration</AssemblyName>
              </PropertyGroup>
            </Project>
            """);

        await File.WriteAllTextAsync(Path.Combine(projectDir, "AliasEntity.cs"), """
            namespace AliasProject;
            public class AliasEntity {}
            """);

        var templatePath = Path.Combine(_tempDir, "IncludeAssemblyNameAlias.tst");
        await File.WriteAllTextAsync(templatePath, """
            ${
                public Template(Settings settings)
                {
                    settings.IncludeProject("Agencr.Platform.Modules.Agents.Integration");
                }
            }

            $Classes(*)[
            export class $Name {}
            ]
            """);

        var reporter = new CapturingReporter();
        var capturingWriter = new CapturingOutputWriter();

        var cache = new InvocationCache();
        var locator = new MsBuildLocatorService();
        var inputResolver = new InputResolver();
        var restoreService = new RestoreService();
        var solutionFallbackService = new SolutionFallbackService();
        var projectGraphService = new ProjectGraphService(locator, solutionFallbackService);
        var roslynWorkspaceService = new RoslynWorkspaceService(cache);
        var outputPathPolicy = new OutputPathPolicy();

        locator.EnsureRegistered(reporter);

        if (!await restoreService.CheckAssetsAsync(projectPath))
        {
            var restored = await restoreService.RestoreAsync(projectPath, reporter);
            Assert.True(restored, "dotnet restore failed for assembly-name alias project");
        }

        var runner = new ApplicationRunner(
            inputResolver,
            restoreService,
            projectGraphService,
            roslynWorkspaceService,
            capturingWriter,
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
            dryRun: false);

        var exitCode = await runner.RunAsync(options, reporter);

        Assert.Equal(0, exitCode);
        Assert.DoesNotContain(reporter.Messages, m => m.Code == DiagnosticCode.TW1201 || m.Code == DiagnosticCode.TW1202);
        Assert.Contains(capturingWriter.Outputs, output => output.Value.Contains("export class AliasEntity {}", StringComparison.Ordinal));
    }
}
