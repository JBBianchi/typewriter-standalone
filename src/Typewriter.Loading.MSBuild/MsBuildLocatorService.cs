using Microsoft.Build.Locator;
using Typewriter.Application.Diagnostics;

namespace Typewriter.Loading.MSBuild;

public sealed class MsBuildLocatorService : IMsBuildLocatorService
{
    private static int _registered = 0;

    public void EnsureRegistered(IDiagnosticReporter reporter)
    {
        if (Interlocked.CompareExchange(ref _registered, 1, 0) == 0)
        {
            try
            {
                MSBuildLocator.RegisterDefaults();
            }
            catch (Exception ex)
            {
                reporter.Report(new DiagnosticMessage(
                    DiagnosticSeverity.Error,
                    DiagnosticCode.TW2001,
                    $"MSBuild SDK not found: {ex.Message}"));
                throw;
            }
        }
    }
}
