namespace ComplexTypesLib;

/// <summary>
/// Demonstrates Task&lt;T&gt; and tuple return types in async methods.
/// </summary>
public class AsyncService
{
    /// <summary>
    /// Returns a plain Task (void async).
    /// </summary>
    public Task ProcessAsync()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Returns Task&lt;string&gt;.
    /// </summary>
    public Task<string> GetNameAsync()
    {
        return Task.FromResult("name");
    }

    /// <summary>
    /// Returns Task&lt;int?&gt; (nullable value inside Task).
    /// </summary>
    public Task<int?> GetCountAsync()
    {
        return Task.FromResult<int?>(null);
    }

    /// <summary>
    /// Returns Task with a named tuple result.
    /// </summary>
    public Task<(string A, int B)> GetTupleAsync()
    {
        return Task.FromResult(("hello", 42));
    }

    /// <summary>
    /// Returns Task&lt;List&lt;string&gt;&gt;.
    /// </summary>
    public Task<List<string>> GetNamesAsync()
    {
        return Task.FromResult(new List<string>());
    }

    /// <summary>
    /// Returns Task with a nullable reference type.
    /// </summary>
    public Task<string?> FindByIdAsync(int id)
    {
        return Task.FromResult<string?>(null);
    }

    /// <summary>
    /// Method with byte array parameter (upstream parity).
    /// </summary>
    public void Upload(byte[] data)
    {
    }

    /// <summary>
    /// Method with default parameter values.
    /// </summary>
    public Task<string> SearchAsync(string query = "default", int limit = 10, bool includeDeleted = false)
    {
        return Task.FromResult(query);
    }
}
