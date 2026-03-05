using Typewriter.Generation;
using Xunit;

namespace Typewriter.UnitTests.Generation;

/// <summary>
/// Tests for <see cref="Compiler.CleanupStaleDirectories"/> which removes stale
/// temp subdirectories left behind by crashed or interrupted invocations.
/// </summary>
public class CompilerStaleCleanupTests : IDisposable
{
    private readonly string _testRoot;

    public CompilerStaleCleanupTests()
    {
        _testRoot = Path.Combine(Path.GetTempPath(), $"TypewriterTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testRoot);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }
    }

    /// <summary>
    /// Verifies that subdirectories older than the threshold are deleted.
    /// </summary>
    [Fact]
    public void CleanupStaleDirectories_DeletesOldSubdirectories()
    {
        var staleDir = Path.Combine(_testRoot, "stale");
        Directory.CreateDirectory(staleDir);
        Directory.SetLastWriteTimeUtc(staleDir, DateTime.UtcNow.AddHours(-48));

        Compiler.CleanupStaleDirectories(_testRoot, TimeSpan.FromHours(24));

        Assert.False(Directory.Exists(staleDir));
    }

    /// <summary>
    /// Verifies that subdirectories within the threshold are not deleted.
    /// </summary>
    [Fact]
    public void CleanupStaleDirectories_PreservesRecentSubdirectories()
    {
        var recentDir = Path.Combine(_testRoot, "recent");
        Directory.CreateDirectory(recentDir);
        // Last write time defaults to now, well within 24-hour threshold.

        Compiler.CleanupStaleDirectories(_testRoot, TimeSpan.FromHours(24));

        Assert.True(Directory.Exists(recentDir));
    }

    /// <summary>
    /// Verifies that a mix of stale and recent directories results in only stale ones being removed.
    /// </summary>
    [Fact]
    public void CleanupStaleDirectories_DeletesOnlyStaleDirectories()
    {
        var staleDir = Path.Combine(_testRoot, "old");
        var recentDir = Path.Combine(_testRoot, "new");
        Directory.CreateDirectory(staleDir);
        Directory.CreateDirectory(recentDir);
        Directory.SetLastWriteTimeUtc(staleDir, DateTime.UtcNow.AddHours(-48));

        Compiler.CleanupStaleDirectories(_testRoot, TimeSpan.FromHours(24));

        Assert.False(Directory.Exists(staleDir));
        Assert.True(Directory.Exists(recentDir));
    }

    /// <summary>
    /// Verifies that cleanup does not throw when the parent directory does not exist.
    /// </summary>
    [Fact]
    public void CleanupStaleDirectories_NoExceptionWhenParentMissing()
    {
        var nonExistent = Path.Combine(_testRoot, "does_not_exist");

        var exception = Record.Exception(() =>
            Compiler.CleanupStaleDirectories(nonExistent, TimeSpan.FromHours(24)));

        Assert.Null(exception);
    }

    /// <summary>
    /// Verifies that cleanup does not throw when the parent directory is empty.
    /// </summary>
    [Fact]
    public void CleanupStaleDirectories_NoExceptionWhenEmpty()
    {
        var exception = Record.Exception(() =>
            Compiler.CleanupStaleDirectories(_testRoot, TimeSpan.FromHours(24)));

        Assert.Null(exception);
    }

    /// <summary>
    /// Verifies that cleanup tolerates a stale directory containing a locked file without throwing,
    /// and that other stale directories are still cleaned up.
    /// </summary>
    [Fact]
    public void CleanupStaleDirectories_ToleratesLockedFiles()
    {
        // Arrange – create a stale directory with a locked file
        var lockedDir = Path.Combine(_testRoot, "locked");
        Directory.CreateDirectory(lockedDir);
        var lockedFilePath = Path.Combine(lockedDir, "locked.dll");
        // Open file with exclusive lock to prevent deletion
        using var lockedStream = new FileStream(
            lockedFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
        Directory.SetLastWriteTimeUtc(lockedDir, DateTime.UtcNow.AddHours(-48));

        // Create another stale directory that can be deleted
        var deletableDir = Path.Combine(_testRoot, "deletable");
        Directory.CreateDirectory(deletableDir);
        Directory.SetLastWriteTimeUtc(deletableDir, DateTime.UtcNow.AddHours(-48));

        // Act – should not throw despite locked file
        var exception = Record.Exception(() =>
            Compiler.CleanupStaleDirectories(_testRoot, TimeSpan.FromHours(24)));

        // Assert
        Assert.Null(exception);
        // The unlocked stale directory should be deleted
        Assert.False(Directory.Exists(deletableDir));
        // The locked directory may still exist (best-effort)
        // No assertion on lockedDir existence — the point is no exception was thrown
    }
}
