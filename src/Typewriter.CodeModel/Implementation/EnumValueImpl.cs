using System.Collections.Generic;
using System.Linq;
using Typewriter.CodeModel.Collections;
using Typewriter.Metadata;
using static Typewriter.CodeModel.Helpers;

namespace Typewriter.CodeModel.Implementation
{
    public sealed class EnumValueImpl : EnumValue
    {
        private readonly IEnumValueMetadata _metadata;

        private EnumValueImpl(IEnumValueMetadata metadata, Item parent, Settings settings)
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

        public override long Value => _metadata.Value;

        private IAttributeCollection? _attributes;

        public override IAttributeCollection Attributes => _attributes ?? (_attributes = AttributeImpl.FromMetadata(_metadata.Attributes, this, Settings));

        private DocComment? _docComment;

        public override DocComment DocComment => (_docComment ?? (_docComment = DocCommentImpl.FromXml(_metadata.DocComment, this)))!;

        public override string ToString()
        {
            return Name;
        }

        public static IEnumValueCollection FromMetadata(IEnumerable<IEnumValueMetadata> metadata, Item parent, Settings settings)
        {
            return new EnumValueCollectionImpl(metadata.Select(e => new EnumValueImpl(e, parent, settings)));
        }
    }
}
