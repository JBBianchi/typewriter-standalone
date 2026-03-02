using System.Collections.Generic;
using System.Linq;
using Typewriter.CodeModel.Collections;
using Typewriter.Metadata;

namespace Typewriter.CodeModel.Implementation
{
    public class AttributeArgumentImpl : AttributeArgument
    {
        private readonly IAttributeArgumentMetadata _metadata;
        private readonly Item _parent;

        public AttributeArgumentImpl(IAttributeArgumentMetadata metadata, Item parent, Settings settings)
        {
            _metadata = metadata;
            _parent = parent;
            Settings = settings;
        }

        public Settings Settings { get; }

        public override Type Type => TypeImpl.FromMetadata(_metadata.Type, _parent, Settings)!;

        public override Type TypeValue => TypeImpl.FromMetadata(_metadata.TypeValue, _parent, Settings)!;

        public override object Value => _metadata.GetValue();

        public static IAttributeArgumentCollection FromMetadata(IEnumerable<IAttributeArgumentMetadata> metadata, Item parent, Settings settings)
        {
            return new AttributeArgumentCollectionImpl(metadata.Select(a => new AttributeArgumentImpl(a, parent, settings)));
        }
    }
}
