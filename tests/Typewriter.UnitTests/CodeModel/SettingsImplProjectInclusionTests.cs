using Typewriter.CodeModel.Configuration;
using Xunit;

namespace Typewriter.UnitTests.CodeModel;

public class SettingsImplProjectInclusionTests
{
    [Fact]
    public void IncludeProject_UniqueName_AddsResolvedProjectPath()
    {
        var root = Path.Combine(Path.GetTempPath(), "tw-project-inclusion-tests", Guid.NewGuid().ToString("N"));
        var apiProjectPath = Path.GetFullPath(Path.Combine(root, "Api", "ApiLib.csproj"));
        var domainProjectPath = Path.GetFullPath(Path.Combine(root, "Domain", "DomainLib.csproj"));
        var context = new ProjectInclusionContext(
            Path.GetFullPath(Path.Combine(root, "MultiProject.sln")),
            [
                new ProjectInclusionTarget(apiProjectPath, "ApiLib", []),
                new ProjectInclusionTarget(domainProjectPath, "DomainLib", [])
            ]);

        var diagnostics = new List<ProjectInclusionDiagnostic>();
        var settings = new SettingsImpl(
            templatePath: Path.GetFullPath(Path.Combine(root, "templates", "Template.tst")),
            solutionFullName: context.EntryPath,
            projectInclusionContext: context,
            projectInclusionDiagnosticReporter: diagnostics.Add);

        settings.IncludeProject("ApiLib");

        Assert.True(settings.HasExplicitProjectSelection);
        Assert.Equal([apiProjectPath], settings.IncludedProjects);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void IncludeProject_AmbiguousName_EmitsDiagnostic_AndPathSelectorDisambiguates()
    {
        var root = Path.Combine(Path.GetTempPath(), "tw-project-inclusion-tests", Guid.NewGuid().ToString("N"));
        var firstProjectPath = Path.GetFullPath(Path.Combine(root, "src", "Shared", "Shared.csproj"));
        var secondProjectPath = Path.GetFullPath(Path.Combine(root, "tests", "Shared", "Shared.csproj"));
        var context = new ProjectInclusionContext(
            Path.GetFullPath(Path.Combine(root, "MultiProject.sln")),
            [
                new ProjectInclusionTarget(firstProjectPath, "Shared", []),
                new ProjectInclusionTarget(secondProjectPath, "Shared", [])
            ]);

        var diagnostics = new List<ProjectInclusionDiagnostic>();
        var settings = new SettingsImpl(
            templatePath: Path.GetFullPath(Path.Combine(root, "templates", "Template.tst")),
            solutionFullName: context.EntryPath,
            projectInclusionContext: context,
            projectInclusionDiagnosticReporter: diagnostics.Add);

        settings.IncludeProject("Shared");

        var ambiguity = Assert.Single(diagnostics);
        Assert.Equal("TW1202", ambiguity.Code);
        Assert.Empty(settings.IncludedProjects);

        settings.IncludeProject(Path.Combine("src", "Shared", "Shared.csproj"));

        Assert.Equal([firstProjectPath], settings.IncludedProjects);
    }

    [Fact]
    public void IncludeProject_NameAlias_AddsResolvedProjectPath()
    {
        var root = Path.Combine(Path.GetTempPath(), "tw-project-inclusion-tests", Guid.NewGuid().ToString("N"));
        var projectPath = Path.GetFullPath(Path.Combine(root, "Api", "ApiLib.csproj"));
        var context = new ProjectInclusionContext(
            Path.GetFullPath(Path.Combine(root, "MultiProject.sln")),
            [
                new ProjectInclusionTarget(projectPath, "ApiLib", [])
                {
                    NameAliases = ["Agencr.Platform.Modules.Agents.Integration", "ApiLib"]
                }
            ]);

        var diagnostics = new List<ProjectInclusionDiagnostic>();
        var settings = new SettingsImpl(
            templatePath: Path.GetFullPath(Path.Combine(root, "templates", "Template.tst")),
            solutionFullName: context.EntryPath,
            projectInclusionContext: context,
            projectInclusionDiagnosticReporter: diagnostics.Add);

        settings.IncludeProject("Agencr.Platform.Modules.Agents.Integration");

        Assert.True(settings.HasExplicitProjectSelection);
        Assert.Equal([projectPath], settings.IncludedProjects);
        Assert.Empty(diagnostics);
    }
}
