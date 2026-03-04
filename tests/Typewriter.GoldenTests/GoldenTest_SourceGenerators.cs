using Typewriter.GoldenTests.Infrastructure;
using Xunit;

namespace Typewriter.GoldenTests;

/// <summary>
/// Golden tests for the <c>source-generators</c> fixture — validates visibility
/// of source-generator-produced types in the template metadata pipeline.
/// </summary>
public class GoldenTest_SourceGenerators : GoldenTestBase
{
    [Fact]
    public async Task SourceGenerators_GenerationMatchesBaselines()
    {
        var templatePaths = new[]
        {
            FixturePath("source-generators", "SourceGenTypes.tst"),
        };

        var result = await RunGenerationAsync(
            templatePaths,
            projectPath: FixturePath("source-generators", "SourceGenLib", "SourceGenLib.csproj"),
            restore: true);

        AssertMatchesBaselines(result, BaselinePath("source-generators"));
    }
}
