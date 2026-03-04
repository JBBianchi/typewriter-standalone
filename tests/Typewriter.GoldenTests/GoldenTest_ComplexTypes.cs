using Typewriter.GoldenTests.Infrastructure;
using Xunit;

namespace Typewriter.GoldenTests;

/// <summary>
/// Golden tests for the <c>complex-types</c> fixture — nullable types, generics,
/// partial classes, async Task unwrapping, and collision naming.
/// </summary>
public class GoldenTest_ComplexTypes : GoldenTestBase
{
    [Fact]
    public async Task ComplexTypes_GenerationMatchesBaselines()
    {
        var templatePaths = new[]
        {
            FixturePath("complex-types", "ComplexTypesLib", "ComplexModels.tst"),
            FixturePath("complex-types", "ComplexTypesLib", "AsyncTypes.tst"),
        };

        var result = await RunGenerationAsync(
            templatePaths,
            projectPath: FixturePath("complex-types", "ComplexTypesLib", "ComplexTypesLib.csproj"),
            restore: true);

        AssertMatchesBaselines(result, BaselinePath("complex-types"));
    }
}
