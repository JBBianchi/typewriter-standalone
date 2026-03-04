using Typewriter.GoldenTests.Infrastructure;
using Xunit;

namespace Typewriter.GoldenTests;

/// <summary>
/// Golden tests for the <c>multi-target</c> fixture — dual-target TFM selection (net10.0;net8.0).
/// </summary>
public class GoldenTest_MultiTarget : GoldenTestBase
{
    [Fact]
    public async Task MultiTarget_GenerationMatchesBaselines()
    {
        var templatePaths = new[]
        {
            FixturePath("multi-target", "MultiTargetLib", "PlatformInfo.tst"),
        };

        var result = await RunGenerationAsync(
            templatePaths,
            projectPath: FixturePath("multi-target", "MultiTargetLib", "MultiTargetLib.csproj"),
            restore: true);

        AssertMatchesBaselines(result, BaselinePath("multi-target"));
    }
}
