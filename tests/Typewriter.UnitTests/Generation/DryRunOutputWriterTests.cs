using NSubstitute;
using Typewriter.Generation.Output;
using Xunit;

namespace Typewriter.UnitTests.Generation;

/// <summary>
/// Tests for <see cref="DryRunOutputWriter"/> decorator behavior:
/// no file I/O, diagnostic callback invocation, and file-count tracking.
/// </summary>
public class DryRunOutputWriterTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), $"tw_dryrun_{Guid.NewGuid():N}");
    private readonly IOutputWriter _inner = Substitute.For<IOutputWriter>();

    /// <summary>
    /// Creates a fresh temporary directory to verify no files are written.
    /// </summary>
    public DryRunOutputWriterTests()
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
    /// Verifies that <see cref="DryRunOutputWriter.WriteAsync"/> does not create any files on disk.
    /// </summary>
    [Fact]
    public async Task WriteAsync_DoesNotCreateFiles()
    {
        var sut = new DryRunOutputWriter(_inner);
        var filePath = Path.Combine(_tempDir, "Model.ts");

        await sut.WriteAsync(filePath, "export interface Model {}", addBom: false, CancellationToken.None);

        Assert.False(File.Exists(filePath));
        Assert.Empty(Directory.GetFiles(_tempDir));
    }

    /// <summary>
    /// Verifies that <see cref="DryRunOutputWriter.WriteAsync"/> does not delegate to the inner writer.
    /// </summary>
    [Fact]
    public async Task WriteAsync_DoesNotCallInnerWriter()
    {
        var sut = new DryRunOutputWriter(_inner);
        var filePath = Path.Combine(_tempDir, "Model.ts");

        await sut.WriteAsync(filePath, "content", addBom: false, CancellationToken.None);

        await _inner.DidNotReceive().WriteAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that the <c>onFileRecorded</c> callback is invoked with the file path
    /// for each call to <see cref="DryRunOutputWriter.WriteAsync"/>, enabling TW5001 diagnostic emission.
    /// </summary>
    [Fact]
    public async Task WriteAsync_InvokesCallbackPerFile()
    {
        var recorded = new List<string>();
        var sut = new DryRunOutputWriter(_inner, onFileRecorded: path => recorded.Add(path));

        var path1 = Path.Combine(_tempDir, "A.ts");
        var path2 = Path.Combine(_tempDir, "B.ts");

        await sut.WriteAsync(path1, "a", addBom: false, CancellationToken.None);
        await sut.WriteAsync(path2, "b", addBom: true, CancellationToken.None);

        Assert.Equal(2, recorded.Count);
        Assert.Equal(path1, recorded[0]);
        Assert.Equal(path2, recorded[1]);
    }

    /// <summary>
    /// Verifies that <see cref="DryRunOutputWriter.WriteAsync"/> works without a callback.
    /// </summary>
    [Fact]
    public async Task WriteAsync_WithoutCallback_DoesNotThrow()
    {
        var sut = new DryRunOutputWriter(_inner);

        await sut.WriteAsync(Path.Combine(_tempDir, "X.ts"), "x", addBom: false, CancellationToken.None);

        Assert.Equal(1, sut.FileCount);
    }

    /// <summary>
    /// Verifies that <see cref="DryRunOutputWriter.FileCount"/> correctly tracks the number
    /// of files that would have been written.
    /// </summary>
    [Fact]
    public async Task FileCount_TracksWriteCalls()
    {
        var sut = new DryRunOutputWriter(_inner);

        Assert.Equal(0, sut.FileCount);

        await sut.WriteAsync(Path.Combine(_tempDir, "1.ts"), "a", addBom: false, CancellationToken.None);
        Assert.Equal(1, sut.FileCount);

        await sut.WriteAsync(Path.Combine(_tempDir, "2.ts"), "b", addBom: false, CancellationToken.None);
        Assert.Equal(2, sut.FileCount);

        await sut.WriteAsync(Path.Combine(_tempDir, "3.ts"), "c", addBom: true, CancellationToken.None);
        Assert.Equal(3, sut.FileCount);
    }

    /// <summary>
    /// Verifies that <see cref="DryRunOutputWriter.RecordedPaths"/> contains all paths
    /// passed to <see cref="DryRunOutputWriter.WriteAsync"/> calls.
    /// </summary>
    [Fact]
    public async Task RecordedPaths_ContainsAllWrittenPaths()
    {
        var sut = new DryRunOutputWriter(_inner);

        var paths = new[]
        {
            Path.Combine(_tempDir, "Alpha.ts"),
            Path.Combine(_tempDir, "Beta.ts"),
            Path.Combine(_tempDir, "Gamma.ts"),
        };

        foreach (var p in paths)
        {
            await sut.WriteAsync(p, "content", addBom: false, CancellationToken.None);
        }

        Assert.Equal(paths.Length, sut.RecordedPaths.Count);
        foreach (var p in paths)
        {
            Assert.Contains(p, sut.RecordedPaths);
        }
    }

    /// <summary>
    /// Verifies that the constructor rejects a <see langword="null"/> inner writer.
    /// </summary>
    [Fact]
    public void Constructor_NullInner_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new DryRunOutputWriter(null!));
    }
}
