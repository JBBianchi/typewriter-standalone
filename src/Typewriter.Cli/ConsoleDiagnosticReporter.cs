using Typewriter.Application;

namespace Typewriter.Cli;

internal sealed class ConsoleDiagnosticReporter : IDiagnosticReporter
{
    private bool _hasErrors;
    private bool _hasWarnings;

    public bool HasErrors => _hasErrors;
    public bool HasWarnings => _hasWarnings;

    public void ReportError(string code, string message, string? file = null, int? line = null, int? column = null)
    {
        _hasErrors = true;
        Console.Error.WriteLine(FormatDiagnostic("error", code, message, file, line, column));
    }

    public void ReportWarning(string code, string message, string? file = null, int? line = null, int? column = null)
    {
        _hasWarnings = true;
        Console.Error.WriteLine(FormatDiagnostic("warning", code, message, file, line, column));
    }

    public void ReportInfo(string message) => Console.WriteLine(message);

    private static string FormatDiagnostic(
        string severity, string code, string message,
        string? file, int? line, int? column)
    {
        var location = file is null ? string.Empty
            : line is null ? $"{file}: "
            : column is null ? $"{file}({line}): "
            : $"{file}({line},{column}): ";

        return $"{location}{severity} {code}: {message}";
    }
}
