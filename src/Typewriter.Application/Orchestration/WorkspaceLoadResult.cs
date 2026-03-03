using Microsoft.CodeAnalysis;

namespace Typewriter.Application.Orchestration;

/// <summary>
/// Data transfer object that bridges the MSBuild workspace loading step and the metadata extraction step.
/// Each entry pairs a Roslyn <see cref="Project"/> with its emitted <see cref="Compilation"/>.
/// </summary>
/// <param name="Entries">
/// The loaded projects and their compilations, in topological traversal order.
/// </param>
public record WorkspaceLoadResult(
    IReadOnlyList<(Project Project, Compilation Compilation)> Entries
);
