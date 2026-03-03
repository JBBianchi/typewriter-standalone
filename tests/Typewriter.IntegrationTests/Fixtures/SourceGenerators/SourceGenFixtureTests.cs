using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using Xunit;

namespace Typewriter.IntegrationTests.Fixtures.SourceGenerators;

/// <summary>
/// Verifies that the <c>SourceGenLib</c> fixture's source generator
/// (<see cref="SourceGenerator.HelloWorldGenerator"/>) produces
/// <c>SourceGenLib.GeneratedHelper</c> and that the type is visible in a Roslyn
/// <see cref="Compilation"/> via <see cref="Compilation.GetTypesByMetadataName"/>.
/// </summary>
public class SourceGenFixtureTests
{
    /// <summary>
    /// Confirms that running <see cref="SourceGenerator.HelloWorldGenerator"/> against a minimal
    /// compilation surfaces <c>SourceGenLib.GeneratedHelper</c> via
    /// <see cref="Compilation.GetTypesByMetadataName"/>.
    /// </summary>
    [Fact]
    public void HelloWorldGenerator_GeneratesGeneratedHelper_VisibleInCompilation()
    {
        // Arrange: build a minimal compilation that mirrors the SourceGenLib fixture project.
        var syntaxTree = CSharpSyntaxTree.ParseText(
            "namespace SourceGenLib { public sealed class Class1 { } }");

        var coreRef = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);

        var compilation = CSharpCompilation.Create(
            "SourceGenLib",
            syntaxTrees: [syntaxTree],
            references: [coreRef],
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // Act: run the incremental generator and obtain the updated compilation.
        var generator = new SourceGenerator.HelloWorldGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out var updatedCompilation,
            out _);

        // Assert: the generated type is present in the updated compilation.
        var types = updatedCompilation.GetTypesByMetadataName("SourceGenLib.GeneratedHelper");
        Assert.NotEmpty(types);
    }
}
