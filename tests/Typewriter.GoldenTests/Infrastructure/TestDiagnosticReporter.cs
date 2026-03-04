using System.Collections.Concurrent;
using Typewriter.Application.Diagnostics;

namespace Typewriter.GoldenTests.Infrastructure;

/// <summary>
/// A diagnostic reporter for golden tests that collects all messages
/// and tracks warning/error counts.
/// </summary>
public sealed class TestDiagnosticReporter : IDiagnosticReporter
{
    private readonly ConcurrentBag<DiagnosticMessage> _messages = [];
    private int _warningCount;
    private int _errorCount;

    /// <inheritdoc />
    public void Report(DiagnosticMessage message)
    {
        _messages.Add(message);
        if (message.Severity == DiagnosticSeverity.Warning)
            Interlocked.Increment(ref _warningCount);
        else if (message.Severity == DiagnosticSeverity.Error)
            Interlocked.Increment(ref _errorCount);
    }

    /// <inheritdoc />
    public int WarningCount => _warningCount;

    /// <inheritdoc />
    public int ErrorCount => _errorCount;

    /// <summary>
    /// Gets all collected diagnostic messages.
    /// </summary>
    public IReadOnlyList<DiagnosticMessage> Messages => [.. _messages];
}
