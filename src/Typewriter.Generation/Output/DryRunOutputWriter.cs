using System.Collections.Concurrent;

namespace Typewriter.Generation.Output;

/// <summary>
/// Decorator over <see cref="IOutputWriter"/> that suppresses all file-system writes and
/// instead records each file path that would have been written. An optional callback is
/// invoked per write so the caller can emit a diagnostic (e.g. TW5001).
/// </summary>
public sealed class DryRunOutputWriter : IOutputWriter
{
    private readonly IOutputWriter _inner;
    private readonly Action<string>? _onFileRecorded;
    private readonly ConcurrentBag<string> _recordedPaths = [];

    /// <summary>
    /// Initializes a new <see cref="DryRunOutputWriter"/> that wraps <paramref name="inner"/>.
    /// </summary>
    /// <param name="inner">The real writer being decorated; retained for composition but never invoked.</param>
    /// <param name="onFileRecorded">
    /// Optional callback invoked with the file path each time <see cref="WriteAsync"/> is called.
    /// Typically used to emit a TW5001 info diagnostic.
    /// </param>
    public DryRunOutputWriter(IOutputWriter inner, Action<string>? onFileRecorded = null)
    {
        ArgumentNullException.ThrowIfNull(inner);
        _inner = inner;
        _onFileRecorded = onFileRecorded;
    }

    /// <summary>
    /// Gets the file paths that would have been written, in the order they were recorded.
    /// </summary>
    public IReadOnlyCollection<string> RecordedPaths => _recordedPaths;

    /// <summary>
    /// Gets the total number of files that would have been written.
    /// </summary>
    public int FileCount => _recordedPaths.Count;

    /// <inheritdoc />
    /// <remarks>
    /// Records <paramref name="filePath"/> without performing any file I/O and invokes the
    /// <c>onFileRecorded</c> callback when one was provided.
    /// </remarks>
    public Task WriteAsync(string filePath, string content, bool addBom, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrEmpty(filePath);
        ArgumentNullException.ThrowIfNull(content);

        _recordedPaths.Add(filePath);
        _onFileRecorded?.Invoke(filePath);

        return Task.CompletedTask;
    }
}
