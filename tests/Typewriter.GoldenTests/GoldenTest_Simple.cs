using Typewriter.GoldenTests.Infrastructure;
using Xunit;

namespace Typewriter.GoldenTests;

/// <summary>
/// Golden tests for the <c>simple</c> fixture — basic class, enum, and interface generation.
/// </summary>
public class GoldenTest_Simple : GoldenTestBase
{
    [Fact]
    public async Task Simple_GenerationMatchesBaselines()
    {
        var templatePaths = new[]
        {
            FixturePath("simple", "SimpleProject", "Enums.tst"),
            FixturePath("simple", "SimpleProject", "Interfaces.tst"),
        };

        var result = await RunGenerationAsync(
            templatePaths,
            projectPath: FixturePath("simple", "SimpleProject", "SimpleProject.csproj"),
            restore: true);

        AssertMatchesBaselines(result, BaselinePath("simple"));
    }
}
