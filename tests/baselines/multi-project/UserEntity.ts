
// Auto-generated from multi-project solution
// This template traverses cross-project references:
//   - DomainLib defines IEntity, EntityBase, Address
//   - ApiLib defines UserEntity and OrderEntity (extend EntityBase, use Address)


// ApiLib.UserEntity
export class UserEntity {
    // Base: EntityBase
    // Implements: 
    
    name: string;
    email: string;
    homeAddress: Address;
}


