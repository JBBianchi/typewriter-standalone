using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Typewriter.Configuration;
using Typewriter.Metadata;

namespace Typewriter.Metadata.Roslyn
{
    /// <summary>
    /// Roslyn-based implementation of <see cref="IMetadataProvider"/> that extracts file metadata
    /// from a <see cref="WorkspaceLoadResult"/> without any Visual Studio or DTE dependencies.
    /// </summary>
    public class RoslynMetadataProvider : IMetadataProvider
    {
        private readonly WorkspaceLoadResult _workspaceLoadResult;

        /// <summary>
        /// Initializes a new <see cref="RoslynMetadataProvider"/> from a workspace load result.
        /// </summary>
        /// <param name="workspaceLoadResult">
        /// The workspace load result produced by <see cref="IRoslynWorkspaceService"/>.
        /// </param>
        public RoslynMetadataProvider(WorkspaceLoadResult workspaceLoadResult)
        {
            _workspaceLoadResult = workspaceLoadResult;
        }

        /// <summary>
        /// Returns the <see cref="IFileMetadata"/> for the C# document at the given path, or
        /// <c>null</c> if the path is not found in any loaded project.
        /// </summary>
        /// <param name="path">The absolute file path to look up.</param>
        /// <param name="settings">Template settings controlling rendering behaviour.</param>
        /// <param name="requestRender">
        /// Callback invoked when a partial type's canonical file should be re-rendered.
        /// May be <see langword="null"/>.
        /// </param>
        /// <returns>
        /// A new <see cref="RoslynFileMetadata"/> for the matched document, or <c>null</c> if not
        /// found.
        /// </returns>
        public IFileMetadata GetFile(string path, Settings settings, Action<string[]> requestRender)
        {
            foreach (var (project, _) in _workspaceLoadResult.Entries)
            {
                var document = project.Documents
                    .FirstOrDefault(d => string.Equals(d.FilePath, path, StringComparison.OrdinalIgnoreCase));
                if (document != null)
                {
                    return new RoslynFileMetadata(document, settings, requestRender);
                }
            }

            return null;
        }

        /// <summary>
        /// Enumerates all C# source documents from the workspace as <see cref="IFileMetadata"/>
        /// instances, in topological project order and document order within each project.
        /// </summary>
        /// <param name="settings">Template settings controlling rendering behaviour.</param>
        /// <param name="requestRender">
        /// Callback invoked when a partial type's canonical file should be re-rendered.
        /// May be <see langword="null"/>.
        /// </param>
        /// <returns>
        /// One <see cref="IFileMetadata"/> per C# document across all loaded project entries.
        /// </returns>
        public IEnumerable<IFileMetadata> GetFiles(Settings settings, Action<string[]> requestRender)
        {
            foreach (var (project, _) in _workspaceLoadResult.Entries)
            {
                foreach (var document in project.Documents
                    .Where(d => d.FilePath != null &&
                                d.FilePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)))
                {
                    yield return new RoslynFileMetadata(document, settings, requestRender);
                }
            }
        }
    }
}
