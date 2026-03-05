using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Typewriter.CodeModel;
using Typewriter.Configuration;
using Typewriter.Generation.Lexing;
using File = System.IO.File;

namespace Typewriter.Generation;

/// <summary>
/// Manages a shadow Roslyn compilation unit for template code.
/// Collects snippets (usings, code blocks, lambdas) and assembles them into a compilable
/// <c>__Typewriter.Template</c> class that can be emitted to an assembly via
/// <see cref="Compile(string)"/>.
/// Adapted from upstream <c>Typewriter.TemplateEditor.Lexing.Roslyn.ShadowClass</c> with
/// the VS-coupled <c>ShadowWorkspace</c> replaced by direct <see cref="CSharpCompilation"/>.
/// </summary>
public class ShadowClass
{
    private const string StartTemplate = """
        namespace __Typewriter
        {
            using System;
            using System.Linq;
            using System.Collections.Generic;
            using Typewriter.CodeModel;
            using Typewriter.Configuration;
            using Attribute = Typewriter.CodeModel.Attribute;
            using Enum = Typewriter.CodeModel.Enum;
            using Type = Typewriter.CodeModel.Type;

        """;

    private const string ClassTemplate = """

            public class Template
            {

        """;

    private const string EndClassTemplate = """

            }
        """;

    private const string EndTemplate = """

        }

        """;

    private readonly List<Snippet> _snippets = [];
    private readonly HashSet<Assembly> _referencedAssemblies = [];
    private int _offset;
    private bool _classAdded;

    /// <summary>Gets the collected code snippets.</summary>
    public IEnumerable<Snippet> Snippets => _snippets;

    /// <summary>Gets the referenced assemblies.</summary>
    public IEnumerable<Assembly> ReferencedAssemblies => _referencedAssemblies;

    /// <summary>Clears all snippets, resets referenced assemblies, and prepares for a new template.</summary>
    public void Clear()
    {
        _snippets.Clear();
        _snippets.Add(Snippet.Create(SnippetType.Class, StartTemplate));
        _offset = StartTemplate.Length;
        _classAdded = false;

        _referencedAssemblies.Clear();
        _referencedAssemblies.Add(typeof(Class).Assembly);
        _referencedAssemblies.Add(typeof(PartialRenderingMode).Assembly);
    }

    /// <summary>Finalizes the shadow class by closing the class and namespace declarations.</summary>
    public void Parse()
    {
        if (!_classAdded)
        {
            _snippets.Add(Snippet.Create(SnippetType.Class, ClassTemplate));
            _offset += ClassTemplate.Length;
            _classAdded = true;
        }

        _snippets.Add(Snippet.Create(SnippetType.Class, EndClassTemplate));
        _snippets.Add(Snippet.Create(SnippetType.Class, EndTemplate));
    }

    /// <summary>
    /// Adds a using directive snippet.
    /// </summary>
    /// <param name="code">The using directive code.</param>
    /// <param name="startIndex">Start index in the template.</param>
    public void AddUsing(string code, int startIndex)
    {
        _snippets.Add(Snippet.Create(SnippetType.Using, code, _offset, startIndex, startIndex + code.Length));
        _offset += code.Length;
    }

    /// <summary>
    /// Adds a code block snippet.
    /// </summary>
    /// <param name="code">The code block text.</param>
    /// <param name="startIndex">Start index in the template.</param>
    public void AddBlock(string code, int startIndex)
    {
        if (!_classAdded)
        {
            _snippets.Add(Snippet.Create(SnippetType.Class, ClassTemplate));
            _offset += ClassTemplate.Length;
            _classAdded = true;
        }

        _snippets.Add(Snippet.Create(SnippetType.Code, code, _offset, startIndex, startIndex + code.Length));
        _offset += code.Length;
    }

    /// <summary>
    /// Adds an assembly reference by path or name.
    /// </summary>
    /// <param name="pathOrName">DLL file path or assembly name.</param>
    public void AddReference(string pathOrName)
    {
        var asm = pathOrName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
            ? Assembly.LoadFile(pathOrName)
            : Assembly.Load(pathOrName);

        _referencedAssemblies.Add(asm);
    }

    /// <summary>
    /// Adds a lambda expression snippet with a generated filter method wrapper.
    /// </summary>
    /// <param name="code">The lambda code (e.g. <c>c =&gt; c.Name == "Foo"</c>).</param>
    /// <param name="type">The full type name of the lambda parameter.</param>
    /// <param name="name">The parameter name.</param>
    /// <param name="startIndex">Unique method index for the generated wrapper.</param>
    public void AddLambda(string code, string type, string name, int startIndex)
    {
        if (!_classAdded)
        {
            _snippets.Add(Snippet.Create(SnippetType.Class, ClassTemplate));
            _offset += ClassTemplate.Length;
            _classAdded = true;
        }

        var method = $"bool __{startIndex} ({type} {name}) {{ return ";
        var index = code.IndexOf("=>", StringComparison.Ordinal) + 2;
        code = code.Remove(0, index);

        _snippets.Add(Snippet.Create(SnippetType.Class, method));
        _offset += method.Length;

        _snippets.Add(Snippet.Create(SnippetType.Lambda, code, _offset, startIndex, startIndex + code.Length, index));
        _offset += code.Length;

        _snippets.Add(Snippet.Create(SnippetType.Class, ";}"));
        _offset += 2;
    }

    /// <summary>
    /// Compiles the assembled template code into an assembly at the specified output path.
    /// Uses <see cref="CSharpCompilation"/> directly, replacing the upstream VS-coupled
    /// <c>ShadowWorkspace</c>.
    /// </summary>
    /// <param name="outputPath">Absolute path for the emitted assembly DLL.</param>
    /// <returns>The <see cref="EmitResult"/> from the Roslyn emit operation.</returns>
    public EmitResult Compile(string outputPath)
    {
        var sourceCode = string.Join(string.Empty, _snippets.Select(s => s.Code));
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);

