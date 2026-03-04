
// Auto-generated from multi-project solution
// This template traverses cross-project references:
//   - DomainLib defines IEntity, EntityBase, Address
//   - ApiLib defines UserEntity and OrderEntity (extend EntityBase, use Address)


// ApiLib.OrderEntity
export class OrderEntity {
    // Base: EntityBase
    // Implements: 
    
    productName: string;
    amount: number;
    customer: UserEntity;
}


