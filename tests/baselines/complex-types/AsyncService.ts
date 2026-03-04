
// Auto-generated async service types
// Exercises: Task<T>, Task<(string, int)>, Task<int?>, default params


// ComplexTypesLib.AsyncService
export class AsyncService {
    
    // Returns: void → Awaited: void
    processAsync(): Promise<void>;
    // Returns: string → Awaited: void
    getNameAsync(): Promise<void>;
    // Returns: number | null → Awaited: void
    getCountAsync(): Promise<void>;
    // Returns: { A: string, B: number } → Awaited: string
    getTupleAsync(): Promise<string>;
    // Returns: string[] → Awaited: string
    getNamesAsync(): Promise<string>;
    // Returns: string | null → Awaited: void
    findByIdAsync(id: number): Promise<void>;
    // Returns: void → Awaited: void
    upload(data: number[]): Promise<void>;
    // Returns: string → Awaited: void
    searchAsync(query: string, limit: number, includeDeleted: boolean): Promise<void>;
}



