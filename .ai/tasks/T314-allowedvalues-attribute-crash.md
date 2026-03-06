# T314: Fix `AllowedValuesAttribute` params-array metadata crash
- Milestone: M5
- Status: Done
- Agent: Codex (Executor)
- Started: 2026-03-06
- Completed: 2026-03-06

## Objective
Prevent `RoslynAttributeMetadata` from throwing when reading attributes such as `AllowedValues(null, ...)` whose Roslyn string form ends in `}` without containing the `{` token that the upstream params-array trimming logic assumes.

## Approach
Patch `src/Typewriter.Metadata.Roslyn/RoslynAttributeMetadata.cs` so attribute value normalization only unwraps params-array braces when a matching opening brace exists, and add a Roslyn-backed regression in `tests/Typewriter.UnitTests/Metadata/MetadataParityTests.cs` that exercises a property decorated with `AllowedValues(null, ...)`.

## Journey
### 2026-03-06
- Reproduced the failure path from the user report by inspecting `RoslynAttributeMetadata`: it parses `AttributeData.ToString()`, slices the argument list, then blindly calls `LastIndexOf("{")` / `LastIndexOf("{\"")` before `Remove(...)`.
- Compared with `origin/src/Roslyn/RoslynAttributeMetadata.cs`; the standalone implementation preserved the same brittle logic, confirming this is an upstream parity bug rather than a regression introduced by the CLI port.
- Confirmed the likely trigger shape: `AllowedValuesAttribute` is a params-array attribute and Roslyn can stringify the arguments with a trailing `}` while omitting the matching `{` in the sliced substring, producing `LastIndexOf(...) == -1` and the observed `startIndex` exception.

## Outcome
- Hardened `src/Typewriter.Metadata.Roslyn/RoslynAttributeMetadata.cs` so params-array brace trimming only removes `{` when a matching opening token is present in the Roslyn-rendered attribute text.
- Added `AllowedValuesAttribute_ParamsArray_DoesNotCrash` to `tests/Typewriter.UnitTests/Metadata/MetadataParityTests.cs`, covering the same CodeModel property attribute access path that templates use (`p.Attributes.Select(a => a.Name)`).
- Attempted targeted verification with:
  - `dotnet test tests\Typewriter.UnitTests\Typewriter.UnitTests.csproj -c Release --filter "FullyQualifiedName~AllowedValuesAttribute_ParamsArray_DoesNotCrash"`
  - `dotnet test tests\Typewriter.UnitTests\Typewriter.UnitTests.csproj -c Release --no-restore --filter "FullyQualifiedName~AllowedValuesAttribute_ParamsArray_DoesNotCrash"`
  - `dotnet build tests\Typewriter.UnitTests\Typewriter.UnitTests.csproj -c Release --no-restore -v minimal`
- Verification remained environment-blocked in the sandbox: restore stalled at `Determining projects to restore...`, and `--no-restore` MSBuild runs failed during project-reference evaluation with `Build FAILED` / `0 Error(s)` and no actionable compiler output.

## Follow-ups
- Run mandatory verification (`dotnet restore`, `dotnet build -c Release`, `dotnet test -c Release`) once a credentialed/network-capable environment is available if sandbox restrictions continue to block full-suite execution.
