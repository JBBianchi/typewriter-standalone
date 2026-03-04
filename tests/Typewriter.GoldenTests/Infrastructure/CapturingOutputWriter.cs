using System.Collections.Concurrent;
using Typewriter.Generation.Output;

namespace Typewriter.GoldenTests.Infrastructure;

/// <summary>
/// An <see cref="IOutputWriter"/> that captures generated output in memory
/// instead of writing to disk, enabling golden test comparison.
/// </summary>
internal sealed class CapturingOutputWriter : IOutputWriter
{
    private readonly ConcurrentDictionary<string, string> _outputs = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the captured outputs keyed by file path.
    /// </summary>
    public IReadOnlyDictionary<string, string> Outputs => _outputs;

    /// <inheritdoc />
    public Task WriteAsync(string filePath, string content, bool addBom, CancellationToken ct)
    {
        _outputs[filePath] = content;
        return Task.CompletedTask;
    }
}
