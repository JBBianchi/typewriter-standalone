using System;
using System.Collections.Generic;
using Typewriter.Configuration;
using Typewriter.VisualStudio;

namespace Typewriter.CodeModel.Configuration;

/// <summary>
/// CLI-portable implementation of <see cref="Settings"/>.
/// Accepts configuration from the CLI context (paths, flags) with no DTE,
/// no registry, and no <c>ThreadHelper</c> references.
/// All values are path-based and effectively immutable after construction;
/// the fluent mutator methods update internal state and return <c>this</c>.
/// </summary>
public class SettingsImpl : Settings
{
    private readonly string _solutionFullName;
    private readonly ProjectInclusionContext? _projectInclusionContext;
    private readonly Action<ProjectInclusionDiagnostic>? _projectInclusionDiagnosticReporter;
    private bool _isSingleFileMode;
    private string? _singleFileName;
    private char _stringLiteralCharacter = '"';
    private List<string>? _includedProjects;
    private bool _hasExplicitProjectSelection;
    private bool _strictNullGeneration = true;
    private bool _utf8BomGeneration = true;
    private readonly string _templatePath;
    private static readonly ILog NoOpLog = Typewriter.VisualStudio.Log.Instance;

    /// <summary>
    /// Initializes a new instance of <see cref="SettingsImpl"/>.
    /// </summary>
    /// <param name="templatePath">Absolute path to the template file.</param>
    /// <param name="solutionFullName">
    ///   Absolute path to the solution or project file used for project-relative
    ///   resolution. Pass an empty string when no solution context is available.
    /// </param>
    /// <param name="includedProjectPaths">
    ///   Optional pre-populated list of project file paths to include.
    ///   When <see langword="null"/>, the list is lazily populated on first access
    ///   via <see cref="IncludeCurrentProject"/> and <see cref="IncludeReferencedProjects"/>
    ///   (mirrors upstream behaviour).
    /// </param>
    /// <param name="projectInclusionContext">
    ///   Optional loaded-project catalog used to resolve <c>Include*</c> settings in CLI mode.
    /// </param>
    /// <param name="projectInclusionDiagnosticReporter">
    ///   Optional callback for deterministic project-selection diagnostics.
    /// </param>
    public SettingsImpl(
        string templatePath,
        string solutionFullName = "",
        IEnumerable<string>? includedProjectPaths = null,
        ProjectInclusionContext? projectInclusionContext = null,
        Action<ProjectInclusionDiagnostic>? projectInclusionDiagnosticReporter = null)
    {
        _templatePath = templatePath;
        _solutionFullName = solutionFullName;
        _projectInclusionContext = projectInclusionContext;
        _projectInclusionDiagnosticReporter = projectInclusionDiagnosticReporter;

        if (includedProjectPaths != null)
        {
            _includedProjects = new List<string>(includedProjectPaths);
            _hasExplicitProjectSelection = true;
        }
    }

    // -------------------------------------------------------------------------
    // Concrete members not present on the abstract base (require CodeModel types
    // or would cause circular project-reference dependencies if placed there).
    // -------------------------------------------------------------------------

    /// <inheritdoc/>
    public override ILog Log => NoOpLog;

    /// <summary>
    /// Gets the resolved set of project-file paths to include when rendering this template.
    /// Populated lazily by <see cref="IncludeCurrentProject"/> and
    /// <see cref="IncludeReferencedProjects"/> on first access (mirrors upstream behaviour).
    /// </summary>
    public ICollection<string> IncludedProjects
    {
        get
        {
            if (_includedProjects == null)
            {
                IncludeCurrentProject();
                IncludeReferencedProjects();
            }

            return _includedProjects!;
        }
    }

    /// <summary>
    /// Gets a value indicating whether explicit project-selection APIs were invoked for this template.
    /// When <see langword="false"/>, the CLI preserves the current whole-workspace default.
    /// </summary>
    public bool HasExplicitProjectSelection => _hasExplicitProjectSelection;

    // -------------------------------------------------------------------------
    // Abstract Settings implementation
    // -------------------------------------------------------------------------

    /// <inheritdoc/>
    public override string SolutionFullName => _solutionFullName;

    /// <inheritdoc/>
    public override bool IsSingleFileMode => _isSingleFileMode;

    /// <inheritdoc/>
    public override string? SingleFileName => _singleFileName;

    /// <inheritdoc/>
    public override char StringLiteralCharacter => _stringLiteralCharacter;

    /// <inheritdoc/>
    public override bool StrictNullGeneration => _strictNullGeneration;

    /// <inheritdoc/>
    public override bool Utf8BomGeneration => _utf8BomGeneration;

    /// <inheritdoc/>
    public override string TemplatePath => _templatePath;

    /// <inheritdoc/>
    public override Settings IncludeProject(string projectName)
    {
        _hasExplicitProjectSelection = true;
        _includedProjects ??= new List<string>();
        ProjectHelpers.AddProject(
            _includedProjects,
            projectName,
            _solutionFullName,
            _projectInclusionContext,
            _projectInclusionDiagnosticReporter);
        return this;
    }

    /// <inheritdoc/>
    public override Settings SingleFileMode(string singleFilename)
    {
        _isSingleFileMode = true;
        _singleFileName = singleFilename;
        return this;
    }

    /// <inheritdoc/>
    public override Settings IncludeReferencedProjects()
    {
        _hasExplicitProjectSelection = true;
        _includedProjects ??= new List<string>();
        ProjectHelpers.AddReferencedProjects(
            _includedProjects,
            _solutionFullName,
            _templatePath,
            _projectInclusionContext,
            _projectInclusionDiagnosticReporter);
        return this;
    }

    /// <inheritdoc/>
    public override Settings IncludeCurrentProject()
    {
        _hasExplicitProjectSelection = true;
        _includedProjects ??= new List<string>();
        ProjectHelpers.AddCurrentProject(
            _includedProjects,
            _solutionFullName,
            _templatePath,
            _projectInclusionContext,
            _projectInclusionDiagnosticReporter);
        return this;
    }

    /// <inheritdoc/>
    public override Settings IncludeAllProjects()
    {
        _hasExplicitProjectSelection = true;
        _includedProjects ??= new List<string>();
        ProjectHelpers.AddAllProjects(
            _solutionFullName,
            _includedProjects,
            _projectInclusionContext,
            _projectInclusionDiagnosticReporter);
        return this;
    }

    /// <inheritdoc/>
    public override Settings UseStringLiteralCharacter(char ch)
    {
        _stringLiteralCharacter = ch;
        return this;
    }

    /// <inheritdoc/>
    public override Settings DisableStrictNullGeneration()
    {
        _strictNullGeneration = false;
        return this;
    }

    /// <inheritdoc/>
    public override Settings DisableUtf8BomGeneration()
    {
        _utf8BomGeneration = false;
        return this;
    }
}
