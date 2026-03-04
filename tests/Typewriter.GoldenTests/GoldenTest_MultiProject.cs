using Typewriter.GoldenTests.Infrastructure;
using Xunit;

namespace Typewriter.GoldenTests;

/// <summary>
/// Golden tests for the <c>multi-project</c> fixture — cross-project type references
/// between DomainLib and ApiLib via a solution file.
/// </summary>
public class GoldenTest_MultiProject : GoldenTestBase
{
    [Fact]
    public async Task MultiProject_GenerationMatchesBaselines()
    {
        var templatePaths = new[]
        {
            FixturePath("multi-project", "CrossProjectTypes.tst"),
        };

        var result = await RunGenerationAsync(
            templatePaths,
            solutionPath: FixturePath("multi-project", "MultiProject.sln"),
            restore: true);

        AssertMatchesBaselines(result, BaselinePath("multi-project"));
    }
}
