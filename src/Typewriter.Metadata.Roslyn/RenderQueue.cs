using System;
using System.Collections.Generic;

namespace Typewriter.Metadata.Roslyn
{
    /// <summary>
    /// Deterministic FIFO render queue that supports enqueue from the <c>requestRender</c>
    /// callback, with deduplication by normalized (case-insensitive) path, scope boundary
    /// enforcement, and a bounded iteration safety cap.
    /// </summary>
    /// <remarks>
    /// Implements Q2 resolution constraints from
    /// <c>_archive/Q2-request-render-batch-mode-resolution-notes.md</c>:
    /// <list type="bullet">
    /// <item>Deterministic render-session queue (FIFO, deduplicated by path).</item>
    /// <item>Bounded iteration safety cap (<see cref="MaxRenderIterations"/>).</item>
    /// <item>Scope boundary enforcement (callback-enqueued files must be in scope).</item>
    /// <item>Observability callbacks for detailed-level diagnostics.</item>
    /// </list>
    /// </remarks>
    public sealed class RenderQueue
    {
        /// <summary>
        /// Maximum number of distinct files allowed per render session (safety cap = 100).
        /// If this cap is reached, the queue rejects further enqueues to prevent infinite loops.
        /// </summary>
        public const int MaxRenderIterations = 100;

        private readonly Queue<string> _queue = new();
        private readonly HashSet<string> _processedOrQueued;
        private readonly HashSet<string> _scopePaths;
        private readonly Action<string, int> _onEnqueued;
        private readonly Action<string> _onOutOfScope;
        private readonly Action<int> _onCapReached;

        /// <summary>
        /// Initializes a new <see cref="RenderQueue"/> with the given scope and observability callbacks.
        /// </summary>
        /// <param name="scopePaths">
        /// The set of file paths that are valid render targets in this session. Paths outside
        /// this set are discarded when enqueued via <see cref="TryEnqueue"/>.
        /// </param>
        /// <param name="onEnqueued">
        /// Optional callback invoked when a new file is successfully enqueued.
        /// Parameters are the file path and the current queue depth.
        /// Wire to <c>detailed</c>-level diagnostics.
        /// </param>
        /// <param name="onOutOfScope">
        /// Optional callback invoked when an enqueue request is discarded because the path
        /// falls outside the current scope. Wire to <c>detailed</c>-level diagnostics.
        /// </param>
        /// <param name="onCapReached">
        /// Optional callback invoked when the safety cap is reached and further enqueues are
        /// rejected. Parameter is the cap value. Wire to <c>detailed</c>-level diagnostics.
        /// </param>
        public RenderQueue(
            IEnumerable<string> scopePaths,
            Action<string, int> onEnqueued = null,
            Action<string> onOutOfScope = null,
            Action<int> onCapReached = null)
        {
            _scopePaths = new HashSet<string>(scopePaths, StringComparer.OrdinalIgnoreCase);
            _processedOrQueued = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _onEnqueued = onEnqueued;
            _onOutOfScope = onOutOfScope;
            _onCapReached = onCapReached;
        }

        /// <summary>Gets the number of files currently waiting in the queue.</summary>
        public int Count => _queue.Count;

        /// <summary>Gets the total number of distinct files that have been enqueued (including already processed).</summary>
        public int TotalEnqueued => _processedOrQueued.Count;

        /// <summary>Gets a value indicating whether the safety cap has been reached.</summary>
        public bool CapReached => _processedOrQueued.Count >= MaxRenderIterations;

        /// <summary>
        /// Attempts to enqueue a file path for rendering. The path is rejected if:
        /// <list type="bullet">
        /// <item>The safety cap (<see cref="MaxRenderIterations"/>) has been reached.</item>
        /// <item>The path is outside the current scope.</item>
        /// <item>The path has already been enqueued or processed (deduplication).</item>
        /// </list>
        /// </summary>
        /// <param name="path">The absolute file path to enqueue.</param>
        /// <returns><see langword="true"/> if the path was enqueued; otherwise <see langword="false"/>.</returns>
        public bool TryEnqueue(string path)
        {
            if (_processedOrQueued.Count >= MaxRenderIterations)
            {
                _onCapReached?.Invoke(MaxRenderIterations);
                return false;
            }

            if (!_scopePaths.Contains(path))
            {
                _onOutOfScope?.Invoke(path);
                return false;
            }

            if (!_processedOrQueued.Add(path))
            {
                return false; // already queued or processed
            }

            _queue.Enqueue(path);
            _onEnqueued?.Invoke(path, _queue.Count);
            return true;
        }

        /// <summary>
        /// Attempts to dequeue the next file path for rendering.
        /// </summary>
        /// <param name="path">When this method returns <see langword="true"/>, contains the next path.</param>
        /// <returns><see langword="true"/> if a path was dequeued; otherwise <see langword="false"/>.</returns>
        public bool TryDequeue(out string path)
        {
            return _queue.TryDequeue(out path);
        }

        /// <summary>
        /// Creates an <see cref="Action{T}"/> callback suitable for passing as the
        /// <c>requestRender</c> parameter to
        /// <see cref="IMetadataProvider.GetFile(string, Typewriter.Configuration.Settings, Action{string[]})"/>
        /// and <see cref="RoslynFileMetadata"/>.
        /// Each path in the callback argument array is individually enqueued via <see cref="TryEnqueue"/>.
        /// </summary>
        /// <returns>A callback that enqueues the requested paths.</returns>
        public Action<string[]> CreateRequestRenderCallback()
        {
            return paths =>
            {
                foreach (var path in paths)
                {
                    TryEnqueue(path);
                }
            };
        }
    }
}
