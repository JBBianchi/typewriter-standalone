using System.Collections.Generic;
using System.Linq;
using Typewriter.CodeModel.Collections;
using Typewriter.Metadata;
using static Typewriter.CodeModel.Helpers;

namespace Typewriter.CodeModel.Implementation
{
    public sealed class InterfaceImpl : Interface
    {
        private readonly IInterfaceMetadata _metadata;

        private InterfaceImpl(IInterfaceMetadata metadata, Item parent, Settings settings)
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

        public override string Namespace => _metadata.Namespace;

        public override string AssemblyName => _metadata.AssemblyName;

        public override bool IsGeneric => _metadata.IsGeneric;

        private Type? _type;

        public override Type Type => (_type ?? (_type = TypeImpl.FromMetadata(_metadata.Type, Parent, Settings)))!;

        private IAttributeCollection? _attributes;

        public override IAttributeCollection Attributes => _attributes ?? (_attributes = AttributeImpl.FromMetadata(_metadata.Attributes, this, Settings));

        private DocComment? _docComment;

        public override DocComment DocComment => (_docComment ?? (_docComment = DocCommentImpl.FromXml(_metadata.DocComment, this)))!;

        private IEventCollection? _events;

        public override IEventCollection Events => _events ?? (_events = EventImpl.FromMetadata(_metadata.Events, this, Settings));

        private IInterfaceCollection? _interfaces;

        public override IInterfaceCollection Interfaces => _interfaces ?? (_interfaces = FromMetadata(_metadata.Interfaces, this, Settings));

        private IMethodCollection? _methods;

        public override IMethodCollection Methods => _methods ?? (_methods = MethodImpl.FromMetadata(_metadata.Methods, this, Settings));

        private IPropertyCollection? _properties;

        public override IPropertyCollection Properties => _properties ?? (_properties = PropertyImpl.FromMetadata(_metadata.Properties, this, Settings));

        private ITypeParameterCollection? _typeParameters;

        public override ITypeParameterCollection TypeParameters => _typeParameters ?? (_typeParameters = TypeParameterImpl.FromMetadata(_metadata.TypeParameters, this));

        private ITypeCollection? _typeArguments;

        public override ITypeCollection TypeArguments => _typeArguments ?? (_typeArguments = TypeImpl.FromMetadata(_metadata.TypeArguments, this, Settings));

        private Class? _containingClass;

        public override Class ContainingClass => (_containingClass ?? (_containingClass = ClassImpl.FromMetadata(_metadata.ContainingClass, this, Settings)))!;

        public override string ToString()
        {
            return Name;
        }

        public static IInterfaceCollection FromMetadata(IEnumerable<IInterfaceMetadata> metadata, Item parent, Settings settings)
        {
            return new InterfaceCollectionImpl(metadata.Select(i => new InterfaceImpl(i, parent, settings)));
        }
    }
}
