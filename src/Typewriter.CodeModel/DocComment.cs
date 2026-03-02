using Typewriter.CodeModel.Attributes;

namespace Typewriter.CodeModel
{
    /// <summary>
    /// Represents an XML documentation comment.
    /// </summary>
    [Context(nameof(DocComment), "DocComments")]
    public abstract class DocComment : Item
    {
        /// <summary>
        /// The contents of the summary tag.
        /// </summary>
        public abstract string Summary { get; }

        /// <summary>
        /// The contents of the returns tag.
        /// </summary>
        public abstract string Returns { get; }

        /// <summary>
        /// All parameter tags of the documentation comment.
        /// </summary>
        public abstract IParameterCommentCollection Parameters { get; }

        /// <summary>
        /// The parent context of the documentation comment.
        /// </summary>
        public abstract Item Parent { get; }

        /// <summary>
        /// Converts the current instance to string.
        /// </summary>
        public static implicit operator string(DocComment instance)
        {
            return instance.ToString()!;
        }
    }
}