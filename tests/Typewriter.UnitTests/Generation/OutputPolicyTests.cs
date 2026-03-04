using Typewriter.Generation.Output;
using Xunit;

namespace Typewriter.UnitTests.Generation;

/// <summary>
/// Tests for <see cref="OutputPathPolicy"/> collision-avoidance and
/// <see cref="OutputWriter"/> skip-unchanged / BOM behaviour.
/// </summary>
public class OutputPolicyTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), $"tw_test_{Guid.NewGuid():N}");

    /// <summary>
    /// Initializes a fresh temporary directory for file-based tests.
    /// </summary>
    public OutputPolicyTests()
    {
        Directory.CreateDirectory(_tempDir);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    /// <summary>
    /// Verifies that <see cref="OutputPathPolicy.Resolve"/> produces the expected collision
    /// suffixes: no suffix for index 0, <c>_1</c> for index 1, and <c>_2</c> for index 2,
    /// matching upstream Typewriter semantics.
    /// </summary>
    [Fact]
    public void CollisionSequence_MatchesUpstream()
    {
        var policy = new OutputPathPolicy();
        var templatePath = Path.Combine("proj", "templates", "Model.tst");
        var sourcePath = Path.Combine("proj", "src", "Customer.cs");

        var path0 = policy.Resolve(templatePath, sourcePath, collisionIndex: 0);
        var path1 = policy.Resolve(templatePath, sourcePath, collisionIndex: 1);
        var path2 = policy.Resolve(templatePath, sourcePath, collisionIndex: 2);

        var expectedDir = Path.Combine("proj", "templates");
        Assert.Equal(Path.Combine(expectedDir, "Customer.ts"), path0);
        Assert.Equal(Path.Combine(expectedDir, "Customer_1.ts"), path1);
        Assert.Equal(Path.Combine(expectedDir, "Customer_2.ts"), path2);
    }

    /// <summary>
    /// Verifies that <see cref="OutputWriter.WriteAsync"/> skips the write when the target
    /// file already contains identical content, leaving the last-write timestamp unchanged.
    /// </summary>
    [Fact]
    public async Task UnchangedContent_SkipsWrite()
    {
        var writer = new OutputWriter();
        var filePath = Path.Combine(_tempDir, "unchanged.ts");
        const string content = "export interface Foo { bar: string; }";

        // Initial write.
        await writer.WriteAsync(filePath, content, addBom: false, CancellationToken.None);

        // Record the timestamp, then set it to a known past value so any write is detectable.
        var knownPast = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        File.SetLastWriteTimeUtc(filePath, knownPast);

        // Second write with identical content — should be skipped.
        await writer.WriteAsync(filePath, content, addBom: false, CancellationToken.None);

        var lastWrite = File.GetLastWriteTimeUtc(filePath);
        Assert.Equal(knownPast, lastWrite);
    }

    /// <summary>
    /// Verifies that <see cref="OutputWriter.WriteAsync"/> emits a UTF-8 BOM
    /// (<c>0xEF 0xBB 0xBF</c>) when <c>addBom</c> is <see langword="true"/>.
    /// </summary>
    [Fact]
    public async Task BomPolicy_IsRespected()
    {
        var writer = new OutputWriter();
        var filePath = Path.Combine(_tempDir, "withbom.ts");
        const string content = "// generated";

        await writer.WriteAsync(filePath, content, addBom: true, CancellationToken.None);

        var rawBytes = await File.ReadAllBytesAsync(filePath);
        Assert.True(rawBytes.Length >= 3, "File should contain at least 3 bytes for the BOM.");
        Assert.Equal(0xEF, rawBytes[0]);
        Assert.Equal(0xBB, rawBytes[1]);
        Assert.Equal(0xBF, rawBytes[2]);
    }
}
