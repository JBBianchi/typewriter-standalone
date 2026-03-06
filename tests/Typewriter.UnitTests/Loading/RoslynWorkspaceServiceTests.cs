using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Typewriter.Loading.MSBuild;
using Xunit;

namespace Typewriter.UnitTests.Loading;

public class RoslynWorkspaceServiceTests
{
    [Fact]
    public void IsActionableCompilationError_ReturnsTrue_ForRegularSourceError()
    {
        var diagnostic = CreateDiagnostic(
            "CS0246",
            DiagnosticSeverity.Error,
            @"C:\repo\src\Project\File.cs");

        var result = RoslynWorkspaceService.IsActionableCompilationError(diagnostic);

        Assert.True(result);
    }

    [Fact]
    public void IsActionableCompilationError_ReturnsFalse_ForGeneratedObjFile()
    {
        var diagnostic = CreateDiagnostic(
            "CS0246",
            DiagnosticSeverity.Error,
            @"C:\repo\src\Project\obj\Debug\net10.0\Generated.g.cs");

        var result = RoslynWorkspaceService.IsActionableCompilationError(diagnostic);

        Assert.False(result);
    }

    [Fact]
    public void IsActionableCompilationError_ReturnsFalse_ForLocationNone()
    {
        var diagnostic = Diagnostic.Create(
            new DiagnosticDescriptor(
                id: "CS9999",
                title: "Synthetic",
                messageFormat: "Synthetic diagnostic",
                category: "Compiler",
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true),
            Location.None);

        var result = RoslynWorkspaceService.IsActionableCompilationError(diagnostic);

        Assert.False(result);
    }

    [Fact]
    public void IsActionableCompilationError_ReturnsFalse_ForWarning()
    {
        var diagnostic = CreateDiagnostic(
            "CS0168",
            DiagnosticSeverity.Warning,
            @"C:\repo\src\Project\File.cs");

        var result = RoslynWorkspaceService.IsActionableCompilationError(diagnostic);

        Assert.False(result);
    }

    private static Diagnostic CreateDiagnostic(string id, DiagnosticSeverity severity, string path)
    {
        var descriptor = new DiagnosticDescriptor(
            id: id,
            title: "Synthetic",
            messageFormat: "Synthetic diagnostic",
            category: "Compiler",
            defaultSeverity: severity,
            isEnabledByDefault: true);

        var tree = CSharpSyntaxTree.ParseText("class C { }", path: path);
        var location = Location.Create(tree, new TextSpan(0, 1));

        return Diagnostic.Create(descriptor, location);
    }
}
