using Typewriter.Application.Configuration;

namespace Typewriter.Application;

/// <summary>Fully-resolved, immutable options for a single <c>generate</c> command invocation.</summary>
/// <remarks>
/// Construct via <see cref="Merge"/> so that CLI args, config file, and defaults are applied
/// in the correct precedence order: CLI args &gt; config file &gt; defaults.
/// </remarks>
public record GenerateCommandOptions(
    IReadOnlyList<string> Templates,
    string? Solution,
    string? Project,
    string? Framework,
    string? Configuration,
    string? Runtime,
    bool Restore,
    string? Output,
    string Verbosity,
    bool FailOnWarnings)
{
    /// <summary>
    /// Merges CLI arguments, an optional config file, and defaults into a
    /// <see cref="GenerateCommandOptions"/> instance.
    /// </summary>
    /// <remarks>Precedence: CLI args &gt; config file &gt; defaults.</remarks>
    public static GenerateCommandOptions Merge(
        TypewriterConfig? config,
        IReadOnlyList<string> templates,
        string? solution,
        string? project,
        string? framework,
        string? configuration,
        string? runtime,
        bool restore,
        string? output,
        string? verbosity,
        bool failOnWarnings)
    {
        return new GenerateCommandOptions(
            Templates:      templates,
            Solution:       solution       ?? config?.Solution,
            Project:        project        ?? config?.Project,
            Framework:      framework      ?? config?.Framework,
            Configuration:  configuration  ?? config?.Configuration,
            Runtime:        runtime        ?? config?.Runtime,
            Restore:        restore,
            Output:         output         ?? config?.Output,
            Verbosity:      verbosity      ?? config?.Verbosity ?? "normal",
            FailOnWarnings: failOnWarnings || (config?.FailOnWarnings ?? false));
    }
}
