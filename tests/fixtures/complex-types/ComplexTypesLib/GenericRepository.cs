namespace ComplexTypesLib;

/// <summary>
/// Demonstrates generic type parameters with constraints.
/// </summary>
public class GenericRepository<T> where T : class, new()
{
    /// <summary>
    /// Property using the generic type.
    /// </summary>
    public List<T> Items { get; set; } = [];

    /// <summary>
    /// Returns the generic type.
    /// </summary>
    public T? FindById(int id)
    {
        return default;
    }

    /// <summary>
    /// Returns Task wrapping the generic type.
    /// </summary>
    public Task<T?> FindByIdAsync(int id)
    {
        return Task.FromResult<T?>(default);
    }

    /// <summary>
    /// Returns a collection of the generic type.
    /// </summary>
    public Task<IEnumerable<T>> GetAllAsync()
    {
        return Task.FromResult<IEnumerable<T>>([]);
    }

    /// <summary>
    /// Generic method with its own type parameter and constraint.
    /// </summary>
    public TResult Transform<TResult>(T source) where TResult : new()
    {
        return new TResult();
    }

    /// <summary>
    /// Method with Dictionary using generic type.
    /// </summary>
    public Dictionary<string, T> ToDictionary()
    {
        return new Dictionary<string, T>();
    }
}

/// <summary>
/// A concrete class inheriting from a generic base.
/// </summary>
public class NullableTypesRepository : GenericRepository<NullableTypes>
{
}

/// <summary>
/// Generic interface with constraint.
/// </summary>
public interface IGenericService<T> where T : class
{
    /// <summary>
    /// Async method returning the generic type.
    /// </summary>
    Task<T> GetAsync(int id);

    /// <summary>
    /// Property of the generic type.
    /// </summary>
    T? Current { get; }
}

/// <summary>
/// Interface inheriting from a closed generic interface.
/// </summary>
public interface INullableTypesService : IGenericService<NullableTypes>
{
}
