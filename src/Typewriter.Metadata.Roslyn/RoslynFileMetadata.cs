using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Typewriter.Configuration;
using Typewriter.Metadata;

namespace Typewriter.Metadata.Roslyn
{
    /// <summary>
    /// Roslyn-based implementation of <see cref="IFileMetadata"/> that extracts type metadata
    /// from a Roslyn <see cref="Document"/> via its semantic model.
    /// </summary>
    /// <remarks>
    /// The semantic model and syntax root are resolved synchronously during construction via
    /// <c>GetAwaiter().GetResult()</c>.  This is safe in the CLI host because there is no
    /// ambient <see cref="System.Threading.SynchronizationContext"/> that could deadlock.
    /// </remarks>
    public class RoslynFileMetadata : IFileMetadata
    {
        private readonly Action<string[]> _requestRender;
        private readonly Document _document;
        private readonly SyntaxNode _root;
        private readonly SemanticModel _semanticModel;

        /// <summary>
        /// Initializes a new <see cref="RoslynFileMetadata"/> from a Roslyn <see cref="Document"/>.
        /// </summary>
        /// <param name="document">The Roslyn document to extract metadata from.</param>
        /// <param name="settings">Template settings controlling rendering behaviour.</param>
        /// <param name="requestRender">
        /// Callback invoked when a partial type's canonical file should be re-rendered.
        /// May be <see langword="null"/>.
        /// </param>
        public RoslynFileMetadata(Document document, Settings settings, Action<string[]> requestRender)
        {
            _requestRender = requestRender;
            _document = document;
            Settings = settings;

            // Resolve semantic model and syntax root synchronously.  The CLI runs outside any
            // VS thread-pool context, so GetAwaiter().GetResult() is safe here.
            _semanticModel = document.GetSemanticModelAsync().GetAwaiter().GetResult();
            _root = _semanticModel.SyntaxTree.GetRootAsync().GetAwaiter().GetResult();
        }

        /// <summary>Gets the settings associated with the template rendering this file.</summary>
        public Settings Settings { get; }

        /// <inheritdoc />
        public string Name => _document.Name;

        /// <inheritdoc />
        public string FullName => _document.FilePath;

        /// <inheritdoc />
        public IEnumerable<IClassMetadata> Classes =>
            RoslynClassMetadata.FromNamedTypeSymbols(GetNamespaceChildNodes<ClassDeclarationSyntax>(), this, Settings);

        /// <inheritdoc />
        public IEnumerable<IDelegateMetadata> Delegates =>
            RoslynDelegateMetadata.FromNamedTypeSymbols(GetNamespaceChildNodes<DelegateDeclarationSyntax>(), Settings);

        /// <inheritdoc />
        public IEnumerable<IEnumMetadata> Enums =>
            RoslynEnumMetadata.FromNamedTypeSymbols(GetNamespaceChildNodes<EnumDeclarationSyntax>(), Settings);

        /// <inheritdoc />
        public IEnumerable<IInterfaceMetadata> Interfaces =>
            RoslynInterfaceMetadata.FromNamedTypeSymbols(GetNamespaceChildNodes<InterfaceDeclarationSyntax>(), this, Settings);

        /// <inheritdoc />
        public IEnumerable<IRecordMetadata> Records =>
            RoslynRecordMetadata.FromNamedTypeSymbols(GetNamespaceChildNodes<RecordDeclarationSyntax>(), this, Settings);

        private IEnumerable<INamedTypeSymbol> GetNamespaceChildNodes<T>()
            where T : SyntaxNode
        {
#pragma warning disable RS1039 // This call to 'SemanticModel.GetDeclaredSymbol()' will always return 'null'
            var symbols = _root.ChildNodes().OfType<T>().Concat(
                _root.ChildNodes().OfType<NamespaceDeclarationSyntax>()
                    .SelectMany(n => n.ChildNodes().OfType<T>())).Concat(
                _root.ChildNodes().OfType<FileScopedNamespaceDeclarationSyntax>()
                    .SelectMany(n => n.ChildNodes().OfType<T>()))
                .Select(c => _semanticModel.GetDeclaredSymbol(c) as INamedTypeSymbol);
#pragma warning restore RS1039

            if (Settings.PartialRenderingMode == PartialRenderingMode.Combined)
            {
                return symbols.Where(s =>
                {
                    var locationToRender = s?.Locations
                        .Select(l => l.SourceTree?.FilePath)
                        .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                        .FirstOrDefault();

                    if (string.Equals(locationToRender, FullName, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }

                    if (locationToRender != null)
                    {
                        _requestRender?.Invoke(new[] { locationToRender });
                    }

                    return false;
                }).ToList();
            }

            return symbols;
        }
    }
}
