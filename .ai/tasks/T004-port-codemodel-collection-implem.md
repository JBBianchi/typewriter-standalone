# T004: Port CodeModel Collection Implementations
- Milestone: M1
- Status: Done
- Agent: Executor (claude-sonnet-4-6)
- Started: 2026-03-02
- Completed: 2026-03-02

## Objective

Copy all 19 `*CollectionImpl.cs` files from `origin/src/Typewriter/CodeModel/Collections/` into `src/Typewriter.CodeModel/Collections/`, keeping namespace `Typewriter.CodeModel.Collections` (already matches target). Also port all CodeModel abstract types and supporting files they depend on, so `dotnet build src/Typewriter.CodeModel` succeeds.

## Approach

1. Read T001 audit (all 19 collection files confirmed clean).
2. Identify full dependency chain needed for build success:
   - Collection impl files need CodeModel abstract types (`Class`, `Item`, `IClassCollection`, etc.) from `origin/src/CodeModel/CodeModel/`.
   - Those types need `Typewriter.CodeModel.Attributes.ContextAttribute`.
   - `Type.cs` needs `Settings` from `Typewriter.Configuration`; resolved via stub in `Typewriter.Metadata` (T003 decision, avoids circular dep).
3. Port 42 files from `origin/src/CodeModel/CodeModel/` → `src/Typewriter.CodeModel/` (same namespace).
4. Port `ContextAttribute.cs` → `src/Typewriter.CodeModel/Attributes/`.
5. Port `PartialRenderingMode.cs` → `src/Typewriter.CodeModel/Configuration/` (for future use).
6. Modify `Type.cs`: change `using Typewriter.Configuration;` → `using Typewriter.Metadata;` to use the `Settings` stub.
7. Port all 19 `*CollectionImpl.cs` → `src/Typewriter.CodeModel/Collections/`.
8. Remove `Placeholder.cs` stub.

## Journey

### 2026-03-02

- Confirmed T001 audit: all 19 files in `origin/src/Typewriter/CodeModel/Collections/` are clean (no VS/COM refs).
- Task description says 17 files, but 19 actually exist — ported all 19.
- Traced dependency chain: collection impls → CodeModel abstract types → `ContextAttribute` + `Settings` + (optional) `PartialRenderingMode`.
- Key finding: `Type.cs` references `Typewriter.Configuration.Settings`. Since `Typewriter.CodeModel` → `Typewriter.Metadata` (CodeModel depends on Metadata), creating a `Settings` in `Typewriter.Configuration` inside CodeModel would be fine. However, T003 already created a stub `Settings` in `Typewriter.Metadata` to resolve this exact pattern. Adapted `Type.cs` to use `using Typewriter.Metadata;` → `Settings` resolves to the stub.
- Ported 42 abstract types (all clean, no VS refs), ContextAttribute, PartialRenderingMode, and 19 CollectionImpl files.
- `dotnet` not available in execution environment; build verification deferred to CI.
- Verified `origin/` unchanged.
- Confirmed zero VS/COM refs in all ported files.

## Outcome

Files created or modified:

| Category | Count | Target |
|----------|-------|--------|
| CodeModel abstract types | 42 | `src/Typewriter.CodeModel/*.cs` |
| ContextAttribute | 1 | `src/Typewriter.CodeModel/Attributes/ContextAttribute.cs` |
| PartialRenderingMode | 1 | `src/Typewriter.CodeModel/Configuration/PartialRenderingMode.cs` |
| CollectionImpl files | 19 | `src/Typewriter.CodeModel/Collections/*.cs` |
| Removed | 1 | `src/Typewriter.CodeModel/Placeholder.cs` |
| Modified | 1 | `src/Typewriter.CodeModel/Type.cs` (namespace `Typewriter.Configuration` → `Typewriter.Metadata` for `Settings`) |

`origin/` unchanged.

## Follow-ups

- T005+: Port `origin/src/Typewriter/CodeModel/Implementation/*.cs` (19 files, all clean per T001 audit).
- T006+: Port `origin/src/Typewriter/CodeModel/Helpers.cs` (1 file, clean).
- Future: When `Settings` is fully ported (with all abstract members), update stub in `src/Typewriter.Metadata/Settings.cs` and change `Type.cs` back to use `Typewriter.Configuration.Settings` in the proper project.
- `PartialRenderingMode.cs` and the full `Settings` abstract class (`origin/src/CodeModel/Configuration/Settings.cs`) to be reconciled in a future task once all deps are clear.
