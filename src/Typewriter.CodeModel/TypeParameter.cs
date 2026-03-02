using Typewriter.CodeModel.Attributes;

namespace Typewriter.CodeModel
{
    /// <summary>
    /// Represents a generic type parameter.
    /// </summary>
    [Context(nameof(TypeParameter), "TypeParameters")]
    public abstract class TypeParameter : Item
    {
        /// <summary>
        /// The name of the type parameter (camelCased).
        /// </summary>
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable IDE1006 // Naming Styles

        // ReSharper disable once InconsistentNaming
        public abstract string name { get; }
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore SA1300 // Element should begin with upper-case letter

        /// <summary>
        /// The name of the type parameter.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// The parent context of the type parameter.
        /// </summary>
        public abstract Item Parent { get; }
    }
}
