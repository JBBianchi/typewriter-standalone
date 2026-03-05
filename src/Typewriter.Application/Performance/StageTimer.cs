using System.Diagnostics;
using Typewriter.Application.Diagnostics;

namespace Typewriter.Application.Performance;

/// <summary>
/// A lightweight <see cref="Stopwatch"/>-based timer that captures named stage durations
/// and reports them via an <see cref="IDiagnosticReporter"/>.
/// </summary>
/// <remarks>
/// This class is not thread-safe. It assumes single-threaded pipeline execution.
/// </remarks>
public sealed class StageTimer
{
    private readonly Dictionary<string, TimeSpan> _stages = new();
    private readonly Stopwatch _stopwatch = new();
    private string? _currentStage;

    /// <summary>
    /// Starts (or restarts) timing for a named stage.
    /// If a previous stage is still running, it is stopped and recorded first.
    /// </summary>
    /// <param name="name">The name of the stage to start timing.</param>
    public void StartStage(string name)
    {
        StopStage();
        _currentStage = name;
        _stopwatch.Restart();
    }

    /// <summary>
    /// Stops the current stage timer and records the elapsed time.
    /// Does nothing if no stage is currently running.
    /// </summary>
    public void StopStage()
    {
        if (_currentStage is null)
            return;

        _stopwatch.Stop();
        _stages[_currentStage] = _stopwatch.Elapsed;
        _currentStage = null;
    }

    /// <summary>
    /// Emits one diagnostic line per recorded stage in insertion order.
    /// Each line has the format <c>{name}: {elapsed}ms</c>.
    /// </summary>
    /// <param name="reporter">The diagnostic reporter to emit stage timings to.</param>
    /// <param name="severity">The severity level for the emitted diagnostics.</param>
    public void Report(IDiagnosticReporter reporter, DiagnosticSeverity severity)
    {
        foreach (var (name, elapsed) in _stages)
        {
            reporter.Report(new DiagnosticMessage(
                severity,
                "TW0000",
                $"{name}: {elapsed.TotalMilliseconds:F0}ms"));
        }
    }
}
