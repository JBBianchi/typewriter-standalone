// Type aliases to disambiguate Typewriter.CodeModel types from System types
// (System.Attribute, System.Enum, System.Type are imported via implicit usings).
using CmAttribute = Typewriter.CodeModel.Attribute;
using CmEnum = Typewriter.CodeModel.Enum;
using CmType = Typewriter.CodeModel.Type;

using Typewriter.CodeModel;
using Typewriter.CodeModel.Collections;
using Xunit;

namespace Typewriter.UnitTests.CodeModel;

/// <summary>
/// Behavioral tests for <see cref="ItemCollectionImpl{T}"/> and key derived collection
/// implementations: <see cref="FieldCollectionImpl"/>, <see cref="ClassCollectionImpl"/>,
/// <see cref="EnumCollectionImpl"/>.
/// </summary>
public class CollectionTests
{
    // =========================================================================
    // ItemCollectionImpl — base collection behavior (via StubCollection)
    // =========================================================================

    [Fact]
    public void ItemCollection_Empty_CountIsZero()
    {
        var collection = new StubCollection(Array.Empty<StubItem>());
        Assert.Empty(collection);
    }

    [Fact]
    public void ItemCollection_WithItems_CountMatchesInput()
    {
        var items = new[] { new StubItem("a"), new StubItem("b"), new StubItem("c") };
        var collection = new StubCollection(items);
        Assert.Equal(3, collection.Count);
    }

    [Fact]
    public void ItemCollection_Enumeration_YieldsAllItems()
    {
        var items = new[] { new StubItem("x"), new StubItem("y") };
        var collection = new StubCollection(items);
        Assert.Equal(new[] { "x", "y" }, collection.Select(i => i.Label));
    }

    [Fact]
    public void ItemCollection_IndexerAccess_ReturnsCorrectItem()
    {
        var items = new[] { new StubItem("first"), new StubItem("second") };
        var collection = new StubCollection(items);
        Assert.Equal("second", collection[1].Label);
    }

    [Fact]
    public void ItemCollection_ItemFilterSelector_ReturnsFilterStrings()
    {
        var item = new StubItem("myItem");
        var collection = new StubCollection(new[] { item });
        var results = collection.ItemFilterSelector(item).ToList();
        Assert.Contains("myItem", results);
    }

    [Fact]
    public void ItemCollection_ItemFilterSelector_WrongType_ReturnsEmpty()
    {
        var collection = new StubCollection(Array.Empty<StubItem>());
        var wrongItem = new OtherItem();
        var results = collection.ItemFilterSelector(wrongItem).ToList();
        Assert.Empty(results);
    }

    [Fact]
    public void ItemCollection_AttributeFilterSelector_DefaultReturnsEmpty()
    {
        var item = new StubItem("x");
        var collection = new StubCollection(new[] { item });
        var results = collection.AttributeFilterSelector(item).ToList();
        Assert.Empty(results);
    }

    [Fact]
    public void ItemCollection_InheritanceFilterSelector_DefaultReturnsEmpty()
    {
        var item = new StubItem("x");
        var collection = new StubCollection(new[] { item });
        var results = collection.InheritanceFilterSelector(item).ToList();
        Assert.Empty(results);
    }

    // =========================================================================
    // FieldCollectionImpl — item and attribute filter
    // =========================================================================

    [Fact]
    public void FieldCollection_ItemFilter_ReturnsNameAndFullName()
    {
        var field = new StubField("MyField", "My.Namespace.MyField");
        var collection = new FieldCollectionImpl(new[] { field });

        var results = collection.ItemFilterSelector(field).ToList();

        Assert.Contains("MyField", results);
        Assert.Contains("My.Namespace.MyField", results);
    }

    [Fact]
    public void FieldCollection_AttributeFilter_ReturnsAttributeNameAndFullName()
    {
        var attr = new StubAttribute("SerializeAttribute", "System.SerializeAttribute");
        var attrs = new AttributeCollectionImpl(new[] { (CmAttribute)attr });
        var field = new StubField("MyField", "My.Namespace.MyField", attrs);
        var collection = new FieldCollectionImpl(new[] { field });

        var results = collection.AttributeFilterSelector(field).ToList();

        Assert.Contains("SerializeAttribute", results);
        Assert.Contains("System.SerializeAttribute", results);
    }

    [Fact]
    public void FieldCollection_AttributeFilter_NoAttributes_ReturnsEmpty()
    {
        var field = new StubField("MyField", "My.Namespace.MyField");
        var collection = new FieldCollectionImpl(new[] { field });

        var results = collection.AttributeFilterSelector(field).ToList();

        Assert.Empty(results);
    }

    [Fact]
    public void FieldCollection_WrongTypePassedToSelector_ReturnsEmpty()
    {
        var collection = new FieldCollectionImpl(Array.Empty<Field>());
        var wrongItem = new StubItem("not-a-field");

        Assert.Empty(collection.ItemFilterSelector(wrongItem));
        Assert.Empty(collection.AttributeFilterSelector(wrongItem));
    }

