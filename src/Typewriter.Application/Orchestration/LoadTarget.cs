namespace Typewriter.Application.Orchestration;

public record LoadTarget(
    string ProjectPath,
    string ProjectName,
    string TargetFramework,
    string? Configuration,
    string? RuntimeIdentifier,
    int TraversalOrder
);
