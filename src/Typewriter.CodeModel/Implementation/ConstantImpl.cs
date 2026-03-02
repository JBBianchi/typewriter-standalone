using System.Collections.Generic;
using System.Linq;
using Typewriter.CodeModel.Collections;
using Typewriter.Metadata;
using static Typewriter.CodeModel.Helpers;

namespace Typewriter.CodeModel.Implementation
{
    public sealed class ConstantImpl : Constant
    {
        private readonly IConstantMetadata _metadata;

        private ConstantImpl(IConstantMetadata metadata, Item parent, Settings settings)
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

        private IAttributeCollection? _attributes;

        public override IAttributeCollection Attributes => _attributes ?? (_attributes = AttributeImpl.FromMetadata(_metadata.Attributes, this, Settings));

        private DocComment? _docComment;

        public override DocComment DocComment => (_docComment ?? (_docComment = DocCommentImpl.FromXml(_metadata.DocComment, this)))!;

        private Type? _type;

        public override Type Type => (_type ?? (_type = TypeImpl.FromMetadata(_metadata.Type, this, Settings)))!;

        public override string Value => _metadata.Value;

        public override string ToString()
        {
            return Name;
        }

        public static IConstantCollection FromMetadata(IEnumerable<IConstantMetadata> metadata, Item parent, Settings settings)
        {
            return new ConstantCollectionImpl(metadata.Select(c => new ConstantImpl(c, parent, settings)));
        }
    }
}
