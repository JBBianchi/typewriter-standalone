namespace Typewriter.VisualStudio;

/// <summary>
/// No-op logger used by the standalone CLI to preserve template API compatibility.
/// </summary>
public sealed class Log : ILog
{
    /// <summary>
    /// Gets a singleton no-op logger instance.
    /// </summary>
    public static ILog Instance { get; } = new Log();

    /// <summary>
    /// Compatibility method matching upstream static API.
    /// </summary>
    public static void Debug(string message, params object[] parameters)
    {
    }

    /// <summary>
    /// Compatibility method matching upstream static API.
    /// </summary>
    public static void Info(string message, params object[] parameters)
    {
    }

    /// <summary>
    /// Compatibility method matching upstream static API.
    /// </summary>
    public static void Warn(string message, params object[] parameters)
    {
    }

    /// <summary>
    /// Compatibility method matching upstream static API.
    /// </summary>
    public static void Error(string message, params object[] parameters)
    {
    }

    /// <inheritdoc />
    public void LogDebug(string message, params object[] parameters)
    {
    }

    /// <inheritdoc />
    public void LogInfo(string message, params object[] parameters)
    {
    }

    /// <inheritdoc />
    public void LogWarning(string message, params object[] parameters)
    {
    }

    /// <inheritdoc />
    public void LogError(string message, params object[] parameters)
    {
    }
}
