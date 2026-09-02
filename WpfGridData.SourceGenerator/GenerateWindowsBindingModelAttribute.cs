using System;

namespace WpfGridData
{
    /// <summary>
    /// Specifies that properties for binding to Windows controls should be generated for the specified model type.
    /// </summary>
    /// <remarks>
    /// Properties will be generated as get-only properties that return
    /// the corresponding property values from the specified model type.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class GenerateWindowsBindingModelAttribute : Attribute
    {
        /// <summary>
        /// Creates a new instance of the <see cref="GenerateWindowsBindingModelAttribute"/> class for the specified model type.
        /// </summary>
        /// <param name="modelType">The model type for which to generate properties for binding to Windows controls.</param>
        public GenerateWindowsBindingModelAttribute(Type modelType) {}
    }
}
