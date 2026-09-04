using System;

namespace WinDataBinding
{
    /// <summary>
    /// Describes a type mapping for the source generator.
    /// </summary>
    /// <remarks>
    /// This attribute can be used to inform the source generator about a custom wrapper for an underlying type.
    /// <para>
    /// A strongly typed ID could be implemented as <c>[MapTypeAttribute(typeof(StrongId), typeof(Guid), "Value")]</c>.
    /// </para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class MapTypeAttribute : Attribute
    {
        /// <summary>
        /// Creates a new instance of the <see cref="MapTypeAttribute"/> class for the specified type mapping.
        /// </summary>
        /// <param name="sourceType">The source type to be remapped.</param>
        /// <param name="targetType">The target type to which the source type is remapped.</param>
        /// <param name="expression">
        /// The expression (member) of <paramref name="sourceType"/> defining the remapping.
        /// This expression is written as is (no parsing or evaluation is performed).
        /// </param>
        public MapTypeAttribute(Type sourceType, Type targetType, string expression) {}
    }
}
