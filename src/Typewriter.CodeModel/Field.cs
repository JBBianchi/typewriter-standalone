using Typewriter.CodeModel.Attributes;

namespace Typewriter.CodeModel
{
    /// <summary>
    /// Represents a field.
    /// </summary>
    [Context(nameof(Field), "Fields")]
    public abstract class Field : Item
    {
        /// <summary>
        /// All attributes defined on the field.
        /// </summary>
        public abstract IAttributeCollection Attributes { get; }

        /// <summary>
        /// The XML documentation comment of the field.
        /// </summary>
        public abstract DocComment DocComment { get; }

        /// <summary>
        /// The full original name of the field including namespace and containing class names.
        /// </summary>
        public abstract string FullName { get; }

        /// <summary>
        /// The name of the assembly containing the attribute.
        /// </summary>
        public abstract string AssemblyName { get; }

        /// <summary>
        /// The name of the field (camelCased).
        /// </summary>
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable IDE1006 // Naming Styles

        // ReSharper disable once InconsistentNaming
        public abstract string name { get; }
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore SA1300 // Element should begin with upper-case letter

        /// <summary>
        /// The name of the field.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// The parent context of the field.
        /// </summary>
        public abstract Item Parent { get; }

        /// <summary>
        /// The type of the field.
        /// </summary>
        public abstract Type Type { get; }

        /// <summary>
        /// Converts the current instance to string.
        /// </summary>
        public static implicit operator string(Field instance)
        {
            return instance.ToString()!;
        }
    }
}