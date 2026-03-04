namespace Typewriter.Application.Diagnostics;

public static class DiagnosticCode
{
    /// <summary>Invalid template pattern.</summary>
    public const string TW1001 = "TW1001";

    /// <summary>Missing solution or project.</summary>
    public const string TW1002 = "TW1002";

    /// <summary>Restore failed.</summary>
    public const string TW2001 = "TW2001";

    /// <summary>Project file not found at the specified path.</summary>
    public const string TW2002 = "TW2002";

    /// <summary>Restore assets missing (obj/project.assets.json not present); run with --restore.</summary>
    public const string TW2003 = "TW2003";

    /// <summary>(Error) MSBuild ProjectGraph constructor failed to load a .sln or .slnx solution file.</summary>
    public const string TW2110 = "TW2110";

    /// <summary>(Error) General workspace load failure: MSBuildWorkspace emitted a diagnostic with error severity during workspace creation or project open.</summary>
    public const string TW2200 = "TW2200";

    /// <summary>(Warning) Non-fatal workspace diagnostic emitted during project open; generation may continue but output could be incomplete.</summary>
    public const string TW2201 = "TW2201";

    /// <summary>(Error) Compilation failure: the Roslyn compilation for a project was null or contained error-severity diagnostics that prevent semantic extraction.</summary>
    public const string TW2202 = "TW2202";

    /// <summary>(Error) Project not found in workspace after OpenProjectAsync completed; the workspace did not contain the expected project entry.</summary>
    public const string TW2203 = "TW2203";

    /// <summary>(Error) Workspace project reference could not be resolved; one or more project-to-project references are missing from the loaded workspace graph.</summary>
    public const string TW2204 = "TW2204";

    /// <summary>(Warning) Workspace loaded with partial documents; one or more source files were excluded or failed to open, which may affect metadata completeness.</summary>
    public const string TW2205 = "TW2205";

    /// <summary>(Warning) SolutionFallbackService could not list projects from a .slnx file via dotnet sln list.</summary>
    public const string TW2310 = "TW2310";

    /// <summary>Multi-target default selection: TFM was chosen implicitly because --framework was not specified.</summary>
    public const string TW2401 = "TW2401";

    /// <summary>Template compile error.</summary>
    public const string TW3001 = "TW3001";

    /// <summary>Template assembly load failure: a required dependency could not be resolved by the isolated load context.</summary>
    public const string TW3002 = "TW3002";

    /// <summary>Output write error.</summary>
    public const string TW4001 = "TW4001";

    /// <summary>Internal violation.</summary>
    public const string TW9001 = "TW9001";
}
