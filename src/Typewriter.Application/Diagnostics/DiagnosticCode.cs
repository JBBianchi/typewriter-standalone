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

    /// <summary>(Warning) SolutionFallbackService could not list projects from a .slnx file via dotnet sln list.</summary>
    public const string TW2310 = "TW2310";

    /// <summary>Multi-target default selection: TFM was chosen implicitly because --framework was not specified.</summary>
    public const string TW2401 = "TW2401";

    /// <summary>Template compile error.</summary>
    public const string TW3001 = "TW3001";

    /// <summary>Output write error.</summary>
    public const string TW4001 = "TW4001";

    /// <summary>Internal violation.</summary>
    public const string TW9001 = "TW9001";
}
