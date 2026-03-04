using System.Text;

namespace Typewriter.Generation.Output;

/// <summary>
/// Default output writer that writes generated content to disk with optional UTF-8 BOM.
/// Skips the write when the target file already exists and its content matches exactly,
/// preserving file timestamps and avoiding unnecessary rebuilds.
/// EOL characters are written as-is — no normalisation is performed.
/// </summary>
public sealed class OutputWriter : IOutputWriter
{
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private static readonly Encoding Utf8WithBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);

    /// <inheritdoc />
    public async Task WriteAsync(string filePath, string content, bool addBom, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrEmpty(filePath);
        ArgumentNullException.ThrowIfNull(content);

        // Skip write when existing content matches exactly.
        if (File.Exists(filePath))
        {
            var existing = await File.ReadAllTextAsync(filePath, Encoding.UTF8, ct);
            if (string.Equals(existing, content, StringComparison.Ordinal))
            {
                return;
            }
        }

        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var encoding = addBom ? Utf8WithBom : Utf8NoBom;
        await File.WriteAllTextAsync(filePath, content, encoding, ct);
    }
}
