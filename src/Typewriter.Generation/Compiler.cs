namespace Typewriter.Generation;

/// <summary>
/// Compiles template code from a shadow class into a loadable assembly type.
/// This is a compilation stub; the full implementation with <c>AssemblyLoadContext</c>
/// and diagnostic reporting will be provided when Compiler.cs is ported.
/// </summary>
internal static class Compiler
{
    /// <summary>
    /// Compiles the shadow class and returns the generated template type.
    /// </summary>
    /// <param name="templateFilePath">Absolute path to the .tst template file.</param>
    /// <param name="shadowClass">The shadow class containing parsed template code.</param>
    /// <returns>The compiled template <see cref="Type"/>.</returns>
    public static Type Compile(string templateFilePath, ShadowClass shadowClass)
    {
        throw new NotImplementedException(
            "Compiler.Compile is not yet implemented. Port Compiler.cs to complete template compilation.");
    }
}
