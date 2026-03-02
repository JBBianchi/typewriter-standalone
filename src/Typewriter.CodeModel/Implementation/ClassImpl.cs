using System.Collections.Generic;
using System.Linq;
using Typewriter.CodeModel.Collections;
using Typewriter.Metadata;
using static Typewriter.CodeModel.Helpers;

namespace Typewriter.CodeModel.Implementation
{
    public sealed class ClassImpl : Class
    {
        private readonly IClassMetadata _metadata;

        private ClassImpl(IClassMetadata metadata, Item parent, Settings settings)
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

        public override bool IsAbstract => _metadata.IsAbstract;

        public override bool IsGeneric => _metadata.IsGeneric;

        public override bool IsStatic => _metadata.IsStatic;

        private Type? _type;

        protected override Type Type => (_type ?? (_type = TypeImpl.FromMetadata(_metadata.Type, Parent, Settings)))!;

        private IAttributeCollection? _attributes;

        public override IAttributeCollection Attributes => _attributes ?? (_attributes = AttributeImpl.FromMetadata(_metadata.Attributes, this, Settings));

        private IConstantCollection? _constants;

        public override IConstantCollection Constants => _constants ?? (_constants = ConstantImpl.FromMetadata(_metadata.Constants, this, Settings));

        private IDelegateCollection? _delegates;

        public override IDelegateCollection Delegates => _delegates ?? (_delegates = DelegateImpl.FromMetadata(_metadata.Delegates, this, Settings));

        private DocComment? _docComment;

        public override DocComment DocComment => (_docComment ?? (_docComment = DocCommentImpl.FromXml(_metadata.DocComment, this)))!;

        private IEventCollection? _events;

        public override IEventCollection Events => _events ?? (_events = EventImpl.FromMetadata(_metadata.Events, this, Settings));

        private IFieldCollection? _fields;

        public override IFieldCollection Fields => _fields ?? (_fields = FieldImpl.FromMetadata(_metadata.Fields, this, Settings));

        private Class? _baseClass;

        public override Class BaseClass => (_baseClass ?? (_baseClass = FromMetadata(_metadata.BaseClass, this, Settings)))!;

        private Class? _containingClass;

        public override Class ContainingClass => (_containingClass ?? (_containingClass = FromMetadata(_metadata.ContainingClass, this, Settings)))!;

        private IInterfaceCollection? _interfaces;

        public override IInterfaceCollection Interfaces => _interfaces ?? (_interfaces = InterfaceImpl.FromMetadata(_metadata.Interfaces, this, Settings));

        private IMethodCollection? _methods;

        public override IMethodCollection Methods => _methods ?? (_methods = MethodImpl.FromMetadata(_metadata.Methods, this, Settings));

        private IPropertyCollection? _properties;

        public override IPropertyCollection Properties => _properties ?? (_properties = PropertyImpl.FromMetadata(GetPropertiesFromClassMetadata(_metadata.Properties), this, Settings));

        private IStaticReadOnlyFieldCollection? _staticReadOnlyFields;

        public override IStaticReadOnlyFieldCollection StaticReadOnlyFields => _staticReadOnlyFields ?? (_staticReadOnlyFields = StaticReadOnlyFieldImpl.FromMetadata(_metadata.StaticReadOnlyFields, this, Settings));

        private ITypeParameterCollection? _typeParameters;

        public override ITypeParameterCollection TypeParameters => _typeParameters ?? (_typeParameters = TypeParameterImpl.FromMetadata(_metadata.TypeParameters, this));

        private ITypeCollection? _typeArguments;

        public override ITypeCollection TypeArguments => _typeArguments ?? (_typeArguments = TypeImpl.FromMetadata(_metadata.TypeArguments, this, Settings));

        private IClassCollection? _nestedClasses;

        public override IClassCollection NestedClasses => _nestedClasses ?? (_nestedClasses = FromMetadata(_metadata.NestedClasses, this, Settings));

        private IEnumCollection? _nestedEnums;

        public override IEnumCollection NestedEnums => _nestedEnums ?? (_nestedEnums = EnumImpl.FromMetadata(_metadata.NestedEnums, this, Settings));

        private IInterfaceCollection? _nestedInterfaces;

        public override IInterfaceCollection NestedInterfaces => _nestedInterfaces ?? (_nestedInterfaces = InterfaceImpl.FromMetadata(_metadata.NestedInterfaces, this, Settings));

        public override string ToString()
        {
            return Name;
        }

        public static IClassCollection FromMetadata(IEnumerable<IClassMetadata> metadata, Item parent, Settings settings)
        {
            return new ClassCollectionImpl(metadata.Select(c => new ClassImpl(c, parent, settings)));
        }

        public static Class? FromMetadata(IClassMetadata? metadata, Item parent, Settings settings)
        {
            return metadata == null ? null : new ClassImpl(metadata, parent, settings);
        }

        /**
         *  Example of this:
         *  generated type:
         *  public partial class GeneratedClass
         *  {
         *      public string GeneratedProperty { get; set; }
         *  }
         *  user-defined type (in separate file):
         *  [ModelMetadata(typeof(GeneratedClassMetadata))]
         *  public partial class GeneratedClass
         *  {
         *
         *      internal sealed class GeneratedClassMetadata
         *      {
         *          [Column(DataType="varchar")]
         *          public string GeneratedProperty { get; set; }
         *      }
         *  }
         */
        /// <summary>
        /// Gets properties from a Metadata type: A nested class that allows decorators to be applied to generated code using either
        /// the MetadataType (.NET Framework) or ModelMetadataType (.NET Core) to link it to the original.
        /// </summary>
        /// <returns>If there is a metadata type defined, returns a collection of overridden properties merged with the originals.
        /// Otherwise, returns the original collection.</returns>
        /// <param name="originalProperties"><see cref="IEnumerable{IPropertyMetadata}"/> of <see cref="IPropertyMetadata"/>.</param>
        internal IEnumerable<IPropertyMetadata> GetPropertiesFromClassMetadata(IEnumerable<IPropertyMetadata> originalProperties)
        {
            var classMetadata = _metadata.Attributes.FirstOrDefault(a => string.Equals(a.Name, "MetadataType", System.StringComparison.OrdinalIgnoreCase) || string.Equals(a.Name, "ModelMetadataType", System.StringComparison.OrdinalIgnoreCase));
            if (classMetadata == null)
            {
                return originalProperties;
            }

            var metadataTypeArgument = classMetadata.Arguments.First();
            var metadataType = metadataTypeArgument.TypeValue;
            if (metadataType == null)
            {
                return originalProperties;
            }

            //loop through the original properties and use the metadata property whenever it matches the name of an original
            var mergedProperties = new List<IPropertyMetadata>();
            foreach (var property in originalProperties)
            {
                var metadataProperty = metadataType.Properties
                    .FirstOrDefault(mp => string.Equals(mp.Name, property.Name, System.StringComparison.OrdinalIgnoreCase));
                mergedProperties.Add(metadataProperty ?? property);
            }

            return mergedProperties;
        }
    }
}
