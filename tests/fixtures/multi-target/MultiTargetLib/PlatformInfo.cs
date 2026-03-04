namespace MultiTargetLib;

/// <summary>
/// Demonstrates conditional compilation across target frameworks.
/// The <see cref="FrameworkName"/> property returns a different value depending on
/// whether the project is compiled for <c>net10.0</c> or <c>net8.0</c>.
/// </summary>
public class PlatformInfo
{
    /// <summary>
    /// Gets the human-readable name of the target framework used at compile time.
    /// </summary>
#if NET8_0
    public string FrameworkName => "net8.0";
#else
    public string FrameworkName => "net10.0";
#endif

    /// <summary>
    /// Gets the major version of the runtime target.
    /// </summary>
#if NET8_0
    public int MajorVersion => 8;
#else
    public int MajorVersion => 10;
#endif

    /// <summary>
    /// Gets a value indicating whether the build targets .NET 8.
    /// </summary>
#if NET8_0
    public bool IsLegacyTarget => true;
#else
    public bool IsLegacyTarget => false;
#endif

    /// <summary>
    /// Gets the shared description that is identical across all TFMs.
    /// </summary>
    public string Description => "Multi-target fixture library";
}
