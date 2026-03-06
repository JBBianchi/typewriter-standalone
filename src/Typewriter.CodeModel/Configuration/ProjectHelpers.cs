using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Typewriter.CodeModel.Configuration;

/// <summary>
/// Filesystem-based project-discovery helpers for CLI use.
/// </summary>
/// <remarks>
/// <para>
/// M1 scope: stubs sufficient for CLI operation without a loaded MSBuild workspace.
/// Methods that require MSBuild project-graph traversal (named project lookup,
/// current-project identity, reference resolution, solution enumeration) are
/// no-ops in M1 and documented for full implementation in M3.
/// </para>
/// <para>
/// Methods that can be answered from the filesystem alone
/// (<see cref="GetProjectItems"/> and <see cref="ProjectListContainsItem"/>)
/// are implemented without DTE or <c>ThreadHelper</c>.
/// </para>
/// </remarks>
internal static class ProjectHelpers
{
    private static readonly StringComparer PathComparer = OperatingSystem.IsWindows()
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;

    /// <summary>
    /// Attempts to find a project named <paramref name="projectName"/> within the
    /// solution at <paramref name="solutionPath"/> and adds its path to
    /// <paramref name="projectList"/>.
    /// </summary>
    internal static void AddProject(
        ICollection<string> projectList,
        string projectName,
        string solutionPath,
        ProjectInclusionContext? projectInclusionContext,
        Action<ProjectInclusionDiagnostic>? diagnosticReporter = null)
    {
        if (projectInclusionContext == null)
        {
            diagnosticReporter?.Invoke(new ProjectInclusionDiagnostic(
                "TW1201",
                $"Cannot resolve project selector '{projectName}' because no project catalog is available."));
            return;
        }

        var selector = NormalizePathSelector(projectName);
        var pathMatches = projectInclusionContext.Targets
            .Where(target => IsQualifiedPathMatch(target.ProjectPath, selector, solutionPath))
            .ToList();

        if (pathMatches.Count == 1)
        {
            AddProjectPath(projectList, pathMatches[0].ProjectPath);
            return;
        }

        var nameMatches = projectInclusionContext.Targets
            .Where(target => IsNameMatch(target, selector))
            .ToList();

        if (nameMatches.Count == 1)
        {
            AddProjectPath(projectList, nameMatches[0].ProjectPath);
            return;
        }

        if (nameMatches.Count > 1)
        {
            var candidates = string.Join(", ", nameMatches.Select(m => m.ProjectPath));
            diagnosticReporter?.Invoke(new ProjectInclusionDiagnostic(
                "TW1202",
                $"Project selector '{projectName}' is ambiguous. Use a path-qualified selector. Matches: {candidates}"));
            return;
        }

        diagnosticReporter?.Invoke(new ProjectInclusionDiagnostic(
            "TW1201",
            $"Cannot find project selector '{projectName}' in '{solutionPath}'."));
    }

    /// <summary>
    /// Adds the project that owns the template at <paramref name="currentProjectPath"/>
    /// to <paramref name="projectList"/>.
    /// </summary>
    internal static void AddCurrentProject(
        ICollection<string> projectList,
        string currentProjectPath,
        string templatePath,
        ProjectInclusionContext? projectInclusionContext,
        Action<ProjectInclusionDiagnostic>? diagnosticReporter = null)
    {
        if (!TryResolveCurrentProject(
                currentProjectPath,
                templatePath,
                projectInclusionContext,
                diagnosticReporter,
                out var project))
        {
            return;
        }

        AddProjectPath(projectList, project.ProjectPath);
    }

    /// <summary>
    /// Adds projects referenced by <paramref name="currentProjectPath"/> to
    /// <paramref name="projectList"/>.
    /// </summary>
    internal static void AddReferencedProjects(
        ICollection<string> projectList,
        string currentProjectPath,
        string templatePath,
        ProjectInclusionContext? projectInclusionContext,
        Action<ProjectInclusionDiagnostic>? diagnosticReporter = null)
    {
        if (!TryResolveCurrentProject(
                currentProjectPath,
                templatePath,
                projectInclusionContext,
                diagnosticReporter,
                out var project))
        {
            return;
        }

        foreach (var referencedProjectPath in project.ReferencedProjectPaths)
        {
            AddProjectPath(projectList, referencedProjectPath);
        }
    }

    /// <summary>
    /// Adds every project in the solution at <paramref name="solutionPath"/> to
    /// <paramref name="projectList"/>.
    /// </summary>
    internal static void AddAllProjects(
        string solutionPath,
        ICollection<string> projectList,
        ProjectInclusionContext? projectInclusionContext,
        Action<ProjectInclusionDiagnostic>? diagnosticReporter = null)
    {
        if (projectInclusionContext == null)
        {
            diagnosticReporter?.Invoke(new ProjectInclusionDiagnostic(
                "TW1201",
                $"Cannot enumerate projects for '{solutionPath}' because no project catalog is available."));
            return;
        }

        foreach (var target in projectInclusionContext.Targets)
        {
            AddProjectPath(projectList, target.ProjectPath);
        }
    }

