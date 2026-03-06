namespace Typewriter.CodeModel.Configuration;

/// <summary>
/// Plain project catalog used by template settings to resolve project-scoping calls
/// without taking a Roslyn or MSBuild dependency in the code-model layer.
/// </summary>
public sealed record ProjectInclusionContext(
    string EntryPath,
    IReadOnlyList<ProjectInclusionTarget> Targets);

/// <summary>
/// Project identity and reference information available to template inclusion settings.
/// </summary>
public sealed record ProjectInclusionTarget(
    string ProjectPath,
    string ProjectName,
    IReadOnlyList<string> ReferencedProjectPaths)
{
    /// <summary>
    /// Optional name selectors that should resolve to this project for backwards compatibility.
    /// Examples: assembly name and project file stem.
    /// </summary>
    public IReadOnlyList<string> NameAliases { get; init; } = [];
}

/// <summary>
/// Deterministic diagnostic emitted while resolving template project-selection settings.
/// </summary>
public sealed record ProjectInclusionDiagnostic(
    string Code,
    string Message);
