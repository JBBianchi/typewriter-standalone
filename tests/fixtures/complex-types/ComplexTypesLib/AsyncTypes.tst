${
    using Typewriter.Extensions.Types;

    string Awaited(Method m)
    {
        var type = m.Type;
        if (type.IsTask)
        {
            return type.IsGeneric
                ? type.TypeArguments.First().Name
                : "void";
        }
        return type.Name;
    }
}
// Auto-generated async service types
// Exercises: Task<T>, Task<(string, int)>, Task<int?>, default params

$Classes(AsyncService)[
// $FullName
export class $Name {
    $Methods[
    // Returns: $Type → Awaited: $Awaited
    $name($Parameters[$name: $Type][, ]): Promise<$Awaited>;]
}
]
$Interfaces(*Service)[
// Interface: $Name
export interface $Name {
    $Methods[
    $name($Parameters[$name: $Type][, ]): Promise<$Type>;]
    $Properties[
    $name: $Type;]
}
]
$Enums(*)[
export enum $Name {
    $Values[
    $Name = $Value,]
}
]
