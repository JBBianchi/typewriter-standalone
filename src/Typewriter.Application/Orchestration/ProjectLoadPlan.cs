namespace Typewriter.Application.Orchestration;

public record ProjectLoadPlan(
    string EntryPath,
    string? SolutionDirectory,
    IReadOnlyList<LoadTarget> Targets,
    IReadOnlyDictionary<string, string> GlobalProperties
);
