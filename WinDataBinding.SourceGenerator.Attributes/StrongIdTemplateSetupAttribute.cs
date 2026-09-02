using System;

namespace WinDataBinding
{
    /// <summary>
    /// Specifies the setup for a custom strong ID.
    /// </summary>
    /// <remarks>
    /// Specifies the template name, the return type, and the property name for the strong ID.
    /// If an ID specifies multiple templates, the first matching setup will be used.
    /// <para>
    /// Example usage: <c>[StrongIdTemplate("DoubleTemplate", typeof(double), "DoubleValue")]</c>
    /// </para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public sealed class StrongIdTemplateAttribute : Attribute
    {
        /// <summary>
        /// Creates a new instance of the <see cref="StrongIdTemplateAttribute"/> class with the specified template setup for the strong ID.
        /// </summary>
        /// <param name="templateName">The name of the template for the strong ID.</param>
        /// <param name="returnType">The return type of the strong ID.</param>
        /// <param name="propertyName">The property name for the strong ID.</param>
        public StrongIdTemplateAttribute(string templateName, Type returnType, string propertyName) {}
    }
}
