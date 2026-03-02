using System.Collections.Generic;
using System.Linq;
using Typewriter.CodeModel.Collections;
using Typewriter.Metadata;
using static Typewriter.CodeModel.Helpers;

namespace Typewriter.CodeModel.Implementation
{
    public class RecordImpl : Record
    {
        private readonly IRecordMetadata _metadata;

        private RecordImpl(IRecordMetadata metadata, Item parent, Settings settings)
        {
            _metadata = metadata;
            Parent = parent;
            Settings = settings;
        }

        public Settings Settings { get; }

        private IAttributeCollection? _attributes;

        public override IAttributeCollection Attributes => _attributes ?? (_attributes = AttributeImpl.FromMetadata(_metadata.Attributes, this, Settings));

        private Record? _baseRecord;

        public override Record BaseRecord => (_baseRecord ?? (_baseRecord = FromMetadata(_metadata.BaseRecord, this, Settings)))!;

        private IConstantCollection? _constants;

        public override IConstantCollection Constants => _constants ?? (_constants = ConstantImpl.FromMetadata(_metadata.Constants, this, Settings));

        private Record? _containingRecord;

        public override Record ContainingRecord => (_containingRecord ?? (_containingRecord = FromMetadata(_metadata.ContainingRecord, this, Settings)))!;

        private IDelegateCollection? _delegates;

        public override IDelegateCollection Delegates => _delegates ?? (_delegates = DelegateImpl.FromMetadata(_metadata.Delegates, this, Settings));

        private DocComment? _docComment;

        public override DocComment DocComment => (_docComment ?? (_docComment = DocCommentImpl.FromXml(_metadata.DocComment, this)))!;

        private IEventCollection? _events;

        public override IEventCollection Events => _events ?? (_events = EventImpl.FromMetadata(_metadata.Events, this, Settings));

        private IFieldCollection? _fields;

        public override IFieldCollection Fields => _fields ?? (_fields = FieldImpl.FromMetadata(_metadata.Fields, this, Settings));

        public override string FullName => _metadata.FullName;

        public override string AssemblyName => _metadata.AssemblyName;

        private IInterfaceCollection? _interfaces;

        public override IInterfaceCollection Interfaces => _interfaces ?? (_interfaces = InterfaceImpl.FromMetadata(_metadata.Interfaces, this, Settings));

        public override bool IsAbstract => _metadata.IsAbstract;

        public override bool IsGeneric => _metadata.IsGeneric;

        private IMethodCollection? _methods;

        public override IMethodCollection Methods => _methods ?? (_methods = MethodImpl.FromMetadata(_metadata.Methods, this, Settings));

        public override string name => CamelCase(_metadata.Name.TrimStart('@'));

        public override string Name => _metadata.Name.TrimStart('@');

        public override string Namespace => _metadata.Namespace;

        public override Item Parent { get; }

        private IPropertyCollection? _properties;

        public override IPropertyCollection Properties => _properties ?? (_properties = PropertyImpl.FromMetadata(_metadata.Properties, this, Settings));

        private IStaticReadOnlyFieldCollection? _staticReadOnlyFields;

        public override IStaticReadOnlyFieldCollection StaticReadOnlyFields => _staticReadOnlyFields ?? (_staticReadOnlyFields = StaticReadOnlyFieldImpl.FromMetadata(_metadata.StaticReadOnlyFields, this, Settings));


        private ITypeParameterCollection? _typeParameters;

        public override ITypeParameterCollection TypeParameters => _typeParameters ?? (_typeParameters = TypeParameterImpl.FromMetadata(_metadata.TypeParameters, this));

        private ITypeCollection? _typeArguments;

        public override ITypeCollection TypeArguments => _typeArguments ?? (_typeArguments = TypeImpl.FromMetadata(_metadata.TypeArguments, this, Settings));

        private Type? _type;

        protected override Type Type => (_type ?? (_type = TypeImpl.FromMetadata(_metadata.Type, Parent, Settings)))!;

        public override string ToString()
        {
            return Name;
        }

        public static IRecordCollection FromMetadata(IEnumerable<IRecordMetadata> metadata, Item parent, Settings settings)
        {
            return new RecordCollectionImpl(metadata.Select(c => new RecordImpl(c, parent, settings)));
        }

        public static Record? FromMetadata(IRecordMetadata? metadata, Item parent, Settings settings)
        {
            return metadata == null ? null : new RecordImpl(metadata, parent, settings);
        }
    }
}
