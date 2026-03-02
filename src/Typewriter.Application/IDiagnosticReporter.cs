namespace Typewriter.Application;

/// <summary>
/// Emits parseable MSBuild-compatible diagnostics. Never throws; accumulates state.
/// </summary>
public interface IDiagnosticReporter
{
    void ReportError(string code, string message, string? file = null, int? line = null, int? column = null);
    void ReportWarning(string code, string message, string? file = null, int? line = null, int? column = null);
    void ReportInfo(string message);
    bool HasErrors { get; }
    bool HasWarnings { get; }
}
