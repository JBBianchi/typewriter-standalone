using System.Collections.Generic;
using System.Linq;
using Typewriter.CodeModel.Collections;
using Typewriter.Metadata;
using static Typewriter.CodeModel.Helpers;

namespace Typewriter.CodeModel.Implementation
{
    public sealed class EnumImpl : Enum
    {
        private readonly IEnumMetadata _metadata;

        private EnumImpl(IEnumMetadata metadata, Item parent, Settings settings)
        {
            _metadata = metadata;
            Parent = parent;
            Settings = settings;
        }

        public Settings Settings { get; }

        public override Item Parent { get; }

        public override string name => CamelCase(_metadata.Name.TrimStart('@'));

        public override string Name => _metadata.Name.TrimStart('@');

        public override string FullName => _metadata.FullName;

        public override string AssemblyName => _metadata.AssemblyName;

        public override string Namespace => _metadata.Namespace;

        private Type? _type;

        protected override Type Type => (_type ?? (_type = TypeImpl.FromMetadata(_metadata.Type, Parent, Settings)))!;

        private bool? _isFlags;

        public override bool IsFlags => _isFlags ?? (_isFlags = Attributes.Any(a => string.Equals(a.FullName, "System.FlagsAttribute", System.StringComparison.OrdinalIgnoreCase))).Value;

        private IAttributeCollection? _attributes;

        public override IAttributeCollection Attributes => _attributes ?? (_attributes = AttributeImpl.FromMetadata(_metadata.Attributes, this, Settings));

        private DocComment? _docComment;

        public override DocComment DocComment => (_docComment ?? (_docComment = DocCommentImpl.FromXml(_metadata.DocComment, this)))!;

        private IEnumValueCollection? _values;

        public override IEnumValueCollection Values => _values ?? (_values = EnumValueImpl.FromMetadata(_metadata.Values, this, Settings));

        private Class? _containingClass;

        public override Class ContainingClass => (_containingClass ?? (_containingClass = ClassImpl.FromMetadata(_metadata.ContainingClass, this, Settings)))!;

        public override string ToString()
        {
            return Name;
        }

        public static IEnumCollection FromMetadata(IEnumerable<IEnumMetadata> metadata, Item parent, Settings settings)
        {
            return new EnumCollectionImpl(metadata.Select(e => new EnumImpl(e, parent, settings)));
        }
    }
}
