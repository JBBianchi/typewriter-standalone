using Typewriter.CodeModel.Attributes;

namespace Typewriter.CodeModel
{
    /// <summary>
    /// Represents an enum value.
    /// </summary>
    [Context(nameof(EnumValue), "Values")]
    public abstract class EnumValue : Item
    {
        /// <summary>
        /// All attributes defined on the enum value.
        /// </summary>
        public abstract IAttributeCollection Attributes { get; }

        /// <summary>
        /// The XML documentation comment of the enum value.
        /// </summary>
        public abstract DocComment DocComment { get; }

        /// <summary>
        /// The full original name of the enum value including namespace and containing class names.
        /// </summary>
        public abstract string FullName { get; }

        /// <summary>
        /// The name of the assembly containing the enum value.
        /// </summary>
        public abstract string AssemblyName { get; }

        /// <summary>
        /// The name of the enum value (camelCased).
        /// </summary>
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable IDE1006 // Naming Styles

        // ReSharper disable once InconsistentNaming
        public abstract string name { get; }
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore SA1300 // Element should begin with upper-case letter

        /// <summary>
        /// The name of the enum value.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// The parent context of the enum value.
        /// </summary>
        public abstract Item Parent { get; }

        /// <summary>
        /// The numeric value.
        /// </summary>
        public abstract long Value { get; }

        /// <summary>
        /// Converts the current instance to string.
        /// </summary>
        public static implicit operator string(EnumValue instance)
        {
            return instance.ToString()!;
        }
    }
}