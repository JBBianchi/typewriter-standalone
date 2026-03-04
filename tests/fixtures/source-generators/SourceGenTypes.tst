${
    using Typewriter.Extensions.Types;

    // Filter: include all public classes in the SourceGenLib namespace.
    // This should capture both the handwritten Class1 and the
    // source-generator-produced GeneratedHelper.
    bool IsSourceGenLib(Class c) => c.Namespace == "SourceGenLib";
}
// Auto-generated from source-generators fixture
// Validates that source-generator-produced types are visible in templates.
//
// PARITY-GAP: Source-generator type visibility in the template metadata pipeline
// is a new capability (upstream Typewriter has no source-generator support).
// If GeneratedHelper does not appear below, the Roslyn workspace may not be
// running generators during metadata extraction. See Q4 in progress.md.

$Classes($IsSourceGenLib)[
export class $Name {
    $Properties[
    $name: $Type;]
    $Fields[
    static readonly $name: $Type = $Default;]
}
]
