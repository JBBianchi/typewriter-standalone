using Typewriter.CodeModel;
using Typewriter.CodeModel.Configuration;
using Typewriter.Metadata;
using Xunit;

namespace Typewriter.UnitTests.CodeModel;

/// <summary>
/// Tests for <see cref="Helpers"/> type-mapping logic: CamelCase, GetTypeScriptName,
/// GetOriginalName, and IsPrimitive.
/// </summary>
public class TypeMappingTests
{
    private static readonly Settings StrictSettings = new SettingsImpl("template.tst");
    private static readonly Settings LenientSettings = new SettingsImpl("template.tst").DisableStrictNullGeneration();

    // -------------------------------------------------------------------------
    // CamelCase
    // -------------------------------------------------------------------------

    [Fact]
    public void CamelCase_EmptyString_ReturnsEmpty()
        => Assert.Equal("", Helpers.CamelCase(""));

    [Fact]
    public void CamelCase_AlreadyLowercase_ReturnsUnchanged()
        => Assert.Equal("myClass", Helpers.CamelCase("myClass"));

    [Fact]
    public void CamelCase_SingleUpperChar_LowercasesIt()
        => Assert.Equal("a", Helpers.CamelCase("A"));

    [Fact]
    public void CamelCase_PascalCaseWord_LowercasesFirstLetter()
        => Assert.Equal("myClass", Helpers.CamelCase("MyClass"));

    [Fact]
    public void CamelCase_AllCapsAcronym_LowercasesAll()
        => Assert.Equal("abc", Helpers.CamelCase("ABC"));

    [Fact]
    public void CamelCase_AcronymFollowedByWord_LowercasesAcronymPart()
        => Assert.Equal("abcDef", Helpers.CamelCase("ABCDef"));

    [Fact]
    public void CamelCase_TwoCharUpperPrefix_LowercasesBoth()
        => Assert.Equal("id", Helpers.CamelCase("ID"));

    // -------------------------------------------------------------------------
    // GetTypeScriptName — null / dynamic / void / object
    // -------------------------------------------------------------------------

    [Fact]
    public void GetTypeScriptName_NullMetadata_ReturnsAny()
        => Assert.Equal("any", Helpers.GetTypeScriptName(null!, StrictSettings));

    [Fact]
    public void GetTypeScriptName_Dynamic_ReturnsAny()
    {
        var meta = new FakeTypeMetadata { Name = "dynamic", FullName = "dynamic", IsDynamic = true };
        Assert.Equal("any", Helpers.GetTypeScriptName(meta, StrictSettings));
    }

    [Fact]
    public void GetTypeScriptName_SystemVoid_ReturnsVoid()
    {
        var meta = new FakeTypeMetadata { Name = "void", FullName = "System.Void" };
        Assert.Equal("void", Helpers.GetTypeScriptName(meta, StrictSettings));
    }

    [Fact]
    public void GetTypeScriptName_SystemObject_ReturnsAny()
    {
        var meta = new FakeTypeMetadata { Name = "object", FullName = "System.Object" };
        Assert.Equal("any", Helpers.GetTypeScriptName(meta, StrictSettings));
    }

    [Fact]
    public void GetTypeScriptName_DynamicKeyword_ReturnsAny()
    {
        var meta = new FakeTypeMetadata { Name = "dynamic", FullName = "dynamic" };
        Assert.Equal("any", Helpers.GetTypeScriptName(meta, StrictSettings));
    }

    // -------------------------------------------------------------------------
    // GetTypeScriptName — primitive boolean
    // -------------------------------------------------------------------------

    [Fact]
    public void GetTypeScriptName_Bool_ReturnsBoolean()
    {
        var meta = new FakeTypeMetadata { Name = "bool", FullName = "System.Boolean" };
        Assert.Equal("boolean", Helpers.GetTypeScriptName(meta, StrictSettings));
    }

    [Fact]
    public void GetTypeScriptName_NullableBool_Strict_ReturnsBooleanOrNull()
    {
        var meta = new FakeTypeMetadata { Name = "bool?", FullName = "System.Boolean?", IsNullable = true };
        Assert.Equal("boolean | null", Helpers.GetTypeScriptName(meta, StrictSettings));
    }

