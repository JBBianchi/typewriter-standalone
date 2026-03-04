# T152: Review origin/ for fixture templates and reference outputs (M7)
- Milestone: M7
- Status: Done
- Agent: Executor (#152)
- Started: 2026-03-04
- Completed: 2026-03-04

## Objective

Examine `origin/src/Typewriter/` and `origin/src/Tests/` for existing `.tst`/`.tstemplate` fixture templates, reference expected outputs, and test input types. Catalogue what to port for each of the five M7 fixture sets: `simple`, `multi-project`, `multi-target`, `source-generators`, `complex-types`.

## Approach

- Searched `origin/` for `.tst`, `.tstemplate`, `.result`, and test support files
- Read all template, golden output, and fixture input files
- Mapped upstream test categories to M7 fixture sets
- Documented parity classification per feature

## Upstream Inventory

### Template files found

| # | Path | Format | Features exercised |
|---|------|--------|--------------------|
| 1 | `origin/src/ItemTemplates/Empty/Template.tst` | `.tst` | Custom extension method (`$LoudName`), class filter (`$Classes(*Model)`), property iteration, `$Type[$Default]`, `using Typewriter.Extensions.Types` |
| 2 | `origin/src/ItemTemplates/Models/Models.tst` | `.tst` | Class filter (`$Classes(Filter)`), property conditionals (`$IsPrimitive`, `$IsDate`, `$IsEnumerable`), `$Default`, `$Class` references, constructor/map pattern |
| 3 | `origin/src/ItemTemplates/Angular/WebApiController.tst` | `.tst` | Lambda filter (`$Classes(c=>c.BaseClass.Name == "ApiController")`), WebApi extensions (`Verb`, `Route`), regex route rewriting, `$Parent as Class`, custom functions (`$CalculatedType`, `$HttpConfig`, `$ServiceName`, `$Data`, `$Params`), `$Parameters[$name: $Type][, ]` separator syntax |
| 4 | `origin/src/Tests/Render/RoutedApiController/BooksController.tstemplate` | `.tstemplate` | Code block with `Extensions.Route(method)`, regex route parameter extraction with type coercion, `$AdjustedParameters`, `$AdjustedRoute`, `$Verb`, `$Parameters([FromBody])` attribute filter, `Promise<$Type>` |
| 5 | `origin/src/Tests/Render/WebApiControllerTests/WebApiController.tstemplate` | `.tstemplate` | `$Classes(*WebApiController)` filter, `$TypeParameters`, nested `$TypeArguments[$TypeArguments[$Name]]`, `$Route`, `$Verb` custom function |
| 6 | `origin/src/Tests/Render/WebApiControllerTests/SingleFile.tstemplate` | `.tstemplate` | `settings.SingleFileMode("index.d.ts")`, lambda filter on namespace, custom `NameAndProperties()` with `cls.FullName` + `cls.Properties`, multi-input aggregation |

### Golden output files (.result)

| # | Path | Validates |
|---|------|-----------|
| 1 | `origin/src/Tests/Render/RoutedApiController/BooksController.result` | Routed API controller → TypeScript module with route interpolation, HTTP verb resolution, `[FromBody]` parameter handling |
| 2 | `origin/src/Tests/Render/WebApiControllerTests/WebApiController.result` | WebAPI CRUD controller → TypeScript module with `Promise<ComplexClassModel[]>`, nested generic unwrapping |
| 3 | `origin/src/Tests/Render/WebApiControllerTests/SingleFile.result` | Multi-model single-file mode → combined class with `FullName:PropertyNames` per model |

### Test input C# types (CodeModel/Support)

| File | Types | Coverage |
|------|-------|----------|
| `ClassInfo.cs` | `ClassInfo`, `BaseClassInfo`, `GenericClassInfo<T>`, `InheritGenericClassInfo` | Inheritance, generics, constants, delegates, events, fields, methods, properties, nested class/interface/enum |
| `PropertyInfo.cs` | `PropertyInfo`, `GenericPropertyInfo<T>` | All primitives, DateTime/Guid/TimeSpan, enums, nullable enums, arrays, IEnumerable, List, Dictionary, IDictionary, getter/setter visibility |
| `MethodInfo.cs` | `MethodInfo`, `GenericMethodInfo<T>` | Void, generic, Task, Task\<string\>, Task\<int?\>, byte[] param, default parameter values |
| `EnumInfo.cs` | `EnumInfo`, `FlagsEnumInfo`, `HexEnumInfo`, `EnumContiningClassInfo` | Auto-increment values, char-to-int, `[Flags]`, hex, byte underlying type, nested enums |
| `IInterfaceInfo.cs` | `IInterfaceInfo`, `IBaseInterfaceInfo`, `IGenericInterface<T>`, `IInheritGenericInterfaceInfo` | Interface inheritance, generics, events, methods, properties, containing class |
| `DelegateInfo.cs` | `Delegate`, `GenericDelegate<T>`, `DelegateInfo`, `GenericDelegateInfo<T>` | Generic delegates, Task return, Dictionary/IDictionary return |
| `TypeInfo.cs` | `TypeInfo` | Type resolution: class refs, generic class, inherited generic, string, ICollection |
| `FileInfo.cs` | Public/Internal classes, delegates, enums, interfaces (with and without namespaces) | File-level code model, visibility scoping |
| `AttributeInfoAttribute.cs` + `AttributeTestClass.cs` | Custom attribute with overloads, test class with various argument combos | Attribute arguments: string, int, named, params, Type |
| `PartialClassInfo.cs` + `PartialClassInfo2.cs` | `PartialClassInfo` (split across 2 files) | Partial class merging: each file contributes separate members |
| `ConstantInfo.cs` | `ConstantInfo` | String with quotes, null, integer constants |
| `StaticReadOnlyFieldInfo.cs` | `StaticReadOnlyFieldInfo` | Static readonly fields |
| `EventInfo.cs` | `EventInfo` | Delegate and generic delegate events |

### WebAPI extension test types (Extensions/Support)

| File | Type | Coverage |
|------|------|----------|
| `HttpMethodController.cs` | `HttpMethodController` | HTTP verb detection by convention name (Get, GetAll, ListAll) + by attribute |
| `RouteController.cs` | `RouteController` | `[Route]` on methods: wildcard, named, HttpGet routes |
| `RouteControllerWithDefaultRoute.cs` | `RouteControllerWithDefaultRouteController` | Class-level `[Route("api/[controller]")]`, `[action]` substitution |
| `BaseController.cs` + `InheritedController.cs` | Controller inheritance | Route prefix inheritance from base class |
| `RouteControllerWithNullableParts.cs` | Nullable route params | `#nullable enable` parameters in routes |
| `RouteLessController.cs` | `RouteLessController` | Controller without Route attributes |

### Metadata test types (Metadata/Support)

| File | Type | Coverage |
|------|------|----------|
| `GeneratedClass.cs` + `GeneratedClass.Additional.cs` | `GeneratedClass` (partial) + `GeneratedClassMetadata` | `[MetadataType]` attribute merging, `[Key]`, `[Display(Name = ...)]` |

---

## Per-Fixture-Set Mapping

### 1. `simple` fixture set

**Purpose**: Basic single-project template rendering — class/enum/interface iteration, property/method mapping, type defaults.

**Upstream template examples to port**:

| Template | Source | What it validates |
|----------|--------|-------------------|
| `Template.tst` (Empty item template) | `origin/src/ItemTemplates/Empty/Template.tst` | Custom extension (`$LoudName`), `$Classes(*Model)` filter, `$Properties`, `$Type[$Default]` |
| `Models.tst` | `origin/src/ItemTemplates/Models/Models.tst` | Class filter, `$IsPrimitive`/`$IsDate`/`$IsEnumerable` conditionals, `$Default`, `$Class` nested references |

**Upstream input types to port**:
- `PropertyInfo.cs` — covers all primitive types, DateTime, Guid, enums, nullable, collections, dictionaries
- `ClassInfo.cs` — covers class features (inheritance, generics, nested types, constants, events)
- `EnumInfo.cs` — covers enum values, flags, hex, nested enums

**Expected output pattern**: TypeScript classes/interfaces with properly mapped types and defaults (e.g., `string` → `string`, `int` → `number`, `DateTime` → `Date`, `bool` → `boolean`, enums → enum references).

**Parity classification**:
| Feature | Parity tag |
|---------|-----------|
| `$Classes(filter)` wildcard | identical |
| `$Properties` iteration | identical |
| `$Type` mapping (primitives) | identical |
| `$Default` values | identical |
| `$IsPrimitive`/`$IsDate`/`$IsEnumerable` | identical |
| Custom extension methods (`$LoudName`) | identical |
| `$rootnamespace$` token | deferred (VS-specific, no project context in CLI) |

### 2. `multi-project` fixture set

**Purpose**: Template rendering across multiple projects (`.sln`/`.slnx` with >1 project), testing `IncludeProject(name)` and cross-project type references.

**Upstream template examples to port**:

| Template | Source | What it validates |
|----------|--------|-------------------|
| `SingleFile.tstemplate` | `origin/src/Tests/Render/WebApiControllerTests/SingleFile.tstemplate` | `settings.SingleFileMode("index.d.ts")`, lambda filter on namespace, aggregation from multiple source files into one output |
| `WebApiController.tst` (Angular) | `origin/src/ItemTemplates/Angular/WebApiController.tst` | `$Classes(c=>c.BaseClass.Name == ...)` lambda filter, `$Parent as Class`, cross-type references (controller → model) |

**Upstream input types to port**:
- `SingleFileModels/Model1.cs`, `Model2.cs`, `Model3.cs` — simple models with one property each, in separate files
- `WebApiController.cs` + `Support/ComplexClassModel.cs` — controller referencing model type from support directory

**Expected output pattern**: SingleFile → combined TypeScript class aggregating all models; WebApiController → TypeScript module with method signatures referencing types from other files/namespaces.

**Golden files**: `SingleFile.result`, `WebApiController.result`

**Parity classification**:
| Feature | Parity tag |
|---------|-----------|
| `settings.SingleFileMode(filename)` | identical |
| Lambda class filter on namespace | identical |
| `cls.FullName` / `cls.Properties` access | identical |
| Multi-file aggregation | identical |
| `IncludeProject(name)` | transformed (name-based → path-qualified selector with TW12xx ambiguity policy) |

### 3. `multi-target` fixture set

**Purpose**: Template rendering for projects with multiple target frameworks (e.g., `net10.0;net8.0`), verifying deterministic TFM selection and `--framework` override.

**Upstream template examples to port**:

| Template | Source | What it validates |
|----------|--------|-------------------|
| `BooksController.tstemplate` | `origin/src/Tests/Render/RoutedApiController/BooksController.tstemplate` | Complex route resolution with `Extensions.Route(method)`, regex parameter extraction, multi-parameter methods — serves as a rich rendering template to verify output is stable across TFMs |
| `WebApiController.tstemplate` | `origin/src/Tests/Render/WebApiControllerTests/WebApiController.tstemplate` | `$TypeParameters`, nested `$TypeArguments` — verifies generic type resolution is consistent across TFMs |

**Upstream input types to port**:
- `BooksController.cs` + `Support/Book.cs` — controller with `[RoutePrefix]`, `[Route]`, `[HttpGet]`, `[HttpPost]`, `[FromBody]`
- `WebApiController.cs` + `Support/ComplexClassModel.cs` — controller with `Task<IHttpActionResult<T>>` return types

**Expected output pattern**: Output must be byte-identical regardless of which TFM is selected (first-TFM default vs `--framework` override).

**Golden files**: `BooksController.result`, `WebApiController.result`

**Parity classification**:
| Feature | Parity tag |
|---------|-----------|
| First-TFM default selection | transformed (upstream doesn't have multi-target concept; CLI adds deterministic first-TFM default) |
| `--framework` override | transformed (new CLI feature) |
| Template output stability across TFMs | identical (output must not change based on TFM selection) |
| `$Route` extension method | identical |
| `$Verb` (HTTP method detection) | identical |
| `$Parameters([FromBody])` attribute filter | identical |

### 4. `source-generators` fixture set

**Purpose**: Verify that types produced by Roslyn source generators are visible in the metadata pipeline and can be iterated by templates.

**Upstream template examples to port**:

| Template | Source | What it validates |
|----------|--------|-------------------|
| `Template.tst` (adapted) | `origin/src/ItemTemplates/Empty/Template.tst` | Basic `$Classes` iteration — applied to a project containing source-generator-produced types to verify they appear in the code model |
| `Models.tst` (adapted) | `origin/src/ItemTemplates/Models/Models.tst` | `$Properties` iteration on source-generated types to verify property metadata extraction works |

**Note**: Upstream has **no source-generator fixtures** — this is a new capability in the standalone CLI. The existing `tests/Typewriter.IntegrationTests/Fixtures/SourceGenerators/` fixture (created in M5, #128) already validates that `Compilation.GetTypesByMetadataName` sees generator output. M7 extends this to verify template rendering against source-generated types.

**Upstream input types to port/adapt**:
- Existing M5 fixture: `SourceGenLib/Class1.cs` + `SourceGenerator/HelloWorldGenerator.cs` — IIncrementalGenerator producing `GeneratedHelper`
- Extend with a template that iterates `$Classes` and confirms `GeneratedHelper` appears

**Expected output pattern**: TypeScript class/interface for `GeneratedHelper` with its generated properties.

**Parity classification**:
| Feature | Parity tag |
|---------|-----------|
| Source-generator type visibility | transformed (upstream doesn't support; CLI adds via Roslyn workspace) |
| `$Classes` iteration includes generated types | transformed |
| Property/method metadata on generated types | transformed |

### 5. `complex-types` fixture set

**Purpose**: Exercise advanced C# type features — generics, nullable, partial classes, inheritance, attributes, delegates, events, nested types, dictionaries, Task-wrapped types.

**Upstream template examples to port**:

| Template | Source | What it validates |
|----------|--------|-------------------|
| `WebApiController.tstemplate` | `origin/src/Tests/Render/WebApiControllerTests/WebApiController.tstemplate` | Nested `$TypeArguments[$TypeArguments[$Name]]` — deep generic unwrapping (`Task<IHttpActionResult<ComplexClassModel[]>>` → `ComplexClassModel[]`) |
| `BooksController.tstemplate` | `origin/src/Tests/Render/RoutedApiController/BooksController.tstemplate` | Complex code-block logic with regex, LINQ, conditional routing, `$Parameters([FromBody])`, route prefix interpolation |

**Upstream input types to port**:
- `ClassInfo.cs` — inheritance (`BaseClassInfo`), generics (`GenericClassInfo<T>`), nested types
- `PropertyInfo.cs` — all 30+ property types (primitives, DateTime, Guid, nullable, enums, collections, dictionaries, generic properties)
- `MethodInfo.cs` — generic methods, Task<T> returns, nullable Task, byte[] params, default values
- `DelegateInfo.cs` — generic delegates, Task-wrapped returns
- `IInterfaceInfo.cs` — interface inheritance, generics, type arguments
- `PartialClassInfo.cs` + `PartialClassInfo2.cs` — partial class merging
- `EnumInfo.cs` — Flags, hex values, byte underlying type
- `AttributeInfoAttribute.cs` + `AttributeTestClass.cs` — attribute arguments (string, int, named, params, Type)
- `TypeInfo.cs` — type resolution across class/interface/collection references

**Expected output pattern**: TypeScript representations that correctly handle generic unwrapping, nullable types (`T | null`), collection flattening, enum values, partial class merge (all members from both files appear).

**Golden files**: `WebApiController.result`, `BooksController.result` (for the rendering subset)

**Parity classification**:
| Feature | Parity tag |
|---------|-----------|
| Generic type arguments / `$TypeArguments` | identical |
| Nullable type handling | identical |
| Partial class merging | identical |
| Attribute value extraction | identical |
| Enum value extraction (int, char, hex, flags) | identical |
| Dictionary type mapping | identical |
| Task<T> unwrapping | identical |
| Nested type iteration | identical |
| `$Parent as Class` casting | identical |
| `IsPrimitive` / `IsEnumerable` / `IsDate` | identical |
| Delegate/event metadata | identical |

---

## Summary: Upstream assets to port per fixture set

| Fixture set | Templates to port | Input types to port | Golden files | New vs ported |
|-------------|-------------------|-----------------------|--------------|---------------|
| `simple` | 2 (Template.tst, Models.tst) | 3 (PropertyInfo, ClassInfo, EnumInfo) | New (derive from upstream test assertions) | Mostly ported |
| `multi-project` | 2 (SingleFile.tstemplate, WebApiController.tst) | 5 (Model1-3, WebApiController, ComplexClassModel) | 2 (SingleFile.result, WebApiController.result) | Ported |
| `multi-target` | 2 (BooksController.tstemplate, WebApiController.tstemplate) | 4 (BooksController, Book, WebApiController, ComplexClassModel) | 2 (BooksController.result, WebApiController.result) | Ported + new multi-target csproj |
| `source-generators` | 2 (adapted Template.tst, adapted Models.tst) | Existing M5 fixture + extensions | New (derive from generator output) | Mostly new |
| `complex-types` | 2 (WebApiController.tstemplate, BooksController.tstemplate) | 9+ (ClassInfo, PropertyInfo, MethodInfo, DelegateInfo, IInterfaceInfo, PartialClassInfo, EnumInfo, AttributeInfo, TypeInfo) | 2 (WebApiController.result, BooksController.result) + new | Mostly ported |

## ParityMatrix.md tags

The following features should be tagged in `ParityMatrix.md`:

### `identical` (behavior-preserving port)
- Template code-block parsing (`${ ... }`)
- Class/Enum/Interface iteration (`$Classes`, `$Enums`, `$Interfaces`)
- Filter syntax: wildcard (`*Model`), attribute (`[Attr]`), base class (`:Base`)
- Lambda filter syntax (`$Classes(c => ...)`)
- Property/Method/Parameter iteration
- Separator syntax (`$Properties[...][, ]`)
- Type mapping (C# → TypeScript primitives)
- `$Default` values
- `$IsPrimitive` / `$IsDate` / `$IsEnumerable` / `$IsEnum` conditionals
- `$TypeArguments` / `$TypeParameters` (generic resolution)
- Nullable type handling
- Partial class merging
- Attribute value extraction
- Enum value extraction (int, char, hex, Flags)
- Dictionary/IDictionary mapping
- Task\<T\> unwrapping
- Nested type iteration
- Custom extension methods (defined in code block)
- `using Typewriter.Extensions.*` imports
- `settings.SingleFileMode(filename)`
- WebApi extensions: `$Route`, `$Verb`, `$Parameters([FromBody])`
- `$Parent as Class` casting
- `#reference` directive
- `.result` golden file comparison pattern

### `transformed` (behavior changed with documented rationale)
- `IncludeProject(name)` → path-qualified selector with TW12xx ambiguity policy
- `$rootnamespace$` → deferred/transformed (VS-specific token, no project context in CLI)
- Multi-target TFM selection → first-TFM default + `--framework` override (upstream has no multi-target)
- Source-generator type visibility → new capability (upstream doesn't support)
- `requestRender` partial combined → deterministic batch queue (see M5, #135)
- Template assembly loading → `TemplateAssemblyLoadContext` (replaces `Assembly.LoadFrom`)

### `deferred` (not in v1 scope)
- VS project mutation (adding generated files to .csproj)
- VS source control integration
- VS ErrorList / Output window diagnostics
- VS registry-coupled settings
- Watch/live mode (deferred to M9/post-v1)
- `$rootnamespace$` token resolution (requires VS project system context)

## Follow-ups

- Create actual fixture directories under `tests/fixtures/` for each of the 5 sets (subsequent M7 tasks)
- Create `tests/baselines/` with `.result` golden files
- Create `tests/Typewriter.GoldenTests/` project with golden comparison test runner
- Create `ParityMatrix.md` with the tags documented above
