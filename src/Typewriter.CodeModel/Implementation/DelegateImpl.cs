using System.Collections.Generic;
using System.Linq;
using Typewriter.CodeModel.Collections;
using Typewriter.Metadata;
using static Typewriter.CodeModel.Helpers;

namespace Typewriter.CodeModel.Implementation
{
    public sealed class DelegateImpl : Delegate
    {
        private readonly IDelegateMetadata _metadata;

        private DelegateImpl(IDelegateMetadata metadata, Item parent, Settings settings)
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

        public override bool IsGeneric => _metadata.IsGeneric;

        private IAttributeCollection? _attributes;

        public override IAttributeCollection Attributes => _attributes ?? (_attributes = AttributeImpl.FromMetadata(_metadata.Attributes, this, Settings));

        private DocComment? _docComment;

        public override DocComment DocComment => (_docComment ?? (_docComment = DocCommentImpl.FromXml(_metadata.DocComment, this)))!;

        private ITypeParameterCollection? _typeParameters;

        public override ITypeParameterCollection TypeParameters => _typeParameters ?? (_typeParameters = TypeParameterImpl.FromMetadata(_metadata.TypeParameters, this));

        private IParameterCollection? _parameters;

        public override IParameterCollection Parameters => _parameters ?? (_parameters = ParameterImpl.FromMetadata(_metadata.Parameters, this, Settings));

        private Type? _type;

        public override Type Type => (_type ?? (_type = TypeImpl.FromMetadata(_metadata.Type, this, Settings)))!;

        public override string ToString()
        {
            return Name;
        }

        public static IDelegateCollection FromMetadata(IEnumerable<IDelegateMetadata> metadata, Item parent, Settings settings)
        {
            return new DelegateCollectionImpl(metadata.Select(d => new DelegateImpl(d, parent, settings)));
        }
    }
}
