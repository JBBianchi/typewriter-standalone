namespace Typewriter.Generation.Output;

/// <summary>
/// Resolves the output file path for a generated file, handling collision avoidance
/// when multiple source files produce identically-named outputs.
/// </summary>
public interface IOutputPathPolicy
{
    /// <summary>
    /// Computes the output file path for a given template/source pair.
    /// </summary>
    /// <param name="templatePath">Absolute path to the <c>.tst</c> template file.</param>
    /// <param name="sourceCsPath">Absolute path to the source <c>.cs</c> file being rendered.</param>
    /// <param name="collisionIndex">
    /// Zero-based collision counter. When <c>0</c> the base name is used as-is;
    /// when greater than zero a <c>_1</c>, <c>_2</c>, … suffix is appended before the extension
    /// to match upstream collision semantics.
    /// </param>
    /// <returns>The resolved absolute output file path.</returns>
    string Resolve(string templatePath, string sourceCsPath, int collisionIndex = 0);
}
