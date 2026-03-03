using NSubstitute;
using Typewriter.Application.Diagnostics;
using Typewriter.Loading.MSBuild;
using Xunit;

namespace Typewriter.UnitTests.Loading;

public class InputResolverTests : IDisposable
{
    private readonly List<string> _tempFiles = new();

    public void Dispose()
    {
        foreach (var f in _tempFiles)
        {
            if (File.Exists(f)) File.Delete(f);
        }
    }

    private string CreateTempFile(string extension)
    {
        string path = Path.Combine(Path.GetTempPath(), $"tw_test_{Guid.NewGuid():N}{extension}");
        File.WriteAllText(path, string.Empty);
        _tempFiles.Add(path);
        return path;
    }

    [Theory]
    [InlineData(".csproj")]
    [InlineData(".sln")]
    [InlineData(".slnx")]
    public async Task AcceptedExtensions_ExistingFile_ReturnsResolvedInput(string extension)
    {
        var reporter = Substitute.For<IDiagnosticReporter>();
        var resolver = new InputResolver();
        string path = CreateTempFile(extension);

        var result = await resolver.ResolveAsync(path, reporter);

        Assert.NotNull(result);
        Assert.Equal(path, result.ProjectPath);
        reporter.DidNotReceive().Report(Arg.Any<DiagnosticMessage>());
    }

    [Theory]
    [InlineData(".sln")]
    [InlineData(".slnx")]
    public async Task SlnAndSlnx_NonExistentFile_EmitsTW2002(string extension)
    {
        var reporter = Substitute.For<IDiagnosticReporter>();
        var resolver = new InputResolver();
        string path = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid():N}{extension}");

        var result = await resolver.ResolveAsync(path, reporter);

        Assert.Null(result);
        reporter.Received(1).Report(Arg.Is<DiagnosticMessage>(
            m => m.Code == DiagnosticCode.TW2002 && m.Severity == DiagnosticSeverity.Error));
    }

    [Fact]
    public async Task Csproj_NonExistentFile_EmitsTW2002()
    {
        var reporter = Substitute.For<IDiagnosticReporter>();
        var resolver = new InputResolver();
        string path = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid():N}.csproj");

        var result = await resolver.ResolveAsync(path, reporter);

        Assert.Null(result);
        reporter.Received(1).Report(Arg.Is<DiagnosticMessage>(
            m => m.Code == DiagnosticCode.TW2002 && m.Severity == DiagnosticSeverity.Error));
    }

    [Fact]
    public async Task UnsupportedExtension_EmitsTW2002()
    {
        var reporter = Substitute.For<IDiagnosticReporter>();
        var resolver = new InputResolver();
        string path = CreateTempFile(".txt");

        var result = await resolver.ResolveAsync(path, reporter);

        Assert.Null(result);
        reporter.Received(1).Report(Arg.Is<DiagnosticMessage>(
            m => m.Code == DiagnosticCode.TW2002 && m.Severity == DiagnosticSeverity.Error));
    }
}
