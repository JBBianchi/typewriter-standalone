using Typewriter.CodeModel.Attributes;

namespace Typewriter.CodeModel
{
    /// <summary>
    /// Represents an XML documentation comment parameter tag.
    /// </summary>
    [Context(nameof(ParameterComment), "ParameterComments")]
    public abstract class ParameterComment : Item
    {
        /// <summary>
        /// The name of the parameter.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// The parameter description.
        /// </summary>
        public abstract string Description { get; }

        /// <summary>
        /// The parent context of the documentation comment parameter.
        /// </summary>
        public abstract Item Parent { get; }

        /// <summary>
        /// Converts the current instance to string.
        /// </summary>
        public static implicit operator string(ParameterComment instance)
        {
            return instance.ToString()!;
        }
    }
}