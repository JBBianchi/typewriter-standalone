namespace Typewriter.Application;

/// <summary>Options parsed from the <c>generate</c> CLI subcommand.</summary>
public sealed class GenerateCommandOptions
{
    public required IReadOnlyList<string> Templates { get; init; }
    public string? Solution { get; init; }
    public string? Project { get; init; }
    public string? Framework { get; init; }
    public string? Configuration { get; init; }
    public string? Runtime { get; init; }
    public bool Restore { get; init; }
    public string? Output { get; init; }
    public string? Verbosity { get; init; }
    public bool FailOnWarnings { get; init; }
}
