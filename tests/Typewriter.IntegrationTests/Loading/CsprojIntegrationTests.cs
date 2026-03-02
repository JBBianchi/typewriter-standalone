using Typewriter.Application.Diagnostics;
using Typewriter.Loading.MSBuild;
using Xunit;

namespace Typewriter.IntegrationTests.Loading;

public class CsprojIntegrationTests
{
    private sealed class FakeDiagnosticReporter : IDiagnosticReporter
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

    [Fact]
    public async Task SimpleLib_Csproj_ProducesValidProjectLoadPlan()
    {
        // Arrange
        var fixturePath = Path.GetFullPath(
            Path.Combine(
                AppContext.BaseDirectory,
                "..", "..", "..", "..", "..",
                "tests", "fixtures", "SimpleLib", "SimpleLib.csproj"
            )
        );

        // Wire real services (no mocks)
        var locator = new MsBuildLocatorService();
        var restoreService = new RestoreService();
        var graphService = new ProjectGraphService(locator);
        var resolver = new InputResolver();
        var reporter = new FakeDiagnosticReporter();

        // MSBuildLocator must be registered before any method that JIT-compiles MSBuild
        // type references is called. RegisterDefaults() sets up the AssemblyResolve handler
        // so that Microsoft.Build assemblies are resolved from the SDK installation.
        locator.EnsureRegistered(reporter);

        // Act
        var resolved = await resolver.ResolveAsync(fixturePath, reporter);
        Assert.NotNull(resolved);

        // Ensure assets exist (run restore if needed in CI)
        var hasAssets = await restoreService.CheckAssetsAsync(resolved.ProjectPath);
        if (!hasAssets)
        {
            var restored = await restoreService.RestoreAsync(resolved.ProjectPath, reporter);
            Assert.True(restored, "dotnet restore failed for SimpleLib fixture");
        }

        var plan = await graphService.BuildPlanAsync(resolved, null, null, null, reporter);

        // Assert
        Assert.NotNull(plan);
        Assert.NotEmpty(plan.Targets);
        Assert.Equal("net10.0", plan.Targets[0].TargetFramework);
    }
}
