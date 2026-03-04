${
    using Typewriter.Extensions.Types;

    string NullableIndicator(Property p)
    {
        return p.Type.IsNullable ? " | null" : "";
    }

    string GenericConstraints(Class c)
    {
        return c.TypeParameters.Any()
            ? "<" + string.Join(", ", c.TypeParameters.Select(t => t.Name)) + ">"
            : "";
    }
}
// Auto-generated complex type models
// Exercises: nullable types, generics, partial class merging

$Classes(Nullable*)[
// Nullable annotations — $FullName
export interface I$Name {
    $Properties[
    $name: $Type$NullableIndicator;]
}
]
$Classes(Generic*)[
// Generic type — $Name$GenericConstraints
export class $Name$GenericConstraints {
    $Properties[
    $name: $Type;]
    $Methods[
    $name($Parameters[$name: $Type][, ]): $Type;]
}
]
$Classes(PartialEntity)[
// Partial class (merged from 2 files) — $Name
export class $Name {
    $Properties[
    $name: $Type;]
    $Methods[
    $name($Parameters[$name: $Type][, ]): $Type;]
}
]
