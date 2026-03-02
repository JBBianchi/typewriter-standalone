namespace Typewriter.Application.Configuration;

/// <summary>POCO that maps to the <c>typewriter.json</c> configuration file schema.</summary>
public class TypewriterConfig
{
    public string? TemplatesGlob { get; set; }
    public string? Solution { get; set; }
    public string? Project { get; set; }
    public string? Framework { get; set; }
    public string? Configuration { get; set; }
    public string? Runtime { get; set; }
    public string? Output { get; set; }
    public string? Verbosity { get; set; }
    public bool? FailOnWarnings { get; set; }
}
