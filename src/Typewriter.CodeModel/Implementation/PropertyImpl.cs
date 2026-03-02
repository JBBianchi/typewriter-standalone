using System.Collections.Generic;
using System.Linq;
using Typewriter.CodeModel.Collections;
using Typewriter.Metadata;
using static Typewriter.CodeModel.Helpers;

namespace Typewriter.CodeModel.Implementation
{
    public sealed class PropertyImpl : Property
    {
        private readonly IPropertyMetadata _metadata;

        private IAttributeCollection? _attributes;

        private DocComment? _docComment;

        private Type? _type;

        private PropertyImpl(IPropertyMetadata metadata, Item parent, Settings settings)
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

        public override bool HasGetter => _metadata.HasGetter;

        public override bool HasSetter => _metadata.HasSetter;

        public override bool IsAbstract => _metadata.IsAbstract;

        public override bool IsVirtual => _metadata.IsVirtual;

        public override IAttributeCollection Attributes => _attributes ?? (_attributes = AttributeImpl.FromMetadata(_metadata.Attributes, this, Settings));

        public override DocComment DocComment => (_docComment ?? (_docComment = DocCommentImpl.FromXml(_metadata.DocComment, this)))!;

        public override Type Type => (_type ?? (_type = TypeImpl.FromMetadata(_metadata.Type, this, Settings)))!;

        public static IPropertyCollection FromMetadata(IEnumerable<IPropertyMetadata> metadata, Item parent, Settings settings)
        {
            return new PropertyCollectionImpl(metadata.Select(p => new PropertyImpl(p, parent, settings)));
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
