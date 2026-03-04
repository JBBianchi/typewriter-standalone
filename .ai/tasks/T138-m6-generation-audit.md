# T138: Audit Generation/ files for VS coupling (M6)
- Milestone: M6
- Status: Done
- Agent: Executor (#138)
- Started: 2026-03-04
- Completed: 2026-03-04

## Objective

Audit all six source files in `origin/src/Typewriter/Generation/` for VS-specific coupling. Produce a concrete list of APIs/types to remove or replace in each file before porting.

## Approach

Read each file, catalogue every VS-coupled `using` directive, every VS-coupled API call site, and every transitive VS dependency through helper types (`ErrorList`, `Log`, `PathResolver`, `ProjectHelpers`, `ExtensionPackage`, `SettingsImpl`). Recommend a replacement or removal action for each.

## Per-File Coupling Inventory

---

### 1. `TemplateCodeParser.cs`

**VS-coupled using directives:**
| Directive | Coupling |
|-----------|----------|
| `using EnvDTE;` | `ProjectItem` type used as parameter |
| `using Typewriter.VisualStudio;` | `Log`, `PathResolver` helpers |

**VS-coupled API call sites:**

| Line(s) | API | Usage |
|---------|-----|-------|
| 19 | `ProjectItem templateProjectItem` (parameter) | Passed through to `ParseReference` and `Compiler.Compile` |
| 56 | `Compiler.Compile(templateProjectItem, shadowClass)` | Forwards `ProjectItem` to `Compiler` |
| 212–213 | `ParseReference(stream, shadowClass, templateProjectItem)` | Forwards `ProjectItem` to reference resolver |
| 228 | `PathResolver.ResolveRelative(reference, templateProjectItem)` | Resolves DLL path relative to VS project item |
| 234 | `Log.Error(...)` | Static VS output-pane logger |

**Recommended replacements:**

| Item | Action |
|------|--------|
| `ProjectItem` parameter | Replace with `string templatePath` (absolute path to `.tst` file). All downstream uses only need the file path for relative-path resolution. |
| `PathResolver.ResolveRelative` | Replace with a pure `Path.GetFullPath(Path.Combine(templateDir, reference))` utility. No VS threading needed. |
| `Log.Error` | Replace with `IDiagnosticReporter` injection (already exists in CLI). |
| `Compiler.Compile(projectItem, ...)` | Will be decoupled when `Compiler.cs` is ported (see below). |

---

### 2. `Compiler.cs`

**VS-coupled using directives:**
| Directive | Coupling |
|-----------|----------|
| `using EnvDTE;` | `ProjectItem` type used as parameter |
| `using Typewriter.VisualStudio;` | `Log`, `ErrorList`, `Constants` helpers |

**VS-coupled API call sites:**

| Line(s) | API | Usage |
|---------|-----|-------|
| 14 | `ProjectItem projectItem` (parameter) | Passed to `ErrorList.AddError`/`AddWarning` |
| 16 | `Constants.TempDirectory` | VS extension temp dir for compiled assemblies |
| 60 | `ErrorList.Clear()` | Clears VS Error List pane |
| 78 | `ErrorList.AddError(projectItem, message)` | Adds error to VS Error List |
| 83 | `ErrorList.AddWarning(projectItem, message)` | Adds warning to VS Error List |
| 89 | `ErrorList.Show()` | Shows VS Error List pane |
| 74 | `Log.Warn(...)` | VS output-pane logger |
| 51 | `Log.Warn(...)` | VS output-pane logger |
| 94 | `Assembly.LoadFrom(path)` | Loads compiled template assembly from temp path |

**Recommended replacements:**

| Item | Action |
|------|--------|
| `ProjectItem` parameter | Remove entirely. Only used to pass context to `ErrorList`. Replace with `string templatePath` for error context. |
| `ErrorList.*` calls | Replace with `IDiagnosticReporter.Report(...)` emitting TW-coded diagnostics. |
| `Log.Warn` | Replace with `IDiagnosticReporter`. |
| `Constants.TempDirectory` | Replace with a configurable temp directory (e.g., `Path.Combine(Path.GetTempPath(), "typewriter-cli")`). |
| `Assembly.LoadFrom(path)` | **Critical coupling**: loads compiled template DLL via filesystem. Replace with `AssemblyLoadContext.Default.LoadFromAssemblyPath(path)` (collectible context preferred for CLI to avoid assembly leaks). |

---

### 3. `Template.cs`

**VS-coupled using directives:**
| Directive | Coupling |
|-----------|----------|
| `using EnvDTE;` | `ProjectItem` type — stored as field, used extensively |
| `using Microsoft.Win32;` | `Registry.GetValue` for long-path check |
| `using Typewriter.Generation.Controllers;` | VS extension controller layer |
| `using Typewriter.VisualStudio;` | `Log`, `ExtensionPackage`, `ProjectHelpers` |

**VS-coupled API call sites:**

| Line(s) | API | Usage |
|---------|-----|-------|
| 24, 32 | `ProjectItem _projectItem` (field + ctor param) | Core identity of template in VS project model |
| 37 | `projectItem.Path()` | Extension method getting file path from ProjectItem |
| 38 | `projectItem.ContainingProject.FullName` | Gets .csproj path from DTE |
| 53 | `new SettingsImpl(Log.Instance, _projectItem, ...)` | VS-coupled `SettingsImpl` ctor |
| 81 | `TemplateCodeParser.Parse(_projectItem, ...)` | Forwards ProjectItem |
| 96 | `_projectItem.DTE.Solution.AllProjects()` | Enumerates all VS solution projects |
| 97 | `m.AllProjectItems(Constants.CsExtension)` | Enumerates all project items (VS DTE) |
| 102 | `ProjectHelpers.ProjectListContainsItem(_projectItem.DTE, ...)` | VS DTE solution query |
| 109 | `Parser.Parse(_projectItem, ...)` | Forwards ProjectItem |
| 123 | `SingleFileParser.Parse(_projectItem, ...)` | Forwards ProjectItem |
| 200 | `ProjectItem item` (local) | Used for output file management in VS project |
| 212 | `ExtensionPackage.Instance.AddGeneratedFilesToProject` | VS extension package singleton |
| 225 | `CheckOutFileFromSourceControl(outputPath)` | VS source control integration |
| 233 | `_projectItem.ProjectItems.AddFromFile(outputPath)` | Adds generated file to VS project |
| 269 | `item.Name = Path.GetFileName(newOutputPath)` | Renames ProjectItem in VS project |
| 274–289 | `GetMappedSourceFile(item)` | Reads `CustomToolNamespace` property from ProjectItem |
| 292–330 | `SetMappedSourceFile(item, path)` | Sets `CustomToolNamespace` property on ProjectItem |
| 332–350 | `GetExistingItem(path)` | Iterates `_projectItem.ProjectItems` collection |
| 474–493 | `FindProjectItem(path)` | Iterates `_projectItem.ProjectItems` collection |
| 495–513 | `CheckOutFileFromSourceControl(path)` | `dte.SourceControl.IsItemUnderSCC/CheckOutItem` |
| 515–519 | `VerifyProjectItem()` | `_projectItem.FileNames[1]` — DTE COM interop |
| 521–529 | `SaveProjectFile()` | `_projectItem.ContainingProject.Save()` |
| 555–573 | `IsLongPathEnabled()` | `Registry.GetValue(...)` — Windows-only |

**Recommended replacements:**

| Item | Action |
|------|--------|
| `ProjectItem` field/param | Replace with a plain data record: `TemplatePath` (string), `ProjectPath` (string), and injected services for file operations. |
| `projectItem.Path()` | Already have the path string. |
| `projectItem.ContainingProject.FullName` | Passed in from `ProjectLoadPlan`. |
| `projectItem.DTE.Solution.AllProjects()` | Replace with data from `WorkspaceLoadResult` (already available from M5). |
| `ExtensionPackage.Instance.AddGeneratedFilesToProject` | **Remove entirely** — CLI does not mutate `.csproj` files (see Q3: default no). |
| `CheckOutFileFromSourceControl` | **Remove entirely** — no VS source control integration in CLI. |
| `_projectItem.ProjectItems.AddFromFile(...)` | **Remove entirely** — CLI writes files to disk only. |
| `GetMappedSourceFile` / `SetMappedSourceFile` | **Remove entirely** — `CustomToolNamespace` is a VS project property. Replacement: track source→output mapping in memory or a sidecar file if needed. |
| `VerifyProjectItem()` / `SaveProjectFile()` | **Remove entirely** — COM interop only. |
| `SettingsImpl(Log.Instance, _projectItem, ...)` | Replace with CLI-side `SettingsImpl` (already ported in M1, T007) that takes `IDiagnosticReporter` + paths. |
| `Registry.GetValue(...)` for long paths | **Remove** — .NET 10 handles long paths natively. |
| `Log.*` calls | Replace with `IDiagnosticReporter`. |
| `ProjectHelpers.ProjectListContainsItem` | Replace with path-based filtering against `WorkspaceLoadResult` project list. |

---

### 4. `Parser.cs`

**VS-coupled using directives:**
| Directive | Coupling |
|-----------|----------|
| `using EnvDTE;` | `ProjectItem` type used as parameter throughout |
| `using Typewriter.VisualStudio;` | `Log`, `ErrorList` helpers |

**VS-coupled API call sites:**

| Line(s) | API | Usage |
|---------|-----|-------|
| 16 | `ProjectItem projectItem` (parameter) | Threaded through all parse methods |
| 34 | `ParseTemplate(projectItem, ...)` | Forwards ProjectItem |
| 46 | `ParseDollar(projectItem, ...)` | Forwards ProjectItem |
| 63 | `TryGetIdentifier(projectItem, ...)` | Forwards ProjectItem |
| 109 | `LogException(e, message, projectItem, sourcePath)` | Forwards ProjectItem to error logging |
| 176 | `TryGetIdentifier(projectItem, ...)` | Forwards ProjectItem |
| 214–228 | `LogException(...)` | `ErrorList.AddError(projectItem, ...)` and `ErrorList.Show()` |
| 225 | `Log.Error(logMessage)` | VS output-pane logger |

**Recommended replacements:**

| Item | Action |
|------|--------|
| `ProjectItem` parameter | Replace with `string templatePath` throughout the call chain. Only used for error context (passed to `ErrorList`/`Log`). |
| `ErrorList.AddError(projectItem, ...)` | Replace with `IDiagnosticReporter.Report(...)` with TW-coded template error diagnostics. |
| `ErrorList.Show()` | **Remove** — CLI writes diagnostics to stderr/stdout. |
| `Log.Error(...)` | Replace with `IDiagnosticReporter`. |

---

### 5. `SingleFileParser.cs`

**VS-coupled using directives:**
| Directive | Coupling |
|-----------|----------|
| `using EnvDTE;` | `ProjectItem` type used as parameter throughout |
| `using Typewriter.VisualStudio;` | `Log`, `ErrorList` helpers |

**VS-coupled API call sites:**

| Line(s) | API | Usage |
|---------|-----|-------|
| 16 | `ProjectItem projectItem` (parameter) | Threaded through all parse/filter methods |
| 19 | `ParseTemplate(projectItem, ...)` | Forwards ProjectItem |
| 46 | `ParseDollar(projectItem, ...)` | Forwards ProjectItem |
| 76, 110 | `TryGetIdentifier(projectItem, ...)` | Forwards ProjectItem |
| 99, 114 | `ApplyFilter(collection, filter, projectItem, ...)` | Forwards ProjectItem |
| 187 | `LogException(e, message, projectItem, sourcePath)` | Forwards ProjectItem to error logging |
| 264–278 | `LogException(...)` | `ErrorList.AddError(projectItem, ...)` and `ErrorList.Show()` |
| 275 | `Log.Error(logMessage)` | VS output-pane logger |
| 280 | `ParseTemplate(projectItem, ...)` (overload) | Forwards ProjectItem |
| 303 | `ParseDollar(projectItem, ...)` (overload) | Forwards ProjectItem |
| 309 | `TryGetIdentifier(projectItem, ...)` | Forwards ProjectItem |
| 355 | `LogException(e, message, projectItem, sourcePath)` | Forwards ProjectItem |

**Recommended replacements:**

| Item | Action |
|------|--------|
| `ProjectItem` parameter | Replace with `string templatePath`. Identical pattern to `Parser.cs` — only used for error context. |
| `ErrorList.AddError(projectItem, ...)` | Replace with `IDiagnosticReporter.Report(...)`. |
| `ErrorList.Show()` | **Remove** — CLI diagnostics go to stderr. |
| `Log.Error(...)` | Replace with `IDiagnosticReporter`. |

---

### 6. `ItemFilter.cs`

**VS-coupled using directives:** NONE

**VS-coupled API call sites:** NONE

**Assessment:** **Clean** — no VS coupling. Uses only `System`, `System.Collections.Generic`, `System.Linq`, and `Typewriter.CodeModel`. Can be lifted as-is into `src/Typewriter.Generation/`.

---

## Summary: Transitive VS Dependencies

The Generation files depend on these VS-coupled helper types:

| Helper Type | Location | VS APIs Used | Action |
|-------------|----------|-------------|--------|
| `ErrorList` | `VisualStudio/ErrorList.cs` | `IVsErrorList`, `ErrorListProvider`, `ErrorTask`, `Package.GetGlobalService` | **Replace** with `IDiagnosticReporter` (already exists) |
| `Log` | `VisualStudio/Log.cs` | `DTE`, `ThreadHelper`, VS output window | **Replace** with `IDiagnosticReporter` (already exists) |
| `PathResolver` | `VisualStudio/PathResolver.cs` | `ProjectItem`, `ThreadHelper`, DTE Solution | **Replace** with `Path.GetFullPath(Path.Combine(...))` |
| `ProjectHelpers` | `CodeModel/Configuration/ProjectHelpers.cs` | `DTE`, `ThreadHelper`, `VSProject`, `Reference` | **Replace** with `WorkspaceLoadResult`-based queries |
| `ExtensionPackage` | `VisualStudio/ExtensionPackage.cs` | `AsyncPackage`, `IVsStatusbar`, DTE events | **Remove** — CLI has no VS package singleton |
| `SettingsImpl` | `CodeModel/Configuration/SettingsImpl.cs` | `ProjectItem`, `ThreadHelper` | **Already ported** (M1, T007) — CLI version takes `IDiagnosticReporter` + paths |
| `Constants` | `Constants.cs` | None | **Lift as-is** or inline needed constants |

## Cross-Cutting Patterns

1. **`ProjectItem` threading**: Every file except `ItemFilter.cs` passes `EnvDTE.ProjectItem` through its API. This is the single biggest coupling point. Replace uniformly with `string templatePath` + injected services.

2. **`ErrorList` + `Log` pairing**: `Parser.cs`, `SingleFileParser.cs`, and `Compiler.cs` all use `ErrorList.AddError/AddWarning/Show` + `Log.Error/Warn` together. Replace both with a single `IDiagnosticReporter` call per diagnostic.

3. **`Assembly.LoadFrom`**: Only in `Compiler.cs` (line 94). Replace with `AssemblyLoadContext` for proper isolation in CLI.

4. **`Registry.GetValue`**: Only in `Template.cs` (line 561). Remove — .NET 10 supports long paths natively on all platforms.

5. **VS project mutation** (`AddFromFile`, `Save`, `CheckOutItem`, `CustomToolNamespace`): Only in `Template.cs`. Remove entirely — CLI writes files to disk without modifying `.csproj`.

## Outcome

All six files audited. Five of six contain VS coupling; `ItemFilter.cs` is clean. The dominant pattern is `EnvDTE.ProjectItem` threading — replacing this with path strings and injected `IDiagnosticReporter` will decouple >80% of the VS surface area. The remaining ~20% is VS project mutation in `Template.cs` (remove entirely) and `Assembly.LoadFrom` in `Compiler.cs` (replace with `AssemblyLoadContext`).

## Follow-ups

- Port `ItemFilter.cs` as-is into `src/Typewriter.Generation/` (no changes needed)
- Port `Parser.cs` and `SingleFileParser.cs` with `ProjectItem` → `string templatePath` + `IDiagnosticReporter`
- Port `TemplateCodeParser.cs` with `ProjectItem` → `string templatePath` + path-based reference resolution
- Port `Compiler.cs` with `ErrorList` → `IDiagnosticReporter` + `AssemblyLoadContext`
- Port `Template.cs` with full rewrite of file management (remove VS project mutation, source control, registry)