    [Fact]
    public void GetTypeScriptName_NullableBool_Lenient_ReturnsBoolean()
    {
        var meta = new FakeTypeMetadata { Name = "bool?", FullName = "System.Boolean?", IsNullable = true };
        Assert.Equal("boolean", Helpers.GetTypeScriptName(meta, LenientSettings));
    }

    // -------------------------------------------------------------------------
    // GetTypeScriptName — numeric primitives
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("System.Byte", "byte")]
    [InlineData("System.SByte", "sbyte")]
    [InlineData("System.Int16", "short")]
    [InlineData("System.Int32", "int")]
    [InlineData("System.Int64", "long")]
    [InlineData("System.UInt16", "ushort")]
    [InlineData("System.UInt32", "uint")]
    [InlineData("System.UInt64", "ulong")]
    [InlineData("System.Single", "float")]
    [InlineData("System.Double", "double")]
    [InlineData("System.Decimal", "decimal")]
    public void GetTypeScriptName_NumberType_ReturnsNumber(string fullName, string csharpName)
    {
        var meta = new FakeTypeMetadata { Name = csharpName, FullName = fullName };
        Assert.Equal("number", Helpers.GetTypeScriptName(meta, StrictSettings));
    }

    [Fact]
    public void GetTypeScriptName_NullableInt_Strict_ReturnsNumberOrNull()
    {
        var meta = new FakeTypeMetadata { Name = "int?", FullName = "System.Int32?", IsNullable = true };
        Assert.Equal("number | null", Helpers.GetTypeScriptName(meta, StrictSettings));
    }

    // -------------------------------------------------------------------------
    // GetTypeScriptName — string-like primitives
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("System.String", "string")]
    [InlineData("System.Char", "char")]
    [InlineData("System.Guid", "Guid")]
    [InlineData("System.TimeSpan", "TimeSpan")]
    public void GetTypeScriptName_StringLikeType_ReturnsString(string fullName, string csharpName)
    {
        var meta = new FakeTypeMetadata { Name = csharpName, FullName = fullName };
        Assert.Equal("string", Helpers.GetTypeScriptName(meta, StrictSettings));
    }

    // -------------------------------------------------------------------------
    // GetTypeScriptName — date types
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("System.DateTime", "DateTime")]
    [InlineData("System.DateTimeOffset", "DateTimeOffset")]
    public void GetTypeScriptName_DateType_ReturnsDate(string fullName, string csharpName)
    {
        var meta = new FakeTypeMetadata { Name = csharpName, FullName = fullName };
        Assert.Equal("Date", Helpers.GetTypeScriptName(meta, StrictSettings));
    }

    [Fact]
    public void GetTypeScriptName_NullableDate_Strict_ReturnsDateOrNull()
    {
        var meta = new FakeTypeMetadata { Name = "DateTime?", FullName = "System.DateTime?", IsNullable = true };
        Assert.Equal("Date | null", Helpers.GetTypeScriptName(meta, StrictSettings));
    }

    // -------------------------------------------------------------------------
    // GetTypeScriptName — custom / unknown type
    // -------------------------------------------------------------------------

    [Fact]
    public void GetTypeScriptName_CustomType_ReturnsTypeName()
    {
        var meta = new FakeTypeMetadata { Name = "MyModel", FullName = "My.Namespace.MyModel" };
        Assert.Equal("MyModel", Helpers.GetTypeScriptName(meta, StrictSettings));
    }

    [Fact]
    public void GetTypeScriptName_NullableCustomType_Strict_ReturnsTypeNameOrNull()
    {
        var meta = new FakeTypeMetadata { Name = "MyModel?", FullName = "My.Namespace.MyModel?", IsNullable = true };
        Assert.Equal("MyModel | null", Helpers.GetTypeScriptName(meta, StrictSettings));
    }

    [Fact]
    public void GetTypeScriptName_NullableCustomType_Lenient_ReturnsTypeName()
    {
        var meta = new FakeTypeMetadata { Name = "MyModel?", FullName = "My.Namespace.MyModel?", IsNullable = true };
        Assert.Equal("MyModel", Helpers.GetTypeScriptName(meta, LenientSettings));
    }

    // -------------------------------------------------------------------------
    // GetTypeScriptName — enumerable
    // -------------------------------------------------------------------------

    [Fact]
    public void GetTypeScriptName_EnumerableWithSingleTypeArg_ReturnsArraySyntax()
    {
        var elementMeta = new FakeTypeMetadata { Name = "string", FullName = "System.String" };
        var meta = new FakeTypeMetadata
        {
            Name = "IEnumerable",
            FullName = "System.Collections.Generic.IEnumerable",
            IsEnumerable = true,
            TypeArguments = new List<ITypeMetadata> { elementMeta }
        };
        Assert.Equal("string[]", Helpers.GetTypeScriptName(meta, StrictSettings));
    }

    [Fact]
    public void GetTypeScriptName_EnumerableWithNoTypeArgs_ReturnsAnyArray()
    {
        var meta = new FakeTypeMetadata
        {
            Name = "IEnumerable",
            FullName = "System.Collections.IEnumerable",
            IsEnumerable = true,
            TypeArguments = new List<ITypeMetadata>()
        };
        Assert.Equal("any[]", Helpers.GetTypeScriptName(meta, StrictSettings));
    }

    [Fact]
    public void GetTypeScriptName_NullableEnumerable_Strict_ReturnsArrayOrNull()
    {
        var elementMeta = new FakeTypeMetadata { Name = "int", FullName = "System.Int32" };
        var meta = new FakeTypeMetadata
        {
            Name = "IEnumerable",
            FullName = "System.Collections.Generic.IEnumerable",
            IsEnumerable = true,
            IsNullable = true,
            TypeArguments = new List<ITypeMetadata> { elementMeta }
        };
        Assert.Equal("number[] | null", Helpers.GetTypeScriptName(meta, StrictSettings));
    }

    // -------------------------------------------------------------------------
    // GetTypeScriptName — dictionary
    // -------------------------------------------------------------------------

    [Fact]
    public void GetTypeScriptName_Dictionary_ReturnsRecordSyntax()
    {
        var keyMeta = new FakeTypeMetadata { Name = "string", FullName = "System.String" };
        var valueMeta = new FakeTypeMetadata { Name = "int", FullName = "System.Int32" };
        var meta = new FakeTypeMetadata
        {
            Name = "Dictionary",
            FullName = "System.Collections.Generic.Dictionary",
            IsDictionary = true,
            TypeArguments = new List<ITypeMetadata> { keyMeta, valueMeta }
        };
        Assert.Equal("Record<string, number>", Helpers.GetTypeScriptName(meta, StrictSettings));
    }

    [Fact]
    public void GetTypeScriptName_NullableDictionary_Strict_ReturnsRecordOrNull()
    {
        var keyMeta = new FakeTypeMetadata { Name = "string", FullName = "System.String" };
        var valueMeta = new FakeTypeMetadata { Name = "string", FullName = "System.String" };
        var meta = new FakeTypeMetadata
        {
            Name = "Dictionary",
            FullName = "System.Collections.Generic.Dictionary",
            IsDictionary = true,
            IsNullable = true,
            TypeArguments = new List<ITypeMetadata> { keyMeta, valueMeta }
        };
        Assert.Equal("Record<string, string> | null", Helpers.GetTypeScriptName(meta, StrictSettings));
    }

    // -------------------------------------------------------------------------
    // GetTypeScriptName — generic type
    // -------------------------------------------------------------------------

    [Fact]
    public void GetTypeScriptName_GenericType_ReturnsGenericSyntax()
    {
        var typeArg = new FakeTypeMetadata { Name = "string", FullName = "System.String" };
        var meta = new FakeTypeMetadata
        {
            Name = "Result",
            FullName = "My.Result",
            IsGeneric = true,
            TypeArguments = new List<ITypeMetadata> { typeArg }
        };
        Assert.Equal("Result<string>", Helpers.GetTypeScriptName(meta, StrictSettings));
    }

    // -------------------------------------------------------------------------
    // GetTypeScriptName — value tuple
    // -------------------------------------------------------------------------

    [Fact]
    public void GetTypeScriptName_ValueTuple_ReturnsObjectLiteralSyntax()
    {
        var nameType = new FakeTypeMetadata { Name = "string", FullName = "System.String" };
        var ageType = new FakeTypeMetadata { Name = "int", FullName = "System.Int32" };
        var nameTupleField = new FakeFieldMetadata { Name = "name", Type = nameType };
        var ageTupleField = new FakeFieldMetadata { Name = "age", Type = ageType };

        var meta = new FakeTypeMetadata
        {
            Name = "ValueTuple",
            FullName = "System.ValueTuple",
            IsValueTuple = true,
            TupleElements = new List<IFieldMetadata> { nameTupleField, ageTupleField }
        };
        Assert.Equal("{ name: string, age: number }", Helpers.GetTypeScriptName(meta, StrictSettings));
    }

    // -------------------------------------------------------------------------
    // GetOriginalName
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("System.Boolean", "bool", "bool")]
    [InlineData("System.Byte", "byte", "byte")]
    [InlineData("System.Char", "char", "char")]
    [InlineData("System.Decimal", "decimal", "decimal")]
    [InlineData("System.Double", "double", "double")]
    [InlineData("System.Int16", "short", "short")]
    [InlineData("System.Int32", "int", "int")]
    [InlineData("System.Int64", "long", "long")]
    [InlineData("System.SByte", "sbyte", "sbyte")]
    [InlineData("System.Single", "float", "float")]
    [InlineData("System.String", "string", "string")]
    [InlineData("System.UInt32", "uint", "uint")]
    [InlineData("System.UInt16", "ushort", "ushort")]
    [InlineData("System.UInt64", "ulong", "ulong")]
    public void GetOriginalName_PrimitiveType_ReturnsCSharpAlias(string fullName, string name, string expected)
    {
        var meta = new FakeTypeMetadata { Name = name, FullName = fullName };
        Assert.Equal(expected, Helpers.GetOriginalName(meta));
    }

    [Fact]
    public void GetOriginalName_NullablePrimitive_ReturnsAliasWithQuestionMark()
    {
        var meta = new FakeTypeMetadata { Name = "bool?", FullName = "System.Boolean?", IsNullable = true };
        Assert.Equal("bool?", Helpers.GetOriginalName(meta));
    }

    [Fact]
    public void GetOriginalName_CustomType_ReturnsOriginalName()
    {
        var meta = new FakeTypeMetadata { Name = "MyModel", FullName = "My.Namespace.MyModel" };
        Assert.Equal("MyModel", Helpers.GetOriginalName(meta));
    }

    // -------------------------------------------------------------------------
    // IsPrimitive
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("System.Boolean", "bool")]
    [InlineData("System.Byte", "byte")]
    [InlineData("System.Int32", "int")]
    [InlineData("System.String", "string")]
    [InlineData("System.Double", "double")]
    public void IsPrimitive_PrimitiveType_ReturnsTrue(string fullName, string name)
    {
        var meta = new FakeTypeMetadata { Name = name, FullName = fullName };
        Assert.True(Helpers.IsPrimitive(meta));
    }

    [Fact]
    public void IsPrimitive_NullablePrimitive_ReturnsTrue()
    {
        var meta = new FakeTypeMetadata { Name = "int?", FullName = "System.Int32?", IsNullable = true };
        Assert.True(Helpers.IsPrimitive(meta));
    }

    [Fact]
    public void IsPrimitive_EnumType_ReturnsTrue()
    {
        var meta = new FakeTypeMetadata { Name = "MyEnum", FullName = "My.MyEnum", IsEnum = true };
        Assert.True(Helpers.IsPrimitive(meta));
    }

    [Fact]
    public void IsPrimitive_CustomReferenceType_ReturnsFalse()
    {
        var meta = new FakeTypeMetadata { Name = "MyModel", FullName = "My.MyModel" };
        Assert.False(Helpers.IsPrimitive(meta));
    }

    [Fact]
    public void IsPrimitive_EnumerableOfPrimitive_ReturnsTrue()
    {
        var elementMeta = new FakeTypeMetadata { Name = "int", FullName = "System.Int32" };
        var meta = new FakeTypeMetadata
        {
            Name = "IEnumerable",
            FullName = "System.Collections.Generic.IEnumerable",
            IsEnumerable = true,
            TypeArguments = new List<ITypeMetadata> { elementMeta }
        };
        Assert.True(Helpers.IsPrimitive(meta));
    }

    [Fact]
    public void IsPrimitive_EnumerableOfCustomType_ReturnsFalse()
    {
        var elementMeta = new FakeTypeMetadata { Name = "MyModel", FullName = "My.MyModel" };
        var meta = new FakeTypeMetadata
        {
            Name = "IEnumerable",
            FullName = "System.Collections.Generic.IEnumerable",
            IsEnumerable = true,
            TypeArguments = new List<ITypeMetadata> { elementMeta }
        };
        Assert.False(Helpers.IsPrimitive(meta));
    }

    [Fact]
    public void IsPrimitive_EnumerableWithNoTypeArgs_ReturnsFalse()
    {
        var meta = new FakeTypeMetadata
        {
            Name = "IEnumerable",
            FullName = "System.Collections.IEnumerable",
            IsEnumerable = true,
            TypeArguments = new List<ITypeMetadata>()
        };
        Assert.False(Helpers.IsPrimitive(meta));
    }

    // -------------------------------------------------------------------------
    // Fake implementations used exclusively within this test class
    // -------------------------------------------------------------------------

    private sealed class FakeTypeMetadata : ITypeMetadata
    {
        // INamedItem
        public string Name { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string AssemblyName { get; set; } = string.Empty;

        // IClassMetadata
        public string DocComment { get; set; } = string.Empty;
        public bool IsAbstract { get; set; }
        public bool IsGeneric { get; set; }
        public bool IsStatic { get; set; }
        public string Namespace { get; set; } = string.Empty;
        public ITypeMetadata Type => this;
        public IEnumerable<IAttributeMetadata> Attributes { get; set; } = Enumerable.Empty<IAttributeMetadata>();
        public IClassMetadata BaseClass { get; set; } = null!;
        public IClassMetadata ContainingClass { get; set; } = null!;
        public IEnumerable<IConstantMetadata> Constants { get; set; } = Enumerable.Empty<IConstantMetadata>();
        public IEnumerable<IDelegateMetadata> Delegates { get; set; } = Enumerable.Empty<IDelegateMetadata>();
        public IEnumerable<IEventMetadata> Events { get; set; } = Enumerable.Empty<IEventMetadata>();
        public IEnumerable<IFieldMetadata> Fields { get; set; } = Enumerable.Empty<IFieldMetadata>();
        public IEnumerable<IInterfaceMetadata> Interfaces { get; set; } = Enumerable.Empty<IInterfaceMetadata>();
        public IEnumerable<IMethodMetadata> Methods { get; set; } = Enumerable.Empty<IMethodMetadata>();
        public IEnumerable<IPropertyMetadata> Properties { get; set; } = Enumerable.Empty<IPropertyMetadata>();
        public IEnumerable<IStaticReadOnlyFieldMetadata> StaticReadOnlyFields { get; set; } = Enumerable.Empty<IStaticReadOnlyFieldMetadata>();
        public IEnumerable<ITypeParameterMetadata> TypeParameters { get; set; } = Enumerable.Empty<ITypeParameterMetadata>();
        public IEnumerable<ITypeMetadata> TypeArguments { get; set; } = Enumerable.Empty<ITypeMetadata>();
        public IEnumerable<IClassMetadata> NestedClasses { get; set; } = Enumerable.Empty<IClassMetadata>();
        public IEnumerable<IEnumMetadata> NestedEnums { get; set; } = Enumerable.Empty<IEnumMetadata>();
        public IEnumerable<IInterfaceMetadata> NestedInterfaces { get; set; } = Enumerable.Empty<IInterfaceMetadata>();

        // ITypeMetadata
        public bool IsDictionary { get; set; }
        public bool IsDynamic { get; set; }
        public bool IsEnum { get; set; }
        public bool IsEnumerable { get; set; }
        public bool IsNullable { get; set; }
        public bool IsTask { get; set; }
        public bool IsDefined { get; set; }
        public bool IsValueTuple { get; set; }
        public ITypeMetadata ElementType { get; set; } = null!;
        public IEnumerable<IFieldMetadata> TupleElements { get; set; } = Enumerable.Empty<IFieldMetadata>();
        public IEnumerable<string> FileLocations { get; set; } = Enumerable.Empty<string>();
        public string DefaultValue { get; set; } = string.Empty;
    }

    private sealed class FakeFieldMetadata : IFieldMetadata
    {
        public string Name { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string AssemblyName { get; set; } = string.Empty;
        public string DocComment { get; set; } = string.Empty;
        public IEnumerable<IAttributeMetadata> Attributes { get; set; } = Enumerable.Empty<IAttributeMetadata>();
        public ITypeMetadata Type { get; set; } = null!;
    }
}
