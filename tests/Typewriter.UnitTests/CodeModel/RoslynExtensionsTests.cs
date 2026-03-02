using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Typewriter.Metadata.Roslyn;
using Xunit;

namespace Typewriter.UnitTests.CodeModel;

/// <summary>
/// Tests for <see cref="Extensions"/> Roslyn symbol helper methods:
/// <see cref="Extensions.GetName"/>, <see cref="Extensions.GetFullName"/>,
/// <see cref="Extensions.GetNamespace"/>, <see cref="Extensions.GetFullTypeName"/>.
/// </summary>
public class RoslynExtensionsTests
{
    // Lazily build a single compilation shared across tests in this class.
    private static readonly Lazy<CSharpCompilation> LazyCompilation = new(CreateCompilation);

    private static CSharpCompilation Compilation => LazyCompilation.Value;

    private static CSharpCompilation CreateCompilation()
    {
        const string source = """
            using System;
            using System.Collections.Generic;

            namespace Outer.Inner
            {
                public class SimpleClass { }
                public class GenericClass<T> { }
                public class NullableHost
                {
                    public int? NullableInt { get; set; }
                }
            }

            namespace Top
            {
                public class TopClass { }
            }
            """;

        var tree = CSharpSyntaxTree.ParseText(source);

        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(IEnumerable<>).Assembly.Location),
        };

        return CSharpCompilation.Create(
            assemblyName: "TestAssembly",
            syntaxTrees: new[] { tree },
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    // -------------------------------------------------------------------------
    // GetName
    // -------------------------------------------------------------------------

    [Fact]
    public void GetName_NamedType_ReturnsSimpleName()
    {
        var symbol = Compilation.GetTypeByMetadataName("Outer.Inner.SimpleClass")!;
        Assert.Equal("SimpleClass", symbol.GetName());
    }

    [Fact]
    public void GetName_GenericType_ReturnsSimpleName()
    {
        // GetName should return just the identifier without type params
        var symbol = Compilation.GetTypeByMetadataName("Outer.Inner.GenericClass`1")!;
        Assert.Equal("GenericClass", symbol.GetName());
    }

    [Fact]
    public void GetName_ArrayType_ReturnsElementTypeNameWithBrackets()
    {
        // Build an array type symbol via a method return type
        const string source = "class Host { public int[] Method() => null!; }";
        var tree = CSharpSyntaxTree.ParseText(source);
        var comp = CSharpCompilation.Create(
            "TestArrayAssembly",
            new[] { tree },
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var hostType = comp.GetTypeByMetadataName("Host")!;
        var method = hostType.GetMembers("Method").OfType<IMethodSymbol>().First();
        var arraySymbol = (IArrayTypeSymbol)method.ReturnType;

        // GetName for array uses $"{array.ElementType}[]"
        // Roslyn's ToString() for int (System.Int32) returns the C# keyword "int"
        var result = arraySymbol.GetName();
        Assert.EndsWith("[]", result);
        Assert.True(result.Length > 2, "Expected non-empty element type name before '[]'");
    }

    // -------------------------------------------------------------------------
    // GetFullName
    // -------------------------------------------------------------------------

    [Fact]
    public void GetFullName_SimpleNamespacedType_ReturnsFullyQualifiedName()
    {
        var symbol = Compilation.GetTypeByMetadataName("Outer.Inner.SimpleClass")!;
        Assert.Equal("Outer.Inner.SimpleClass", symbol.GetFullName());
    }

    [Fact]
    public void GetFullName_SingleLevelNamespace_ReturnsFullyQualifiedName()
    {
        var symbol = Compilation.GetTypeByMetadataName("Top.TopClass")!;
        Assert.Equal("Top.TopClass", symbol.GetFullName());
    }

    [Fact]
    public void GetFullName_GenericType_IncludesTypeParameterInName()
    {
        var symbol = Compilation.GetTypeByMetadataName("Outer.Inner.GenericClass`1")!;
        var fullName = symbol.GetFullName();
        // Should look like "Outer.Inner.GenericClass<T>"
        Assert.StartsWith("Outer.Inner.", fullName);
        Assert.Contains("GenericClass", fullName);
        Assert.Contains("<T>", fullName);
    }

    [Fact]
    public void GetFullName_TypeParameter_ReturnsJustParameterName()
    {
        var symbol = Compilation.GetTypeByMetadataName("Outer.Inner.GenericClass`1")!;
        var typeParam = symbol.TypeParameters.First();
        Assert.Equal("T", typeParam.GetFullName());
    }

    // -------------------------------------------------------------------------
    // GetNamespace
    // -------------------------------------------------------------------------

    [Fact]
    public void GetNamespace_NestedNamespace_ReturnsCompoundNamespace()
    {
        var symbol = Compilation.GetTypeByMetadataName("Outer.Inner.SimpleClass")!;
        Assert.Equal("Outer.Inner", symbol.GetNamespace());
    }

    [Fact]
    public void GetNamespace_SingleLevelNamespace_ReturnsSingleName()
    {
        var symbol = Compilation.GetTypeByMetadataName("Top.TopClass")!;
        Assert.Equal("Top", symbol.GetNamespace());
    }

    // -------------------------------------------------------------------------
    // GetFullTypeName
    // -------------------------------------------------------------------------

    [Fact]
    public void GetFullTypeName_NonGenericType_ReturnsSimpleName()
    {
        var symbol = Compilation.GetTypeByMetadataName("Outer.Inner.SimpleClass")!;
        Assert.Equal("SimpleClass", symbol.GetFullTypeName());
    }

    [Fact]
    public void GetFullTypeName_GenericOpenType_IncludesTypeParameterName()
    {
        var symbol = Compilation.GetTypeByMetadataName("Outer.Inner.GenericClass`1")!;
        var result = symbol.GetFullTypeName();
        Assert.Equal("GenericClass<T>", result);
    }

    [Fact]
    public void GetFullTypeName_NullableValueType_ReturnsInnerTypeNameWithQuestionMark()
    {
        // Create a compilation that has int? as a field type
        const string source = "class Host { public System.Nullable<int> Field; }";
        var tree = CSharpSyntaxTree.ParseText(source);
        var comp = CSharpCompilation.Create(
            "NullableAssembly",
            new[] { tree },
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var hostType = comp.GetTypeByMetadataName("Host")!;
        var field = hostType.GetMembers("Field").OfType<IFieldSymbol>().First();
        var nullableType = (INamedTypeSymbol)field.Type;

        var result = nullableType.GetFullTypeName();
        // System.Nullable<int> -> "Int32?" per GetFullTypeName logic
        Assert.Equal("Int32?", result);
    }

    [Fact]
    public void GetFullTypeName_ClosedGenericType_IncludesClosedTypeArgument()
    {
        // Create List<string>
        const string source = "using System.Collections.Generic; class Host { public List<string> Field; }";
        var tree = CSharpSyntaxTree.ParseText(source);
        var comp = CSharpCompilation.Create(
            "ClosedGenericAssembly",
            new[] { tree },
            new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(List<>).Assembly.Location),
            },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var hostType = comp.GetTypeByMetadataName("Host")!;
        var field = hostType.GetMembers("Field").OfType<IFieldSymbol>().First();
        var listType = (INamedTypeSymbol)field.Type;

        var result = listType.GetFullTypeName();
        // List<string> -> "List<System.String>"
        Assert.StartsWith("List<", result);
        Assert.Contains("String", result);
    }
}
