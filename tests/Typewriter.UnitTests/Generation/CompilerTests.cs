using Typewriter.Generation;
using Typewriter.Generation.Performance;
using Xunit;

namespace Typewriter.UnitTests.Generation;

/// <summary>
/// Tests for <see cref="Compiler"/> IDisposable behavior.
/// </summary>
public class CompilerTests
{
    /// <summary>
    /// Verifies that Dispose deletes the per-invocation subdirectory when it exists.
    /// </summary>
    [Fact]
    public void Dispose_DeletesSubDirectory_WhenItExists()
    {
        // Arrange
        var compiler = new Compiler(new InvocationCache());

        // Force the subdirectory to be created by accessing it indirectly.
        // We use reflection to read _subDirectory since it's private.
        var subDirField = typeof(Compiler).GetField("_subDirectory",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var subDir = (string)subDirField.GetValue(compiler)!;

        Directory.CreateDirectory(subDir);
        // Place a file inside to verify recursive deletion.
        File.WriteAllText(Path.Combine(subDir, "test.txt"), "test");

        Assert.True(Directory.Exists(subDir));

        // Act
        compiler.Dispose();

        // Assert
        Assert.False(Directory.Exists(subDir));
    }

    /// <summary>
    /// Verifies that Dispose does not throw when the subdirectory does not exist.
    /// </summary>
    [Fact]
    public void Dispose_DoesNotThrow_WhenSubDirectoryDoesNotExist()
    {
        // Arrange
        var compiler = new Compiler(new InvocationCache());

        // Act & Assert – should not throw
        compiler.Dispose();
    }

    /// <summary>
    /// Verifies that calling Dispose multiple times does not throw.
    /// </summary>
    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var compiler = new Compiler(new InvocationCache());
        var subDirField = typeof(Compiler).GetField("_subDirectory",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var subDir = (string)subDirField.GetValue(compiler)!;
        Directory.CreateDirectory(subDir);

        // Act & Assert – neither call should throw
        compiler.Dispose();
        compiler.Dispose();
    }

    /// <summary>
    /// Verifies that each Compiler instance gets a unique per-invocation subdirectory.
    /// </summary>
    [Fact]
    public void Constructor_CreatesUniqueSubDirectoryPerInstance()
    {
        // Arrange & Act
        var compiler1 = new Compiler(new InvocationCache());
        var compiler2 = new Compiler(new InvocationCache());

        var subDirField = typeof(Compiler).GetField("_subDirectory",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var subDir1 = (string)subDirField.GetValue(compiler1)!;
        var subDir2 = (string)subDirField.GetValue(compiler2)!;

        // Assert – subdirectories must be distinct
        Assert.NotEqual(subDir1, subDir2);

        // Cleanup
        compiler1.Dispose();
        compiler2.Dispose();
    }
}
