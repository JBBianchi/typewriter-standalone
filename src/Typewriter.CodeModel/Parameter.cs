using Typewriter.CodeModel.Attributes;

namespace Typewriter.CodeModel
{
    /// <summary>
    /// Represents a parameter.
    /// </summary>
    [Context(nameof(Parameter), "Parameters")]
    public abstract class Parameter : Item
    {
        /// <summary>
        /// All attributes defined on the parameter.
        /// </summary>
        public abstract IAttributeCollection Attributes { get; }

        /// <summary>
        /// The full original name of the parameter.
        /// </summary>
        public abstract string FullName { get; }

        /// <summary>
        /// The name of the assembly containing the parameter.
        /// </summary>
        public abstract string AssemblyName { get; }

        /// <summary>
        /// The default value of the parameter if it's optional.
        /// </summary>
        public abstract string DefaultValue { get; }

        /// <summary>
        /// Determines if the parameter has a default value.
        /// </summary>
        public abstract bool HasDefaultValue { get; }

        /// <summary>
        /// The name of the parameter (camelCased).
        /// </summary>
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable IDE1006 // Naming Styles

        // ReSharper disable once InconsistentNaming
        public abstract string name { get; }
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore SA1300 // Element should begin with upper-case letter

        /// <summary>
        /// The name of the parameter.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// The parent context of the parameter.
        /// </summary>
        public abstract Item Parent { get; }

        /// <summary>
        /// The type of the parameter.
        /// </summary>
        public abstract Type Type { get; }

        /// <summary>
        /// Converts the current instance to string.
        /// </summary>
        public static implicit operator string(Parameter instance)
        {
            return instance.ToString()!;
        }
    }
}