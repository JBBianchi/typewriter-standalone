namespace Typewriter.Generation.Output;

/// <summary>
/// Default output-path policy that mirrors the upstream Typewriter collision-avoidance convention.
/// The output file is placed alongside the template, using the source file's base name with the
/// template's extension replaced by <c>.ts</c>. When <paramref name="collisionIndex"/> is greater
/// than zero, a <c>_1</c>, <c>_2</c>, … suffix is inserted before the extension.
/// </summary>
public sealed class OutputPathPolicy : IOutputPathPolicy
{
    /// <inheritdoc />
    public string Resolve(string templatePath, string sourceCsPath, int collisionIndex = 0)
    {
        ArgumentException.ThrowIfNullOrEmpty(templatePath);
        ArgumentException.ThrowIfNullOrEmpty(sourceCsPath);

        var directory = Path.GetDirectoryName(templatePath) ?? string.Empty;
        var baseName = Path.GetFileNameWithoutExtension(sourceCsPath);
        const string extension = ".ts";

        if (collisionIndex > 0)
        {
            baseName = $"{baseName}_{collisionIndex}";
        }

        return Path.Combine(directory, baseName + extension);
    }
}
