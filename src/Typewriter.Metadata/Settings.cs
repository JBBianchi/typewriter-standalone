using System;
using Typewriter.Configuration;

namespace Typewriter.Metadata;

/// <summary>
/// Provides settings for Typewriter Templates.
/// </summary>
/// <remarks>
/// This is the CLI-portable abstract base class. Members that would require
/// cross-project type references (<c>Typewriter.CodeModel.File</c>) or
/// VS-host types (<c>EnvDTE</c>, <c>ILog</c>) are intentionally omitted here
/// to avoid circular project dependencies. Concrete CLI implementations live
/// in <c>Typewriter.CodeModel.Configuration.SettingsImpl</c> and add those members.
/// </remarks>
public abstract class Settings
{
    /// <summary>
    /// Gets or sets a value indicating how partial classes and interfaces are rendered.
    /// </summary>
    public PartialRenderingMode PartialRenderingMode { get; set; } = PartialRenderingMode.Partial;

    /// <summary>
    /// Gets or sets the file extension for output files.
    /// </summary>
    public string OutputExtension { get; set; } = ".ts";

    /// <summary>
    /// Gets or sets the output directory to which generated files are saved.
    /// </summary>
    public string? OutputDirectory { get; set; }

    /// <summary>
    /// Gets or sets a filename factory used to compute output file names.
    /// Kept on the shared metadata contract for legacy template compatibility.
    /// </summary>
    public virtual Func<dynamic, string>? OutputFilenameFactory { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether generated files should not be added to the project.
    /// </summary>
    public bool SkipAddingGeneratedFilesToProject { get; set; }

    /// <summary>
    /// Gets the full path of the solution or project file.
    /// </summary>
    public abstract string SolutionFullName { get; }

    /// <summary>
    /// Gets a value indicating whether this instance is in single-file mode.
    /// </summary>
    public abstract bool IsSingleFileMode { get; }

    /// <summary>
    /// Gets the name of the single output file when <see cref="IsSingleFileMode"/> is <see langword="true"/>.
    /// </summary>
    public abstract string? SingleFileName { get; }

    /// <summary>
    /// Gets the string literal character used in generated TypeScript. Default is <c>"</c>.
    /// </summary>
    public abstract char StringLiteralCharacter { get; }

    /// <summary>
    /// Gets a value indicating whether strict null generation is enabled.
    /// When <see langword="true"/>, nullable C# types produce <c>type | null</c> in TypeScript.
    /// </summary>
    public abstract bool StrictNullGeneration { get; }

    /// <summary>
    /// Gets a value indicating whether UTF-8 BOM is written to generated output files.
    /// </summary>
    public abstract bool Utf8BomGeneration { get; }

    /// <summary>
    /// Gets the full path to the template file.
    /// </summary>
    public abstract string TemplatePath { get; }

    /// <summary>
    /// Includes files from the project with the given name when rendering the template.
    /// </summary>
    public abstract Settings IncludeProject(string projectName);

    /// <summary>
    /// Switches the template to single-file mode, directing all output to
    /// <paramref name="singleFilename"/>.
    /// </summary>
    public abstract Settings SingleFileMode(string singleFilename);

    /// <summary>
    /// Includes files from the project that contains the current template file.
    /// </summary>
    public abstract Settings IncludeCurrentProject();

    /// <summary>
    /// Includes files from all projects referenced by the current project.
    /// </summary>
    public abstract Settings IncludeReferencedProjects();

    /// <summary>
    /// Includes files from every project in the solution.
    /// Note: may have a significant performance impact on large solutions.
    /// </summary>
    public abstract Settings IncludeAllProjects();

    /// <summary>
    /// Sets the string literal delimiter character used in generated TypeScript.
    /// </summary>
    public abstract Settings UseStringLiteralCharacter(char ch);

    /// <summary>
    /// Disables strict null generation; nullable types produce plain <c>type</c>
    /// instead of <c>type | null</c>.
    /// </summary>
    public abstract Settings DisableStrictNullGeneration();

    /// <summary>
    /// Disables UTF-8 BOM generation in generated output files.
    /// </summary>
    public abstract Settings DisableUtf8BomGeneration();
}
