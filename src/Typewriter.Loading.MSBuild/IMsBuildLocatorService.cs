using Typewriter.Application.Diagnostics;

namespace Typewriter.Loading.MSBuild;

public interface IMsBuildLocatorService
{
    void EnsureRegistered(IDiagnosticReporter reporter);
}
