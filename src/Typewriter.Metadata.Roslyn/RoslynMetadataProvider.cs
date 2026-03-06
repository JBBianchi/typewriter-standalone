using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Typewriter.CodeModel.Configuration;
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
                if (!IsProjectIncluded(project, settings))
                {
                    continue;
                }

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
                if (!IsProjectIncluded(project, settings))
                {
                    continue;
                }

                foreach (var document in project.Documents
                    .Where(d => d.FilePath != null &&
                                d.FilePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)))
                {
                    yield return new RoslynFileMetadata(document, settings, requestRender);
                }
            }
        }

        /// <summary>
        /// Creates a <see cref="RenderQueue"/> scoped to all C# source documents in the
        /// workspace. The queue enforces deterministic FIFO ordering, deduplication by
        /// normalized path, scope boundary checks, and a bounded iteration safety cap.
        /// </summary>
        /// <param name="onEnqueued">
        /// Optional callback invoked when a file is successfully enqueued.
        /// Parameters are the file path and the current queue depth.
        /// Wire to <c>--verbosity detailed</c> diagnostics.
        /// </param>
        /// <param name="onOutOfScope">
        /// Optional callback invoked when an enqueue request is discarded because the path
        /// is outside the current workspace scope. Wire to <c>--verbosity detailed</c> diagnostics.
        /// </param>
        /// <param name="onCapReached">
        /// Optional callback invoked when the safety cap is reached and further enqueues are
        /// rejected. Wire to <c>--verbosity detailed</c> diagnostics.
        /// </param>
        /// <returns>A new <see cref="RenderQueue"/> scoped to the workspace documents.</returns>
        public RenderQueue CreateRenderQueue(
            Action<string, int> onEnqueued = null,
            Action<string> onOutOfScope = null,
            Action<int> onCapReached = null)
        {
            var scopePaths = GetScopePaths();
            return new RenderQueue(scopePaths, onEnqueued, onOutOfScope, onCapReached);
        }

        /// <summary>
        /// Processes files through a <see cref="RenderQueue"/>, yielding <see cref="IFileMetadata"/>
        /// for each dequeued path. The queue's <c>requestRender</c> callback is wired so that
        /// re-render requests from combined partial mode are automatically enqueued and processed.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The caller must seed the <paramref name="renderQueue"/> before calling this method
        /// (e.g. via <see cref="SeedRenderQueue"/>). Files enqueued via the <c>requestRender</c>
        /// callback during metadata extraction are also yielded, subject to the queue's
        /// deduplication, scope boundary, and safety cap constraints.
        /// </para>
        /// <para>
        /// Implements Q2 resolution constraints from
        /// <c>_archive/Q2-request-render-batch-mode-resolution-notes.md</c>.
        /// </para>
        /// </remarks>
        /// <param name="renderQueue">
        /// A <see cref="RenderQueue"/> previously created via <see cref="CreateRenderQueue"/> and
        /// seeded with initial file paths.
        /// </param>
        /// <param name="settings">Template settings controlling rendering behaviour.</param>
        /// <returns>
        /// One <see cref="IFileMetadata"/> per successfully dequeued and resolved file path.
        /// </returns>
        public IEnumerable<IFileMetadata> ProcessRenderQueue(RenderQueue renderQueue, Settings settings)
        {
            var requestRender = renderQueue.CreateRequestRenderCallback();

            while (renderQueue.TryDequeue(out var path))
            {
                var metadata = GetFile(path, settings, requestRender);
                if (metadata != null)
                {
                    yield return metadata;
                }
            }
        }

        /// <summary>
        /// Seeds a <see cref="RenderQueue"/> with all in-scope C# source documents from the
        /// workspace, in topological project order and document order within each project.
        /// </summary>
        /// <param name="renderQueue">The render queue to seed.</param>
        public void SeedRenderQueue(RenderQueue renderQueue)
        {
            foreach (var path in GetScopePaths())
            {
                renderQueue.TryEnqueue(path);
            }
        }

        /// <summary>
        /// Returns the ordered list of in-scope C# source file paths from the workspace,
        /// in topological project order and document order within each project.
        /// </summary>
        private List<string> GetScopePaths()
        {
            var paths = new List<string>();
            foreach (var (project, _) in _workspaceLoadResult.Entries)
            {
                foreach (var document in project.Documents
                    .Where(d => d.FilePath != null &&
                                d.FilePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)))
                {
                    paths.Add(document.FilePath);
                }
            }

            return paths;
        }

        private static bool IsProjectIncluded(Project project, Settings settings)
        {
            if (settings is not SettingsImpl runtimeSettings || !runtimeSettings.HasExplicitProjectSelection)
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(project.FilePath))
            {
                return false;
            }

            return runtimeSettings.IncludedProjects.Contains(project.FilePath, GetPathComparer());
        }

        private static StringComparer GetPathComparer() =>
            OperatingSystem.IsWindows()
                ? StringComparer.OrdinalIgnoreCase
                : StringComparer.Ordinal;
    }
}
