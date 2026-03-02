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
    /// <summary>
    /// Attempts to find a project named <paramref name="projectName"/> within the
    /// solution at <paramref name="solutionPath"/> and adds its path to
    /// <paramref name="projectList"/>.
    /// </summary>
    /// <remarks>
    /// M1 stub — named-project lookup requires MSBuild graph traversal (deferred to M3).
    /// </remarks>
    internal static void AddProject(
        ICollection<string> projectList,
        string projectName,
        string solutionPath)
    {
        // M1: MSBuild project-graph lookup deferred to M3.
        _ = projectList;
        _ = projectName;
        _ = solutionPath;
    }

    /// <summary>
    /// Adds the project that owns the template at <paramref name="currentProjectPath"/>
    /// to <paramref name="projectList"/>.
    /// </summary>
    /// <remarks>
    /// M1 stub — project identity requires MSBuild loading (deferred to M3).
    /// </remarks>
    internal static void AddCurrentProject(
        ICollection<string> projectList,
        string currentProjectPath)
    {
        // M1: MSBuild project identity deferred to M3.
        _ = projectList;
        _ = currentProjectPath;
    }

    /// <summary>
    /// Adds projects referenced by <paramref name="currentProjectPath"/> to
    /// <paramref name="projectList"/>.
    /// </summary>
    /// <remarks>
    /// M1 stub — reference-graph traversal requires MSBuild loading (deferred to M3).
    /// </remarks>
    internal static void AddReferencedProjects(
        ICollection<string> projectList,
        string currentProjectPath)
    {
        // M1: reference graph traversal deferred to M3.
        _ = projectList;
        _ = currentProjectPath;
    }

    /// <summary>
    /// Adds every project in the solution at <paramref name="solutionPath"/> to
    /// <paramref name="projectList"/>.
    /// </summary>
    /// <remarks>
    /// M1 stub — solution-level project enumeration requires MSBuild loading (deferred to M3).
    /// </remarks>
    internal static void AddAllProjects(
        string solutionPath,
        ICollection<string> projectList)
    {
        // M1: solution-level enumeration deferred to M3.
        _ = solutionPath;
        _ = projectList;
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
            .Any(d => filename.StartsWith(d!.FullName, StringComparison.OrdinalIgnoreCase));
    }
}
