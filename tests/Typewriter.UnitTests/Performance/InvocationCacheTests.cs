using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Typewriter.Generation.Performance;
using Xunit;

namespace Typewriter.UnitTests.Performance;

public class InvocationCacheTests
{
    /// <summary>
    /// Verifies that compilations cached under different scopes do not collide,
    /// even when the underlying project path is identical (AGENTS.md §11.1).
    /// </summary>
    [Fact]
    public void GetOrAddCompilation_DifferentScopes_DoNotCollide()
    {
        // Arrange — two caches with different scopes pointing at the same project.
        var cacheA = new InvocationCache();
        cacheA.SetScope("/solutions/A.sln");

        var cacheB = new InvocationCache();
        cacheB.SetScope("/solutions/B.sln");

        var compilationA = CSharpCompilation.Create("A");
        var compilationB = CSharpCompilation.Create("B");

        const string projectPath = "/projects/Shared.csproj";

        // Act
        var resultA = cacheA.GetOrAddCompilation(projectPath, _ => compilationA);
        var resultB = cacheB.GetOrAddCompilation(projectPath, _ => compilationB);

        // Assert — each scope returns its own compilation.
        Assert.Same(compilationA, resultA);
        Assert.Same(compilationB, resultB);
    }

    /// <summary>
    /// Verifies that repeated calls with the same scope and project path
    /// return the cached compilation without invoking the factory again.
    /// </summary>
    [Fact]
    public void GetOrAddCompilation_SameScope_ReturnsCachedValue()
    {
        var cache = new InvocationCache();
        cache.SetScope("/solutions/A.sln");

        var compilation = CSharpCompilation.Create("Cached");
        var callCount = 0;

        const string projectPath = "/projects/Lib.csproj";

        var first = cache.GetOrAddCompilation(projectPath, _ =>
        {
            callCount++;
            return compilation;
        });

        var second = cache.GetOrAddCompilation(projectPath, _ =>
        {
            callCount++;
            return CSharpCompilation.Create("ShouldNotBeCreated");
        });

        Assert.Same(first, second);
        Assert.Equal(1, callCount);
    }

    /// <summary>
    /// Verifies that SetScope follows first-write-wins semantics.
    /// A second call to SetScope does not change the scope.
    /// </summary>
    [Fact]
    public void SetScope_CalledTwice_FirstWins()
    {
        var cache = new InvocationCache();
        cache.SetScope("/scope/First.sln");
        cache.SetScope("/scope/Second.sln");

        var compilation = CSharpCompilation.Create("Test");
        const string projectPath = "/projects/Lib.csproj";

        cache.GetOrAddCompilation(projectPath, _ => compilation);

        // The key should use the first scope. Verify by checking Compilations dictionary.
        var firstScopeKey = Path.GetFullPath("/scope/First.sln") + "|" + Path.GetFullPath(projectPath);
        var secondScopeKey = Path.GetFullPath("/scope/Second.sln") + "|" + Path.GetFullPath(projectPath);

        Assert.True(cache.Compilations.ContainsKey(firstScopeKey));
        Assert.False(cache.Compilations.ContainsKey(secondScopeKey));
    }

    /// <summary>
    /// Verifies that template assembly caching is unaffected by scope
    /// (templates are scope-independent).
    /// </summary>
    [Fact]
    public void GetOrAddTemplate_IgnoresScope()
    {
        var cache = new InvocationCache();
        cache.SetScope("/scope/A.sln");

        var callCount = 0;
        const string templatePath = "/templates/Test.tst";

        var first = cache.GetOrAddTemplate(templatePath, _ =>
        {
            callCount++;
            return typeof(string).Assembly;
        });

        var second = cache.GetOrAddTemplate(templatePath, _ =>
        {
            callCount++;
            return typeof(int).Assembly;
        });

        Assert.Same(first, second);
        Assert.Equal(1, callCount);
    }
}
