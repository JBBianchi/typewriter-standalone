using System.Collections.Generic;

namespace Typewriter.CodeModel.Collections
{
    public class StaticReadOnlyFieldCollectionImpl : ItemCollectionImpl<StaticReadOnlyField>, IStaticReadOnlyFieldCollection
    {
        public StaticReadOnlyFieldCollectionImpl(IEnumerable<StaticReadOnlyField> values)
            : base(values)
        {
        }

        protected override IEnumerable<string> GetAttributeFilter(StaticReadOnlyField item)
        {
            if (item is null)
            {
                yield break;
            }

            foreach (var attribute in item.Attributes)
            {
                yield return attribute.Name;
                yield return attribute.FullName;
            }
        }

        protected override IEnumerable<string> GetItemFilter(StaticReadOnlyField item)
        {
            if (item is null)
            {
                yield break;
            }

            yield return item.Name;
            yield return item.FullName;
        }
    }
}