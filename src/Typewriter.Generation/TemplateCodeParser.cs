using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Typewriter.Generation.Lexing;
using Stream = Typewriter.Generation.Lexing.Stream;

namespace Typewriter.Generation;

/// <summary>
/// Parses template code, extracting <c>#reference</c> directives, code blocks, and lambda expressions.
/// Adapted from upstream <c>Typewriter.Generation.TemplateCodeParser</c> with VS coupling removed:
/// <c>EnvDTE.ProjectItem</c> replaced with <c>string templateFilePath</c>,
/// <c>PathResolver.ResolveRelative</c> replaced with path-based resolution,
/// <c>Log.Error</c> removed (non-fatal reference errors are silently caught).
/// </summary>
public static class TemplateCodeParser
{
    private static int counter;

    /// <summary>
    /// Parses a template string, compiling embedded code and extracting extension types.
    /// </summary>
    /// <param name="templateFilePath">Absolute path to the <c>.tst</c> template file.</param>
    /// <param name="template">The raw template content.</param>
    /// <param name="extensions">List populated with compiled extension types.</param>
    /// <returns>The processed template output, or <c>null</c> if the template is empty.</returns>
    public static string? Parse(string templateFilePath, string template, List<Type> extensions)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            return null;
        }

        var output = string.Empty;
        var stream = new Stream(template);
        var shadowClass = new ShadowClass();
        var contexts = new Contexts(shadowClass);

        shadowClass.Clear();

        while (stream.Advance())
        {
            if (ParseReference(stream, shadowClass, templateFilePath))
            {
                continue;
            }

            if (ParseCodeBlock(stream, shadowClass))
            {
                continue;
            }

            if (ParseLambda(stream, shadowClass, contexts, ref output))
            {
                continue;
            }

            output += stream.Current;
        }

        shadowClass.Parse();

        extensions.Clear();
        extensions.Add(Compiler.Compile(templateFilePath, shadowClass));
        extensions.AddRange(FindExtensionClasses(shadowClass));

        return output;
    }

    private static IEnumerable<Type> FindExtensionClasses(ShadowClass shadowClass)
    {
        var types = new List<Type>();

        var usings = shadowClass.Snippets.Where(s => s.Type == SnippetType.Using && s.Code.StartsWith("using", StringComparison.OrdinalIgnoreCase));
        foreach (var usingStatement in usings.Select(u => u.Code))
        {
            var ns = usingStatement.Remove(0, 5).Trim().Trim(';');

            foreach (var assembly in shadowClass.ReferencedAssemblies)
            {
                types.AddRange(assembly.GetExportedTypes().Where(t => string.Equals(t.Namespace, ns, StringComparison.OrdinalIgnoreCase) &&
                    t.GetMethods(BindingFlags.Static | BindingFlags.Public).Any(m =>
                        m.IsDefined(typeof(ExtensionAttribute), false) &&
                        string.Equals(m.GetParameters().First().ParameterType.Namespace, "Typewriter.CodeModel", StringComparison.OrdinalIgnoreCase))));
            }
        }

        return types;
    }

    private static bool ParseCodeBlock(Stream stream, ShadowClass shadowClass)
    {
        if (stream.Current == '$' && stream.Peek() == '{')
        {
            for (var i = 0; ; i--)
            {
                var current = stream.Peek(i);
                if (current == '`' || (current == '/' && stream.Peek(i - 1) == '/'))
                {
                    return false;
                }

                if (current == '\n' || current == char.MinValue)
                {
                    break;
                }
            }

            stream.Advance();

            var block = stream.PeekBlock(1, '{', '}');
            var codeStream = new Stream(block, stream.Position + 1);

            ParseUsings(codeStream, shadowClass);
            ParseCode(codeStream, shadowClass);

            stream.Advance(block.Length + 1);

            return true;
        }

        return false;
    }

    private static void ParseUsings(Stream stream, ShadowClass shadowClass)
    {
        stream.Advance();

        while (true)
        {
            stream.SkipWhitespace();

            if ((stream.Current == 'u' && string.Equals(stream.PeekWord(), "using", StringComparison.OrdinalIgnoreCase)) || (stream.Current == '/' && stream.Peek() == '/'))
            {
                var line = stream.PeekLine();
                shadowClass.AddUsing(line, stream.Position);
                stream.Advance(line.Length);

                continue;
            }

            break;
        }
    }

    private static void ParseCode(Stream stream, ShadowClass shadowClass)
    {
        var code = new StringBuilder();

        do
        {
            if (stream.Current != char.MinValue)
            {
                code.Append(stream.Current);
            }
        }
        while (stream.Advance());

        shadowClass.AddBlock(code.ToString(), 0);
    }

    private static bool ParseLambda(Stream stream, ShadowClass shadowClass, Contexts contexts, ref string template)
    {
        if (stream.Current == '$')
        {
            var identifier = stream.PeekWord(1);
            if (identifier != null)
            {
                var filter = stream.PeekBlock(identifier.Length + 2, '(', ')');
                if (filter != null && stream.Peek(filter.Length + 2 + identifier.Length + 1) == '[')
                {
                    try
                    {
                        var index = filter.IndexOf("=>", StringComparison.Ordinal);

                        if (index > 0)
                        {
                            var name = filter.Substring(0, index);

                            var contextName = identifier;
#pragma warning disable MA0026
                            // TODO: Make the TemplateCodeParser context aware
#pragma warning restore MA0026
                            if (string.Equals(contextName, "TypeArguments", StringComparison.OrdinalIgnoreCase))
                            {
                                contextName = "Types";
                            }
                            else if (contextName.StartsWith("Nested", StringComparison.OrdinalIgnoreCase))
                            {
                                contextName = contextName.Remove(0, 6);
                            }

                            var type = contexts.Find(contextName)?.Type.FullName;

                            if (type == null)
                            {
                                return false;
                            }

                            var methodIndex = counter++;

                            shadowClass.AddLambda(filter, type, name, methodIndex);

                            stream.Advance(filter.Length + 2 + identifier.Length);
                            template += $"${identifier}($__{methodIndex})";

                            return true;
                        }
                    }
                    catch
                    {
                    }
                }
            }
        }

        return false;
    }

    private static bool ParseReference(Stream stream, ShadowClass shadowClass, string templateFilePath)
    {
        const string keyword = "reference";

        if (stream.Current == '#' && stream.Peek() == keyword[0] && string.Equals(stream.PeekWord(1), keyword, StringComparison.OrdinalIgnoreCase))
        {
            var reference = stream.PeekLine(keyword.Length + 1);
            if (reference != null)
            {
                var len = reference.Length + keyword.Length + 1;
                reference = reference.Trim('"', ' ', '\n', '\r');
                try
                {
                    if (reference.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                    {
                        reference = ResolveReferencePath(reference, templateFilePath);
                    }

                    shadowClass.AddReference(reference);
                    return true;
                }
                catch
                {
                    // Reference resolution errors are non-fatal; diagnostics
                    // will be reported at a higher level when IDiagnosticReporter
                    // is wired into the template engine.
                }
                finally
                {
                    stream.Advance(len - 1);
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Resolves a DLL reference path relative to the template file location.
    /// Handles upstream <c>~\</c> (project-relative) and <c>~~\</c> (solution-relative)
    /// prefixes by resolving them relative to the template directory in CLI context.
    /// </summary>
    /// <param name="reference">The reference path from the <c>#reference</c> directive.</param>
    /// <param name="templateFilePath">Absolute path to the template file.</param>
    /// <returns>The resolved absolute path.</returns>
    internal static string ResolveReferencePath(string reference, string templateFilePath)
    {
        if (Path.IsPathRooted(reference))
        {
            return reference;
        }

        // Strip upstream ~\ (project-relative) and ~~\ (solution-relative) prefixes.
        // In CLI context, both resolve relative to the template file directory.
        if (reference.StartsWith("~~\\", StringComparison.Ordinal) || reference.StartsWith("~~/", StringComparison.Ordinal))
        {
            reference = reference.Substring(3);
        }
        else if (reference.StartsWith("~\\", StringComparison.Ordinal) || reference.StartsWith("~/", StringComparison.Ordinal))
        {
            reference = reference.Substring(2);
        }

        var templateDir = Path.GetDirectoryName(templateFilePath) ?? ".";
        return Path.GetFullPath(Path.Combine(templateDir, reference));
    }
}
