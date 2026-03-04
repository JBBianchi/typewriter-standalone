# Parity Matrix

Tracks upstream Typewriter parity status for each feature area.

**Tags**

| Tag | Meaning |
|-----|---------|
| `identical` | Output matches upstream exactly |
| `transformed` | Intentionally different with documented rationale |
| `deferred` | Not implemented in v1 |

## Feature Matrix

| Feature | Tag | Rationale / Notes | Golden Test Fixture |
|---------|-----|-------------------|---------------------|
| Class generation | `identical` | `$Classes` iteration, property/method mapping, inheritance, constants, nested types — all match upstream behavior | `simple` (UserModel via Interfaces.tst), `multi-project` (EntityBase, UserEntity via CrossProjectTypes.tst), `complex-types` (ClassInfo) |
| Enum generation | `identical` | `$Enums` iteration, auto-increment values, `[Flags]`, hex, byte underlying type, nested enums — all match upstream | `simple` (UserRole via Enums.tst), `complex-types` (EnumInfo, FlagsEnumInfo, HexEnumInfo) |
| Interface generation | `identical` | `$Interfaces` iteration, interface inheritance, generics, events, methods, properties — all match upstream | `simple` (INamedEntity via Interfaces.tst), `complex-types` (IInterfaceInfo, IBaseInterfaceInfo, IGenericInterface) |
| Nullable types | `identical` | Nullable reference/value type handling (`T?` → `T \| null`) matches upstream behavior | `complex-types` (NullableTypes via ComplexModels.tst, AsyncService via AsyncTypes.tst) |
| Generic types | `identical` | `$TypeArguments`, `$TypeParameters`, nested generic unwrapping (`Task<IHttpActionResult<T[]>>`) match upstream | `complex-types` (GenericRepository via ComplexModels.tst) |
| Partial classes | `identical` | Members from all partial declarations are merged into a single code-model class, matching upstream | `complex-types` (PartialEntity split across PartialEntity.cs + PartialEntity2.cs) |
| Multi-project references | `transformed` | `IncludeProject(name)` replaced with path-qualified selector and TW12xx ambiguity diagnostics; cross-project type resolution is preserved but project selection uses explicit paths instead of VS DTE name matching | `multi-project` (CrossProjectTypes.tst with DomainLib ↔ ApiLib references) |
| TFM selection | `transformed` | Upstream has no multi-target concept (single-TFM VS projects only). CLI adds deterministic first-declared-TFM default and `--framework` override. Template output stability across TFMs is identical to upstream single-TFM behavior | `multi-target` (PlatformInfo.tst with `net10.0;net8.0` dual-target project) |
| Source generator symbols | `transformed` | Upstream does not support source-generator-produced types. CLI exposes them via Roslyn workspace `RunGeneratorsAndUpdateCompilation`; `$Classes` iteration includes generated types with full property/method metadata | `source-generators` (SourceGenTypes.tst against HelloWorldGenerator output) |
| BOM policy | `identical` | UTF-8 output encoding with optional BOM preservation matches upstream file-writing behavior; `OutputWriter` preserves existing file BOM when overwriting | `simple` (any template output verifies encoding policy) |
| Collision naming | `identical` | When multiple input types produce the same output filename, `_1`/`_2` suffixes are appended, matching upstream `Template.GetOutputFilename` collision avoidance | `complex-types` (OutputPolicyTests verify `_1`/`_2` suffix behavior) |
| requestRender batch mode | `transformed` | Upstream `requestRender` callback triggers immediate re-render within VS host. CLI replaces with deterministic FIFO `RenderQueue` (dedup, 100-item safety cap, scope boundary) that processes all enqueued renders after the initial pass completes. See D-0004, #135 | `complex-types` (multi-template fixture exercising render queue convergence) |

## Fixture Index

| Fixture | Path | Features Covered |
|---------|------|------------------|
| `simple` | `tests/fixtures/simple/SimpleProject/` | Class, enum, interface generation; type mapping; BOM policy |
| `multi-project` | `tests/fixtures/multi-project/` | Multi-project references; cross-project type resolution; single-file mode |
| `multi-target` | `tests/fixtures/multi-target/MultiTargetLib/` | TFM selection; conditional compilation; `--framework` override |
| `source-generators` | `tests/fixtures/source-generators/` | Source generator symbol visibility; `$Classes` on generated types |
| `complex-types` | `tests/fixtures/complex-types/ComplexTypesLib/` | Nullable types; generic types; partial classes; collision naming; requestRender batch mode |

## References

- Feature inventory based on review findings from #152 ([T152-m7-fixture-review.md](../../.ai/tasks/T152-m7-fixture-review.md))
- Parity gap policy: see [AGENTS.md](../../AGENTS.md) §10 (Parity Drift Guard)
- Detailed implementation plan: see [DETAILED_IMPLEMENTATION_PLAN.md](../../DETAILED_IMPLEMENTATION_PLAN.md) M7
