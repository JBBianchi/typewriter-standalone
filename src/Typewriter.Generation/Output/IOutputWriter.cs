namespace Typewriter.Generation.Output;

/// <summary>
/// Writes generated output to the filesystem, optionally prepending a UTF-8 BOM and
/// skipping writes when the existing file content already matches.
/// </summary>
public interface IOutputWriter
{
    /// <summary>
    /// Writes <paramref name="content"/> to <paramref name="filePath"/>, creating
    /// intermediate directories as needed.
    /// </summary>
    /// <param name="filePath">Absolute path of the output file.</param>
    /// <param name="content">The generated text content to write.</param>
    /// <param name="addBom">When <c>true</c>, writes a UTF-8 byte-order mark prefix.</param>
    /// <param name="ct">Token used to cancel the operation.</param>
    Task WriteAsync(string filePath, string content, bool addBom, CancellationToken ct);
}
