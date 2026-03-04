
export interface IUserModel {
    
    firstName: string;
    lastName: string;
    email: string;
    age: number;
    isActive: boolean;
    role: UserRole;
    tags: string[];
    lastLoginAt: Date | null;
}
