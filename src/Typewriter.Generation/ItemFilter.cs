using System;
using System.Collections.Generic;
using System.Linq;
using Typewriter.CodeModel;

namespace Typewriter.Generation
{
    /// <summary>
    /// Applies name-based, attribute-based, or inheritance-based filters to collections of code model items.
    /// </summary>
    internal static class ItemFilter
    {
        /// <summary>
        /// Applies the specified filter expression to the given items.
        /// </summary>
        /// <param name="items">The items to filter.</param>
        /// <param name="filter">
        /// The filter expression. Wrap in <c>[...]</c> for attribute filtering,
        /// prefix with <c>:</c> for inheritance filtering, or use a plain name for item filtering.
        /// Supports <c>*</c> wildcard for starts-with, ends-with, and contains matching.
        /// </param>
        /// <param name="matchFound">Set to <see langword="true"/> if any items matched the filter.</param>
        /// <returns>The filtered collection, or the original collection if no filter was specified.</returns>
        internal static IEnumerable<Item> Apply(IEnumerable<Item> items, string filter, ref bool matchFound)
        {
            if (string.IsNullOrWhiteSpace(filter))
            {
                return items;
            }

            if (!(items is IFilterable filterable))
            {
                return items;
            }

            Func<Item, IEnumerable<string>> selector;

            filter = filter.Trim();

            if (filter.StartsWith("[", StringComparison.OrdinalIgnoreCase) && filter.EndsWith("]", StringComparison.OrdinalIgnoreCase))
            {
                filter = filter.Trim('[', ']', ' ');
                selector = filterable.AttributeFilterSelector;
            }
            else if (filter.StartsWith(":", StringComparison.OrdinalIgnoreCase))
            {
                filter = filter.Remove(0, 1).Trim();
                selector = filterable.InheritanceFilterSelector;
            }
            else
            {
                selector = filterable.ItemFilterSelector;
            }

            var filtered = ApplyFilter(items, filter, selector);

            matchFound = matchFound || filtered.Any();

            return filtered;
        }

        private static ICollection<Item> ApplyFilter(IEnumerable<Item> items, string filter, Func<Item, IEnumerable<string>> selector)
        {
            var parts = filter.Split('*');

            for (var i = 0; i < parts.Length; i++)
            {
                var part = parts[i];

                if (parts.Length == 1)
                {
                    items = items.Where(item => selector(item).Any(p => string.Equals(p, part, StringComparison.OrdinalIgnoreCase)));
                }
                else if (i == 0 && !string.IsNullOrWhiteSpace(part))
                {
                    items = items.Where(item => selector(item).Any(p => p.StartsWith(part, StringComparison.OrdinalIgnoreCase)));
                }
                else if (i == parts.Length - 1 && !string.IsNullOrWhiteSpace(part))
                {
                    items = items.Where(item => selector(item).Any(p => p.EndsWith(part, StringComparison.OrdinalIgnoreCase)));
                }
                else if (i > 0 && i < parts.Length - 1 && !string.IsNullOrWhiteSpace(part))
                {
                    items = items.Where(item => selector(item).Any(p => p.Contains(part)));
                }
            }

            return items.ToList();
        }
    }
}