    /// <summary>
    /// Enumerates source files matching <paramref name="filter"/> under the
    /// directories that contain the project files in <paramref name="projectList"/>.
    /// </summary>
    /// <remarks>
    /// M1: filesystem-based; does not validate VS project membership (no DTE).
    /// </remarks>
    internal static IEnumerable<string> GetProjectItems(
        ICollection<string> projectList,
        string filter)
    {
        return projectList
            .Select(p => new FileInfo(p).Directory)
            .Where(d => d != null && d.Exists)
            .SelectMany(d => d!.GetFiles(filter, SearchOption.AllDirectories))
            .Select(f => f.FullName);
    }

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="filename"/> falls under
    /// the directory of any project file in <paramref name="projectList"/>.
    /// </summary>
    /// <remarks>
    /// M1: filesystem path prefix check; does not consult the DTE solution.
    /// </remarks>
    internal static bool ProjectListContainsItem(
        string filename,
        ICollection<string> projectList)
    {
        return projectList
            .Select(p => new FileInfo(p).Directory)
            .Where(d => d != null)
            .Any(d => filename.StartsWith(d!.FullName, GetPathComparison()));
    }

    private static bool TryResolveCurrentProject(
        string solutionPath,
        string templatePath,
        ProjectInclusionContext? projectInclusionContext,
        Action<ProjectInclusionDiagnostic>? diagnosticReporter,
        out ProjectInclusionTarget project)
    {
        project = null!;

        if (projectInclusionContext == null)
        {
            diagnosticReporter?.Invoke(new ProjectInclusionDiagnostic(
                "TW1201",
                $"Cannot resolve the current project for template '{templatePath}' because no project catalog is available."));
            return false;
        }

        var entryPath = Path.GetFullPath(solutionPath);
        if (entryPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
        {
            var directMatch = projectInclusionContext.Targets
                .FirstOrDefault(target => PathComparer.Equals(Path.GetFullPath(target.ProjectPath), entryPath));
            if (directMatch != null)
            {
                project = directMatch;
                return true;
            }
        }

        var templateFullPath = Path.GetFullPath(templatePath);
        var containingProjects = projectInclusionContext.Targets
            .Select(target => new
            {
                Target = target,
                Directory = Path.GetDirectoryName(Path.GetFullPath(target.ProjectPath)) ?? string.Empty
            })
            .Where(x => IsPathUnderDirectory(templateFullPath, x.Directory))
            .OrderByDescending(x => x.Directory.Length)
            .ToList();

        if (containingProjects.Count == 1)
        {
            project = containingProjects[0].Target;
            return true;
        }

        if (containingProjects.Count > 1
            && containingProjects[0].Directory.Length > containingProjects[1].Directory.Length)
        {
            project = containingProjects[0].Target;
            return true;
        }

        if (containingProjects.Count > 1)
        {
            var candidates = string.Join(", ", containingProjects.Select(x => x.Target.ProjectPath));
            diagnosticReporter?.Invoke(new ProjectInclusionDiagnostic(
                "TW1202",
                $"Template '{templatePath}' maps to multiple projects. Move the template or use IncludeProject with a path-qualified selector. Matches: {candidates}"));
            return false;
        }

        diagnosticReporter?.Invoke(new ProjectInclusionDiagnostic(
            "TW1201",
            $"Cannot resolve the current project for template '{templatePath}'. The template is not under a loaded project directory."));
        return false;
    }

    private static void AddProjectPath(ICollection<string> projectList, string projectPath)
    {
        if (!projectList.Contains(projectPath, PathComparer))
        {
            projectList.Add(projectPath);
        }
    }

    private static bool IsQualifiedPathMatch(string projectPath, string selector, string solutionPath)
    {
        if (!LooksLikePathSelector(selector))
        {
            return false;
        }

        var normalizedProjectPath = Path.GetFullPath(projectPath);
        var normalizedSelector = NormalizePathSelector(selector);
        if (PathComparer.Equals(normalizedProjectPath, normalizedSelector))
        {
            return true;
        }

        var rootDirectory = GetResolutionRoot(solutionPath);
        var relativeProjectPath = NormalizePathSelector(Path.GetRelativePath(rootDirectory, normalizedProjectPath));
        return PathComparer.Equals(relativeProjectPath, normalizedSelector);
    }

    private static bool LooksLikePathSelector(string selector)
    {
        return selector.Contains(Path.DirectorySeparatorChar)
            || selector.Contains(Path.AltDirectorySeparatorChar)
            || selector.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
            || selector.EndsWith(".fsproj", StringComparison.OrdinalIgnoreCase)
            || selector.EndsWith(".vbproj", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetResolutionRoot(string solutionPath)
    {
        var fullPath = Path.GetFullPath(solutionPath);
        return Directory.Exists(fullPath)
            ? fullPath
            : Path.GetDirectoryName(fullPath) ?? Environment.CurrentDirectory;
    }

    private static string NormalizePathSelector(string path)
    {
        return path
            .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)
            .Trim();
    }

    private static bool IsNameMatch(ProjectInclusionTarget target, string selector)
    {
        if (string.Equals(target.ProjectName, selector, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (target.NameAliases.Count == 0)
        {
            return false;
        }

        return target.NameAliases.Any(alias => string.Equals(alias, selector, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsPathUnderDirectory(string path, string directory)
    {
        if (string.IsNullOrWhiteSpace(directory))
        {
            return false;
        }

        var normalizedDirectory = directory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        var normalizedPath = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        return normalizedPath.StartsWith(normalizedDirectory, GetPathComparison());
    }

    private static StringComparison GetPathComparison()
    {
        return OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
    }
}
