using System.Text.Json;
using Typewriter.Application;
using Typewriter.Application.Configuration;
using Xunit;

namespace Typewriter.UnitTests.Configuration;

public class ConfigurationPrecedenceTests
{
    // ---- Merge: CLI overrides ----

    [Fact]
    public void CliOverridesConfigAndTemplate()
    {
        var config = new TypewriterConfig { Framework = "net8.0" };

        var result = GenerateCommandOptions.Merge(
            config:        config,
            templates:     ["tmpl.tst"],
            solution:      null,
            project:       null,
            framework:     "net10.0",
            configuration: null,
            runtime:       null,
            restore:       false,
            output:        null,
            verbosity:     null,
            failOnWarnings: false);

        Assert.Equal("net10.0", result.Framework);
    }

    [Fact]
    public void ConfigFallsBackWhenCliIsNull()
    {
        var config = new TypewriterConfig { Framework = "net8.0" };

        var result = GenerateCommandOptions.Merge(
            config:        config,
            templates:     ["tmpl.tst"],
            solution:      null,
            project:       null,
            framework:     null,
            configuration: null,
            runtime:       null,
            restore:       false,
            output:        null,
            verbosity:     null,
            failOnWarnings: false);

        Assert.Equal("net8.0", result.Framework);
    }

    [Fact]
    public void DefaultVerbosityIsNormal()
    {
        var result = GenerateCommandOptions.Merge(
            config:        null,
            templates:     ["tmpl.tst"],
            solution:      null,
            project:       null,
            framework:     null,
            configuration: null,
            runtime:       null,
            restore:       false,
            output:        null,
            verbosity:     null,
            failOnWarnings: false);

        Assert.Equal("normal", result.Verbosity);
    }

    [Fact]
    public void CliVerbosityOverridesConfigAndDefault()
    {
        var config = new TypewriterConfig { Verbosity = "quiet" };

        var result = GenerateCommandOptions.Merge(
            config:        config,
            templates:     ["tmpl.tst"],
            solution:      null,
            project:       null,
            framework:     null,
            configuration: null,
            runtime:       null,
            restore:       false,
            output:        null,
            verbosity:     "detailed",
            failOnWarnings: false);

        Assert.Equal("detailed", result.Verbosity);
    }

    [Fact]
    public void ConfigVerbosityUsedWhenCliIsNull()
    {
        var config = new TypewriterConfig { Verbosity = "minimal" };

        var result = GenerateCommandOptions.Merge(
            config:        config,
            templates:     ["tmpl.tst"],
            solution:      null,
            project:       null,
            framework:     null,
            configuration: null,
            runtime:       null,
            restore:       false,
            output:        null,
            verbosity:     null,
            failOnWarnings: false);

        Assert.Equal("minimal", result.Verbosity);
    }

    [Fact]
    public void FailOnWarnings_ConfigTrueIsPreservedWhenCliFalse()
    {
        var config = new TypewriterConfig { FailOnWarnings = true };

        var result = GenerateCommandOptions.Merge(
            config:        config,
            templates:     ["tmpl.tst"],
            solution:      null,
            project:       null,
            framework:     null,
            configuration: null,
            runtime:       null,
            restore:       false,
            output:        null,
            verbosity:     null,
            failOnWarnings: false);

        Assert.True(result.FailOnWarnings);
    }

    [Fact]
    public void FailOnWarnings_CliTrueOverridesConfigFalse()
    {
        var config = new TypewriterConfig { FailOnWarnings = false };

        var result = GenerateCommandOptions.Merge(
            config:        config,
            templates:     ["tmpl.tst"],
            solution:      null,
            project:       null,
            framework:     null,
            configuration: null,
            runtime:       null,
            restore:       false,
            output:        null,
            verbosity:     null,
            failOnWarnings: true);

        Assert.True(result.FailOnWarnings);
    }

    [Fact]
    public void NullConfig_AllFieldsUseCliOrDefault()
    {
        var result = GenerateCommandOptions.Merge(
            config:        null,
            templates:     ["a.tst", "b.tst"],
            solution:      "my.sln",
            project:       null,
            framework:     "net9.0",
            configuration: "Release",
            runtime:       "linux-x64",
            restore:       true,
            output:        "./out",
            verbosity:     "quiet",
            failOnWarnings: true);

        Assert.Equal(new[] { "a.tst", "b.tst" }, result.Templates);
        Assert.Equal("my.sln",    result.Solution);
        Assert.Null(result.Project);
        Assert.Equal("net9.0",    result.Framework);
        Assert.Equal("Release",   result.Configuration);
        Assert.Equal("linux-x64", result.Runtime);
        Assert.True(result.Restore);
        Assert.Equal("./out",     result.Output);
        Assert.Equal("quiet",     result.Verbosity);
        Assert.True(result.FailOnWarnings);
    }

    // ---- TypewriterConfigLoader ----

    [Fact]
    public void Loader_ReturnsNull_WhenNoConfigFileExists()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        // Place a .git sentinel so the walker stops here instead of going further.
        Directory.CreateDirectory(Path.Combine(tempDir, ".git"));
        try
        {
            var result = TypewriterConfigLoader.Load(tempDir);
            Assert.Null(result);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void Loader_ReturnsConfig_WhenFileExistsInStartDirectory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        Directory.CreateDirectory(Path.Combine(tempDir, ".git"));

        var json = """{"framework": "net8.0", "verbosity": "quiet"}""";
        File.WriteAllText(Path.Combine(tempDir, "typewriter.json"), json);
        try
        {
            var result = TypewriterConfigLoader.Load(tempDir);
            Assert.NotNull(result);
            Assert.Equal("net8.0", result.Framework);
            Assert.Equal("quiet",  result.Verbosity);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void Loader_FindsConfigInAncestorDirectory()
    {
        var root    = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var subDir  = Path.Combine(root, "src", "templates");
        Directory.CreateDirectory(subDir);
        Directory.CreateDirectory(Path.Combine(root, ".git"));

        var json = """{"solution": "ancestor.sln"}""";
        File.WriteAllText(Path.Combine(root, "typewriter.json"), json);
        try
        {
            var result = TypewriterConfigLoader.Load(subDir);
            Assert.NotNull(result);
            Assert.Equal("ancestor.sln", result.Solution);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Loader_ParsesPartialConfig_LeavingMissingFieldsNull()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        Directory.CreateDirectory(Path.Combine(tempDir, ".git"));

        // Only "project" is present; all other fields must stay null.
        var json = """{"project": "MyApp.csproj"}""";
        File.WriteAllText(Path.Combine(tempDir, "typewriter.json"), json);
        try
        {
            var result = TypewriterConfigLoader.Load(tempDir);
            Assert.NotNull(result);
            Assert.Equal("MyApp.csproj", result.Project);
            Assert.Null(result.Framework);
            Assert.Null(result.Solution);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void Loader_ThrowsOrReturnsNull_OnMalformedJson()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        Directory.CreateDirectory(Path.Combine(tempDir, ".git"));

        File.WriteAllText(Path.Combine(tempDir, "typewriter.json"), "{ not valid json");
        try
        {
            // Malformed JSON surfaces a JsonException (per task spec: "surface exception or return null").
            Assert.Throws<JsonException>(() => TypewriterConfigLoader.Load(tempDir));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }
}