    // =========================================================================
    // ClassCollectionImpl — item, attribute, and inheritance filters
    // =========================================================================

    [Fact]
    public void ClassCollection_ItemFilter_ReturnsNameAndFullName()
    {
        var cls = new StubClass("MyClass", "My.Namespace.MyClass");
        var collection = new ClassCollectionImpl(new[] { cls });

        var results = collection.ItemFilterSelector(cls).ToList();

        Assert.Contains("MyClass", results);
        Assert.Contains("My.Namespace.MyClass", results);
    }

    [Fact]
    public void ClassCollection_AttributeFilter_ReturnsAttributeNamesAndFullNames()
    {
        var attr = new StubAttribute("ObsoleteAttribute", "System.ObsoleteAttribute");
        var attrs = new AttributeCollectionImpl(new[] { (CmAttribute)attr });
        var cls = new StubClass("MyClass", "My.Namespace.MyClass", attributes: attrs);
        var collection = new ClassCollectionImpl(new[] { cls });

        var results = collection.AttributeFilterSelector(cls).ToList();

        Assert.Contains("ObsoleteAttribute", results);
        Assert.Contains("System.ObsoleteAttribute", results);
    }

    [Fact]
    public void ClassCollection_InheritanceFilter_IncludesBaseClassNamesAndInterfaceNames()
    {
        var baseClass = new StubClass("BaseModel", "My.Namespace.BaseModel");
        var iface = new StubInterface("IDisposable", "System.IDisposable");
        var ifaces = new InterfaceCollectionImpl(new[] { (Interface)iface });
        var cls = new StubClass("MyClass", "My.Namespace.MyClass", baseClass: baseClass, interfaces: ifaces);
        var collection = new ClassCollectionImpl(new[] { cls });

        var results = collection.InheritanceFilterSelector(cls).ToList();

        Assert.Contains("BaseModel", results);
        Assert.Contains("My.Namespace.BaseModel", results);
        Assert.Contains("IDisposable", results);
        Assert.Contains("System.IDisposable", results);
    }

    [Fact]
    public void ClassCollection_InheritanceFilter_NoBaseNoInterfaces_ReturnsEmpty()
    {
        var cls = new StubClass("MyClass", "My.Namespace.MyClass");
        var collection = new ClassCollectionImpl(new[] { cls });

        var results = collection.InheritanceFilterSelector(cls).ToList();

        Assert.Empty(results);
    }

    // =========================================================================
    // EnumCollectionImpl — item and attribute filters
    // =========================================================================

    [Fact]
    public void EnumCollection_ItemFilter_ReturnsNameAndFullName()
    {
        var enm = new StubEnum("Status", "My.Namespace.Status");
        var collection = new EnumCollectionImpl(new[] { enm });

        var results = collection.ItemFilterSelector(enm).ToList();

        Assert.Contains("Status", results);
        Assert.Contains("My.Namespace.Status", results);
    }

    [Fact]
    public void EnumCollection_AttributeFilter_ReturnsAttributeNames()
    {
        var attr = new StubAttribute("FlagsAttribute", "System.FlagsAttribute");
        var attrs = new AttributeCollectionImpl(new[] { (CmAttribute)attr });
        var enm = new StubEnum("Status", "My.Namespace.Status", attributes: attrs);
        var collection = new EnumCollectionImpl(new[] { enm });

        var results = collection.AttributeFilterSelector(enm).ToList();

        Assert.Contains("FlagsAttribute", results);
        Assert.Contains("System.FlagsAttribute", results);
    }

    // =========================================================================
    // Stub types — minimal implementations used only in this test class
    // =========================================================================

    // --- Simple item stubs ---------------------------------------------------

    private sealed class StubItem : Item
    {
        public StubItem(string label) => Label = label;
        public string Label { get; }
    }

    private sealed class OtherItem : Item { }

    private sealed class StubCollection : ItemCollectionImpl<StubItem>
    {
        public StubCollection(IEnumerable<StubItem> items) : base(items) { }

        protected override IEnumerable<string> GetItemFilter(StubItem item)
        {
            yield return item.Label;
        }
    }

    // --- Field stubs ---------------------------------------------------------

#pragma warning disable IDE1006 // Naming Styles
    private sealed class StubField : Field
    {
        private readonly IAttributeCollection _attributes;

        public StubField(string name, string fullName, IAttributeCollection? attributes = null)
        {
            Name = name;
            FullName = fullName;
            _attributes = attributes ?? new AttributeCollectionImpl(Array.Empty<CmAttribute>());
        }

