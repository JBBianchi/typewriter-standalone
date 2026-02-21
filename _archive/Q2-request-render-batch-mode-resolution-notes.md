# Q2 Resolution Notes: `requestRender` in Batch Mode

- Date: 2026-02-21
- Scope: CLI equivalent of upstream `requestRender` callback behavior for combined partial rendering
- Status: Decision constraints recorded (implementation in M5)

## Context
Upstream can trigger secondary renders through `requestRender` when combined partial mode determines another file is the canonical render location.

## Upstream Evidence
- Callback shape is part of metadata provider contract:
  - `origin/src/Metadata/Providers/IMetadataProvider.cs:9`
- Callback invocation in combined partial mode:
  - `origin/src/Roslyn/RoslynFileMetadata.cs:64`
  - `origin/src/Roslyn/RoslynFileMetadata.cs:78`
- Callback wired to render pipeline in VS host:
  - `origin/src/Typewriter/Generation/Controllers/GenerationController.cs:129`

## Resolution Constraints
1. Deterministic render-session queue:
   - queue supports enqueue from callback,
   - dedupe by normalized path,
   - stable processing order.
2. Bounded iteration safety cap:
   - enforce max rendered/enqueued files per session to avoid infinite loops.
3. Scope boundary:
   - callback-enqueued files must be inside current load/render scope.
4. Observability:
   - at `detailed` verbosity, log when callback enqueues a file not already queued/processed.

## Why this fits product direction
- Preserves upstream intent rather than dropping callback semantics.
- Works for one-shot batch CLI now.
- Reuses same scheduling semantics for future watch mode with different event source.

## Validation targets
- Baseline parity in combined partial scenarios.
- No duplicate render outputs from callback scheduling.
- Scope boundary enforced.
- Convergence under configured safety cap.
- Detailed enqueue logs visible in diagnostics output.