        // Rewrite all methods and constructors to public static, matching upstream behavior.
        var root = syntaxTree.GetCompilationUnitRoot();
        root = MakeAllMethodsPublicStatic(root);
        syntaxTree = root.SyntaxTree;

        var metadataReferences = _referencedAssemblies
            .Select(ResolveAssemblyPath)
            .Where(path => path != null)
            .Select(path => MetadataReference.CreateFromFile(path!))
            .Cast<MetadataReference>()
            .ToList();

        // Add core runtime references that the upstream ShadowWorkspace included by default.
        AddCoreReferenceIfMissing(metadataReferences, typeof(object));     // System.Private.CoreLib
        AddCoreReferenceIfMissing(metadataReferences, typeof(Uri));        // System
        AddCoreReferenceIfMissing(metadataReferences, typeof(Enumerable)); // System.Linq
        AddCoreReferenceIfMissing(metadataReferences, typeof(Console));    // System.Console

        // .NET uses type-forwarding from System.Runtime for core types (Object,
        // IEnumerable<T>, etc.). Without this reference the compilation fails with
        // CS0012 "type is defined in an assembly that is not referenced".
        AddTrustedPlatformAssembly(metadataReferences, "System.Runtime.dll");
        AddTrustedPlatformAssembly(metadataReferences, "System.Collections.dll");
        AddTrustedPlatformAssembly(metadataReferences, "netstandard.dll");

        var compilation = CSharpCompilation.Create(
            assemblyName: Path.GetFileNameWithoutExtension(outputPath),
            syntaxTrees: [syntaxTree],
            references: metadataReferences,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var fileStream = File.Create(outputPath);
        return compilation.Emit(fileStream);
    }

    /// <summary>
    /// Resolves the file path of an assembly safely in both normal and single-file deployment
    /// modes, where <see cref="Assembly.Location"/> returns an empty string.
    /// </summary>
    /// <param name="assembly">The assembly to resolve.</param>
    /// <returns>
    /// The absolute file path of the assembly, or <c>null</c> if no path can be resolved.
    /// </returns>
    internal static string? ResolveAssemblyPath(Assembly assembly)
    {
#pragma warning disable IL3000 // Assembly.Location returns empty in single-file — handled by fallbacks below
        var location = assembly.Location;
#pragma warning restore IL3000

        if (!string.IsNullOrEmpty(location))
        {
            return location;
        }

        // Fallback: AppContext.BaseDirectory + assembly simple name + ".dll"
        var baseDirCandidate = Path.Combine(AppContext.BaseDirectory, assembly.GetName().Name + ".dll");
        if (File.Exists(baseDirCandidate))
        {
            return baseDirCandidate;
        }

        // Fallback: TRUSTED_PLATFORM_ASSEMBLIES lookup by filename
        var fileName = assembly.GetName().Name + ".dll";
        var trustedAssemblies = (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string)?
            .Split(Path.PathSeparator) ?? [];

        var match = trustedAssemblies
            .FirstOrDefault(a => string.Equals(Path.GetFileName(a), fileName, StringComparison.OrdinalIgnoreCase));

        if (match != null)
        {
            return match;
        }

        return null;
    }

    private static void AddTrustedPlatformAssembly(List<MetadataReference> references, string assemblyFileName)
    {
        var trustedAssemblies = (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string)?
            .Split(Path.PathSeparator) ?? [];

        var match = trustedAssemblies
            .FirstOrDefault(a => string.Equals(Path.GetFileName(a), assemblyFileName, StringComparison.OrdinalIgnoreCase));

        if (match != null && !references.Any(r => string.Equals(r.Display, match, StringComparison.OrdinalIgnoreCase)))
        {
            references.Add(MetadataReference.CreateFromFile(match));
        }
    }

    private static void AddCoreReferenceIfMissing(List<MetadataReference> references, System.Type type)
    {
        var location = type.Assembly.Location;
        if (string.IsNullOrEmpty(location))
        {
            return;
        }

        if (references.Any(r => string.Equals(r.Display, location, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        references.Add(MetadataReference.CreateFromFile(location));
    }

    private static CompilationUnitSyntax MakeAllMethodsPublicStatic(CompilationUnitSyntax root)
    {
        return (CompilationUnitSyntax)PublicStaticRewriter.Instance.Visit(root);
    }

    /// <summary>
    /// Single-pass syntax rewriter that adds <c>public static</c> modifiers to all methods
    /// and <c>public</c> to all constructors. Using a rewriter instead of iterative
    /// <see cref="SyntaxNode.ReplaceNode"/> avoids span-invalidation bugs when multiple
    /// declarations exist.
    /// </summary>
    private sealed class PublicStaticRewriter : CSharpSyntaxRewriter
    {
        public static readonly PublicStaticRewriter Instance = new();

        public override SyntaxNode? VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            var trivia = node.GetTrailingTrivia();
            var modifiers = SyntaxFactory.TokenList(
                SyntaxFactory.Token(SyntaxKind.PublicKeyword).WithTrailingTrivia(trivia));
            return node.WithModifiers(modifiers);
        }

        public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            var trivia = node.ReturnType.GetTrailingTrivia();
            var modifiers = SyntaxFactory.TokenList(
                SyntaxFactory.Token(SyntaxKind.PublicKeyword).WithTrailingTrivia(trivia),
                SyntaxFactory.Token(SyntaxKind.StaticKeyword).WithTrailingTrivia(trivia));
            return node.WithModifiers(modifiers);
        }
    }
}
