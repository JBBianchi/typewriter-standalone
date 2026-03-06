using Typewriter.VisualStudio;
using File = Typewriter.CodeModel.File;

namespace Typewriter.Configuration;

/// <summary>
/// Compatibility surface for upstream templates that reference
/// <c>Typewriter.Configuration.Settings</c>.
/// </summary>
/// <remarks>
/// The CLI runtime stores core setting state in <see cref="Typewriter.Metadata.Settings"/>.
/// This type preserves the upstream namespace/API shape while reusing that base contract.
/// </remarks>
public abstract class Settings : Typewriter.Metadata.Settings
{
    /// <summary>
    /// Gets or sets a filename factory used to compute output file names.
    /// </summary>
    public virtual Func<File, string>? OutputFilenameFactory { get; set; }

    /// <summary>
    /// Gets a logger compatible with upstream template expectations.
    /// </summary>
    public abstract ILog Log { get; }
}