        public override string Name { get; }
        public override string FullName { get; }
        public override IAttributeCollection Attributes => _attributes;
        public override string name => Name;
        public override string AssemblyName => string.Empty;
        public override DocComment DocComment => null!;
        public override Item Parent => null!;
        public override CmType Type => null!;
    }
#pragma warning restore IDE1006

    // --- Attribute stubs -----------------------------------------------------

    private sealed class StubAttribute : CmAttribute
    {
        public StubAttribute(string name, string fullName)
        {
            Name = name;
            FullName = fullName;
        }

        public override string Name { get; }
        public override string FullName { get; }

#pragma warning disable IDE1006
        public override string name => Name;
#pragma warning restore IDE1006

        public override string AssemblyName => string.Empty;
        public override Item Parent => null!;
        public override string Value => string.Empty;
        public override IAttributeArgumentCollection Arguments => null!;
        public override CmType Type => null!;
    }

    // --- Class stubs ---------------------------------------------------------

    private sealed class StubClass : Class
    {
        private readonly IAttributeCollection _attributes;
        private readonly IInterfaceCollection _interfaces;

        public StubClass(
            string name,
            string fullName,
            IAttributeCollection? attributes = null,
            Class? baseClass = null,
            IInterfaceCollection? interfaces = null)
        {
            Name = name;
            FullName = fullName;
            _attributes = attributes ?? new AttributeCollectionImpl(Array.Empty<CmAttribute>());
            BaseClass = baseClass!;
            _interfaces = interfaces ?? new InterfaceCollectionImpl(Array.Empty<Interface>());
        }

        public override string Name { get; }
        public override string FullName { get; }
        public override IAttributeCollection Attributes => _attributes;
        public override Class BaseClass { get; }
        public override IInterfaceCollection Interfaces => _interfaces;

#pragma warning disable IDE1006
        public override string name => Name;
#pragma warning restore IDE1006

        public override string AssemblyName => string.Empty;
        public override bool IsAbstract => false;
        public override bool IsGeneric => false;
        public override bool IsStatic => false;
        public override string Namespace => string.Empty;
        public override Item Parent => null!;
        public override Class ContainingClass => null!;
        public override DocComment DocComment => null!;
        public override IConstantCollection Constants => null!;
        public override IDelegateCollection Delegates => null!;
        public override IEventCollection Events => null!;
        public override IFieldCollection Fields => null!;
        public override IMethodCollection Methods => null!;
        public override IPropertyCollection Properties => null!;
        public override IStaticReadOnlyFieldCollection StaticReadOnlyFields => null!;
        public override ITypeParameterCollection TypeParameters => null!;
        public override ITypeCollection TypeArguments => null!;
        public override IClassCollection NestedClasses => null!;
        public override IEnumCollection NestedEnums => null!;
        public override IInterfaceCollection NestedInterfaces => null!;
        protected override CmType Type => null!;
    }

    // --- Interface stubs -----------------------------------------------------

    private sealed class StubInterface : Interface
    {
        private readonly IInterfaceCollection _interfaces;

        public StubInterface(string name, string fullName)
        {
            Name = name;
            FullName = fullName;
            _interfaces = new InterfaceCollectionImpl(Array.Empty<Interface>());
        }

        public override string Name { get; }
        public override string FullName { get; }

#pragma warning disable IDE1006
        public override string name => Name;
#pragma warning restore IDE1006

        public override string AssemblyName => string.Empty;
        public override bool IsGeneric => false;
        public override string Namespace => string.Empty;
        public override Item Parent => null!;
        public override Class ContainingClass => null!;
        public override DocComment DocComment => null!;
        public override IAttributeCollection Attributes => new AttributeCollectionImpl(Array.Empty<CmAttribute>());
        public override IEventCollection Events => null!;
        public override IInterfaceCollection Interfaces => _interfaces;
        public override IMethodCollection Methods => null!;
        public override IPropertyCollection Properties => null!;
        public override ITypeParameterCollection TypeParameters => null!;
        public override ITypeCollection TypeArguments => null!;
        public override CmType Type => null!;
    }

    // --- Enum stubs ----------------------------------------------------------

    private sealed class StubEnum : CmEnum
    {
        private readonly IAttributeCollection _attributes;

        public StubEnum(string name, string fullName, IAttributeCollection? attributes = null)
        {
            Name = name;
            FullName = fullName;
            _attributes = attributes ?? new AttributeCollectionImpl(Array.Empty<CmAttribute>());
        }

        public override string Name { get; }
        public override string FullName { get; }
        public override IAttributeCollection Attributes => _attributes;

#pragma warning disable IDE1006
        public override string name => Name;
#pragma warning restore IDE1006

        public override string AssemblyName => string.Empty;
        public override bool IsFlags => false;
        public override string Namespace => string.Empty;
        public override Item Parent => null!;
        public override Class ContainingClass => null!;
        public override DocComment DocComment => null!;
        public override IEnumValueCollection Values => null!;
        protected override CmType Type => null!;
    }
}
