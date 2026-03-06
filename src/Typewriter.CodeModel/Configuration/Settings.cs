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
    private Func<File, string>? _outputFilenameFactory;

    /// <summary>
    /// Gets or sets a filename factory used to compute output file names.
    /// Keeps upstream typing for templates compiled against
    /// <c>Typewriter.Configuration.Settings</c> while syncing to the shared
    /// metadata contract.
    /// </summary>
    public new virtual Func<File, string>? OutputFilenameFactory
    {
        get
        {
            if (_outputFilenameFactory != null)
            {
                return _outputFilenameFactory;
            }

            if (base.OutputFilenameFactory == null)
            {
                return null;
            }

            return file => base.OutputFilenameFactory(file);
        }
        set
        {
            _outputFilenameFactory = value;
            base.OutputFilenameFactory = value == null
                ? null
                : file => value((File)file);
        }
    }

    /// <summary>
    /// Gets a logger compatible with upstream template expectations.
    /// </summary>
    public abstract ILog Log { get; }
}
