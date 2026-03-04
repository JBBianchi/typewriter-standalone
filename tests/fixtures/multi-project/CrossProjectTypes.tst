${
    using Typewriter.Extensions.Types;

    string BaseClassName(Class c)
    {
        return c.BaseClass != null ? c.BaseClass.Name : "none";
    }

    string ImplementedInterfaces(Class c)
    {
        return string.Join(", ", c.Interfaces.Select(i => i.Name));
    }
}
// Auto-generated from multi-project solution
// This template traverses cross-project references:
//   - DomainLib defines IEntity, EntityBase, Address
//   - ApiLib defines UserEntity and OrderEntity (extend EntityBase, use Address)

$Classes(*Entity)[
// $FullName
export class $Name {
    // Base: $BaseClassName
    // Implements: $ImplementedInterfaces
    $Properties[
    $name: $Type;]
}
]
$Interfaces(IEntity)[
export interface $Name {
    $Properties[
    $name: $Type;]
}
]
