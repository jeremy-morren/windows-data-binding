using System;

namespace WinDataBinding
{
    /// <summary>
    /// Describes a custom <c>StronglyTypedId</c> template, so a strongly typed ID declared with it can be
    /// bound. Apply to the options class named by <see cref="GenerateWindowsBindingModelAttribute"/>.
    /// </summary>
    /// <remarks>
    /// The four built-in templates need no setup. 
    /// A custom template does, because the property holding the underlying value is written by StronglyTypedId's own source generator, 
    /// and source generators cannot see each other's output.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class StrongIdTemplateSetupAttribute : Attribute
    {
        /// <summary>
        /// Creates a new instance of the <see cref="StrongIdTemplateSetupAttribute"/> class for the
        /// specified custom template.
        /// </summary>
        /// <param name="templateName">The template name, as passed to <c>[StronglyTypedId]</c>.</param>
        /// <param name="valueType">The type of the underlying value.</param>
        /// <param name="propertyName">The name of the property holding the underlying value.</param>
        /// <param name="isFormattable">Indicates whether the Strong ID template implements <c>IFormattable</c>.</param>
        public StrongIdTemplateSetupAttribute(string templateName, Type valueType, string propertyName, bool isFormattable = true) {}
    }
}
