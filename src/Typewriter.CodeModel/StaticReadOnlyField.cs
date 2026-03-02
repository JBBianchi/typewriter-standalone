using Typewriter.CodeModel.Attributes;

namespace Typewriter.CodeModel
{
    /// <summary>
    /// Represents a static readonly field.
    /// </summary>
    [Context(nameof(StaticReadOnlyField), "StaticReadOnlyFields")]
    public abstract class StaticReadOnlyField : Item
    {
        /// <summary>
        /// All attributes defined on the static readonly field.
        /// </summary>
        public abstract IAttributeCollection Attributes { get; }

        /// <summary>
        /// The XML documentation comment of the static readonly field.
        /// </summary>
        public abstract DocComment DocComment { get; }

        /// <summary>
        /// The full original name of the static readonly field including namespace and containing class names.
        /// </summary>
        public abstract string FullName { get; }

        /// <summary>
        /// The name of the assembly containing the static readonly field.
        /// </summary>
        public abstract string AssemblyName { get; }

        /// <summary>
        /// The name of the static readonly field (camelCased).
        /// </summary>
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable IDE1006 // Naming Styles

        // ReSharper disable once InconsistentNaming
        public abstract string name { get; }
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore SA1300 // Element should begin with upper-case letter

        /// <summary>
        /// The name of the static readonly field.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// The parent context of the static readonly field.
        /// </summary>
        public abstract Item Parent { get; }

        /// <summary>
        /// The type of the static readonly field.
        /// </summary>
        public abstract Type Type { get; }

        /// <summary>
        /// The value of the static readonly field.
        /// </summary>
        public abstract string Value { get; }

        /// <summary>
        /// Converts the current instance to string.
        /// </summary>
        public static implicit operator string(StaticReadOnlyField instance)
        {
            return instance.ToString()!;
        }
    }
}