namespace Typewriter.VisualStudio;

/// <summary>
/// Minimal logging contract preserved for template compatibility.
/// </summary>
public interface ILog
{
    void LogDebug(string message, params object[] parameters);

    void LogInfo(string message, params object[] parameters);

    void LogWarning(string message, params object[] parameters);

    void LogError(string message, params object[] parameters);
}
