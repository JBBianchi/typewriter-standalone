using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Typewriter.CodeModel.Configuration;
using Typewriter.CodeModel.Implementation;
using Typewriter.Configuration;
using Typewriter.Metadata;
using Typewriter.Metadata.Roslyn;
using Xunit;

namespace Typewriter.UnitTests.Metadata;

/// <summary>
/// M5 parity tests that verify Roslyn metadata extraction, render queue behaviour,
/// and source-generator type visibility.
/// </summary>
public class MetadataParityTests
{
    // -------------------------------------------------------------------------
    // Shared helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Creates a minimal <see cref="Settings"/> instance for test use.
    /// </summary>
    private static SettingsImpl CreateSettings(
        PartialRenderingMode mode = PartialRenderingMode.Partial)
    {
        var settings = new SettingsImpl(templatePath: "test.tst");
        settings.PartialRenderingMode = mode;
        return settings;
    }

    /// <summary>
    /// Builds an <see cref="AdhocWorkspace"/>-backed <see cref="WorkspaceLoadResult"/> from
    /// inline source texts. Each source text becomes a separate document in a single project.
    /// </summary>
    private static (WorkspaceLoadResult Result, AdhocWorkspace Workspace) CreateWorkspaceFromSources(
        params (string FileName, string Source)[] sources)
    {
        var host = MefHostServices.Create(MefHostServices.DefaultAssemblies);
        var workspace = new AdhocWorkspace(host);
        var projectId = ProjectId.CreateNewId();
        var projectInfo = ProjectInfo.Create(
            projectId,
            VersionStamp.Default,
            "TestProject",
            "TestProject",
            LanguageNames.CSharp,
            compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
            metadataReferences: new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.ValueTuple<,>).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.ComponentModel.DataAnnotations.AllowedValuesAttribute).Assembly.Location),
            });

        workspace.AddProject(projectInfo);

        foreach (var (fileName, source) in sources)
        {
            var docId = DocumentId.CreateNewId(projectId);
            var docInfo = DocumentInfo.Create(docId, fileName, loader: TextLoader.From(
                TextAndVersion.Create(SourceText.From(source), VersionStamp.Default, fileName)),
                filePath: "/" + fileName);
            workspace.AddDocument(docInfo);
        }

        var project = workspace.CurrentSolution.GetProject(projectId)!;
        var compilation = project.GetCompilationAsync().GetAwaiter().GetResult()!;

        var result = new WorkspaceLoadResult(new[] { (project, compilation) });
        return (result, workspace);
    }

    // -------------------------------------------------------------------------
    // 1. NullableTaskTupleGenericParity
    // -------------------------------------------------------------------------

    /// <summary>
    /// Loads a simple fixture class with a <c>Task&lt;(string A, int B)&gt;</c> return type and
    /// asserts that <see cref="IFileMetadata"/> exposes the correct generic and tuple information.
    /// </summary>
    [Fact]
    public void NullableTaskTupleGenericParity()
    {
        const string source = """
            using System.Threading.Tasks;

            namespace TestFixture
            {
                public class ServiceWithTuple
                {
                    public Task<(string A, int B)> GetPairAsync() => Task.FromResult(("hello", 42));
                }
            }
            """;

        var (loadResult, workspace) = CreateWorkspaceFromSources(("ServiceWithTuple.cs", source));
        using (workspace)
        {
            var settings = CreateSettings();
            var provider = new RoslynMetadataProvider(loadResult);
            var files = provider.GetFiles(settings, null).ToList();

            Assert.Single(files);
            var file = files[0];

            var classes = file.Classes.ToList();
            Assert.Single(classes);

            var cls = classes[0];
            Assert.Equal("ServiceWithTuple", cls.Name);

            var methods = cls.Methods.ToList();
            Assert.Single(methods);

            var method = methods[0];
            Assert.Equal("GetPairAsync", method.Name);

            // The method return type should be unwrapped from Task<T> — the inner type is
            // the ValueTuple (string A, int B).
            var returnType = method.Type;
            Assert.True(returnType.IsTask, "Return type should be flagged as Task.");
            Assert.True(returnType.IsValueTuple, "Inner type should be a ValueTuple.");

            // Tuple elements should be exposed
            var tupleElements = returnType.TupleElements.ToList();
            Assert.Equal(2, tupleElements.Count);
            Assert.Equal("A", tupleElements[0].Name);
            Assert.Equal("B", tupleElements[1].Name);
        }
    }

    // -------------------------------------------------------------------------
    // 2. PartialCombinedMode_RequestRenderEquivalent
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="RoslynMetadataProvider"/> in <see cref="PartialRenderingMode.Combined"/>
    /// mode with a render queue produces the same file list as a direct full-parse pass.
    /// </summary>
    [Fact]
    public void PartialCombinedMode_RequestRenderEquivalent()
    {
        const string sourceA = """
            namespace TestFixture
            {
                public class Alpha { public int Value { get; set; } }
            }
            """;

        const string sourceB = """
            namespace TestFixture
            {
                public class Beta { public string Name { get; set; } }
            }
            """;

        var (loadResult, workspace) = CreateWorkspaceFromSources(
            ("Alpha.cs", sourceA),
            ("Beta.cs", sourceB));
        using (workspace)
        {
            var provider = new RoslynMetadataProvider(loadResult);

            // Full-parse pass (Partial mode, no render queue)
            var partialSettings = CreateSettings(PartialRenderingMode.Partial);
            var fullParseFiles = provider.GetFiles(partialSettings, null)
                .Select(f => f.FullName)
                .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Combined mode via render queue
            var combinedSettings = CreateSettings(PartialRenderingMode.Combined);
            var renderQueue = provider.CreateRenderQueue();
            provider.SeedRenderQueue(renderQueue);
            var queueFiles = provider.ProcessRenderQueue(renderQueue, combinedSettings)
                .Select(f => f.FullName)
                .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                .ToList();

            Assert.Equal(fullParseFiles.Count, queueFiles.Count);
            Assert.Equal(fullParseFiles, queueFiles);
        }
    }

    // -------------------------------------------------------------------------
    // 3. PartialCombinedMode_RequestRender_RespectsScopeBoundary
    // -------------------------------------------------------------------------

    /// <summary>
    /// Enqueues a cross-scope re-render request; asserts it is discarded (log emitted, queue unchanged).
    /// </summary>
    [Fact]
    public void PartialCombinedMode_RequestRender_RespectsScopeBoundary()
    {
        const string source = """
            namespace TestFixture
            {
                public class InScope { }
            }
            """;

        var (loadResult, workspace) = CreateWorkspaceFromSources(("InScope.cs", source));
        using (workspace)
        {
            string? outOfScopePath = null;
            var provider = new RoslynMetadataProvider(loadResult);
            var renderQueue = provider.CreateRenderQueue(
                onOutOfScope: path => outOfScopePath = path);
            provider.SeedRenderQueue(renderQueue);

            var countBeforeOutOfScope = renderQueue.Count;

            // Attempt to enqueue a path that is NOT in the workspace scope
            var callback = renderQueue.CreateRequestRenderCallback();
            callback(new[] { "/totally/out-of-scope/NotHere.cs" });

            // The out-of-scope callback should have fired
            Assert.Equal("/totally/out-of-scope/NotHere.cs", outOfScopePath);
            // Queue should not have grown beyond what was seeded
            Assert.Equal(countBeforeOutOfScope, renderQueue.Count);
        }
    }

    // -------------------------------------------------------------------------
    // 4. PartialCombinedMode_RequestRender_ConvergesWithinSafetyCap
    // -------------------------------------------------------------------------

    /// <summary>
    /// Feeds a circular re-render scenario; asserts the safety cap (100) terminates the loop.
    /// </summary>
    [Fact]
    public void PartialCombinedMode_RequestRender_ConvergesWithinSafetyCap()
    {
        // Build a workspace with enough unique files to exceed the safety cap.
        var sources = new List<(string FileName, string Source)>();
        for (var i = 0; i < RenderQueue.MaxRenderIterations + 10; i++)
        {
            sources.Add(($"Class{i}.cs", $$"""
                namespace TestFixture
                {
                    public class Class{{i}} { }
                }
                """));
        }

        var (loadResult, workspace) = CreateWorkspaceFromSources(sources.ToArray());
        using (workspace)
        {
            int? capValue = null;
            var provider = new RoslynMetadataProvider(loadResult);
            var renderQueue = provider.CreateRenderQueue(
                onCapReached: cap => capValue = cap);

            // Seed the queue — this should hit the safety cap before finishing all files
            provider.SeedRenderQueue(renderQueue);

            // The cap should have been reached
            Assert.True(renderQueue.CapReached, "Safety cap should have been reached.");
            Assert.NotNull(capValue);
            Assert.Equal(RenderQueue.MaxRenderIterations, capValue!.Value);

            // Total enqueued should equal the cap, not all 110 files
            Assert.Equal(RenderQueue.MaxRenderIterations, renderQueue.TotalEnqueued);
        }
    }

    // -------------------------------------------------------------------------
    // 5. PartialCombinedMode_RequestRender_DetailedLogsNewEnqueue
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that the <c>detailed</c> verbosity log callback is invoked when a new file
    /// is enqueued into the render queue.
    /// </summary>
    [Fact]
    public void PartialCombinedMode_RequestRender_DetailedLogsNewEnqueue()
    {
        const string source = """
            namespace TestFixture
            {
                public class LogTest { }
            }
            """;

        var (loadResult, workspace) = CreateWorkspaceFromSources(("LogTest.cs", source));
        using (workspace)
        {
            var enqueuedLogs = new List<(string Path, int Depth)>();
            var provider = new RoslynMetadataProvider(loadResult);
            var renderQueue = provider.CreateRenderQueue(
                onEnqueued: (path, depth) => enqueuedLogs.Add((path, depth)));

            // Seed the queue — this should trigger the onEnqueued callback for each file
            provider.SeedRenderQueue(renderQueue);

            // At least one enqueue log should have been recorded
            Assert.NotEmpty(enqueuedLogs);

            // The logged path should end with "LogTest.cs"
            Assert.Contains(enqueuedLogs, e => e.Path.EndsWith("LogTest.cs", StringComparison.OrdinalIgnoreCase));

            // Depth should be positive (at least 1 after the enqueue)
            Assert.All(enqueuedLogs, e => Assert.True(e.Depth >= 1, "Queue depth should be >= 1 after enqueue."));
        }
    }

    // -------------------------------------------------------------------------
    // 6. SourceGeneratorTypes_AreVisible
    // -------------------------------------------------------------------------

    /// <summary>
    /// Loads the source-generator fixture and asserts that generator-produced types are visible
    /// via <see cref="WorkspaceLoadResult"/> compilation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// PARITY-GAP: Source-generator types are visible in the Roslyn <see cref="Compilation"/>
    /// obtained via <c>CSharpGeneratorDriver</c>, but <see cref="WorkspaceLoadResult"/> holds
    /// a standard project compilation that does NOT include generator output unless the generator
    /// is wired as an analyzer in the MSBuild project graph.
    /// </para>
    /// <para>
    /// In unit-test scope we construct a standalone compilation and manually run the generator
    /// to verify the types appear. Full end-to-end visibility via <see cref="WorkspaceLoadResult"/>
    /// depends on the MSBuild workspace loading pipeline which is tested in integration tests.
    /// </para>
    /// </remarks>
    [Fact]
    public void SourceGeneratorTypes_AreVisible()
    {
        // Arrange: build a minimal compilation mirroring SourceGenLib
        var syntaxTree = CSharpSyntaxTree.ParseText(
            "namespace SourceGenLib { public sealed class Class1 { } }");

        var coreRef = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);

        var compilation = CSharpCompilation.Create(
            "SourceGenLib",
            syntaxTrees: [syntaxTree],
            references: [coreRef],
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // Act: run the incremental generator and obtain the updated compilation.
        var generator = new SourceGenerator.HelloWorldGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out var updatedCompilation,
            out var diagnostics);

        // Assert: the generated type is present
        var types = updatedCompilation.GetTypesByMetadataName("SourceGenLib.GeneratedHelper");
        Assert.NotEmpty(types);

        // Verify basic properties of the generated type
        var generatedType = types[0];
        Assert.True(generatedType.IsStatic, "GeneratedHelper should be a static class.");
        Assert.Equal("GeneratedHelper", generatedType.Name);

        // Verify the GeneratorName constant is present
        var generatorNameMember = generatedType.GetMembers("GeneratorName")
            .OfType<IFieldSymbol>()
            .FirstOrDefault();
        Assert.NotNull(generatorNameMember);
        Assert.Equal("HelloWorldGenerator", generatorNameMember!.ConstantValue);
    }

    // -------------------------------------------------------------------------
    // 7. AllowedValuesAttribute_ParamsArray_DoesNotCrash
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies params-array attributes such as <c>AllowedValues(null, ...)</c> do not
    /// throw during CodeModel attribute materialization and remain visible to templates.
    /// </summary>
    [Fact]
    public void AllowedValuesAttribute_ParamsArray_DoesNotCrash()
    {
        const string source = """
            using System.ComponentModel.DataAnnotations;

            namespace TestFixture
            {
                public static class TestPseudoEnum
                {
                    public const string Value1 = "value1";
                    public const string Value2 = "value2";
                }

                public sealed class TestModel
                {
                    [AllowedValues(null, TestPseudoEnum.Value1, TestPseudoEnum.Value2)]
                    public string? PseudoEnum { get; init; }
                }
            }
            """;

        var (loadResult, workspace) = CreateWorkspaceFromSources(("TestModel.cs", source));
        using (workspace)
        {
            var settings = CreateSettings();
            var provider = new RoslynMetadataProvider(loadResult);
            var fileMetadata = provider.GetFiles(settings, null).Single();
            var file = new FileImpl(fileMetadata, settings);

            var model = file.Classes.Single(c => c.Name == "TestModel");
            var property = model.Properties.Single(p => p.Name == "PseudoEnum");
            var attribute = property.Attributes.Single(a => a.Name == "AllowedValues");

            Assert.Equal("AllowedValues", attribute.Name);
            Assert.NotNull(attribute.Value);
            Assert.NotNull(attribute.Arguments);
        }
    }
}
