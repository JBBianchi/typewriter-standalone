using System;

namespace Typewriter.CodeModel.Attributes
{
    /// <summary>
    /// Marks context metadata classes.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ContextAttribute
        : System.Attribute
    {
        /// <summary>
        /// Initializes a new instance of the ContextAttribute.
        /// </summary>
        public ContextAttribute(string name, string collectionName)
        {
            Name = name;
            CollectionName = collectionName;
        }

        /// <summary>
        /// The name of the context.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The name of collections of the context.
        /// </summary>
        public string CollectionName { get; }
    }
}