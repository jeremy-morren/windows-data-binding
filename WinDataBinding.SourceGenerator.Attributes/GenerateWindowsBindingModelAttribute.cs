using System;

namespace WinDataBinding
{
    /// <summary>
    /// Specifies that properties for binding to Windows controls should be generated for the specified model type.
    /// </summary>
    /// <remarks>
    /// Properties will be generated as get-only properties that return
    /// the corresponding property values from the specified model type.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
    [JetBrains.Annotations.MeansImplicitUse(JetBrains.Annotations.ImplicitUseTargetFlags.WithMembers)]
    public sealed class GenerateWindowsBindingModelAttribute : Attribute
    {
        /// <summary>
        /// Creates a new instance of the <see cref="GenerateWindowsBindingModelAttribute"/> class for the specified model type.
        /// </summary>
        /// <param name="modelType">The model type for which to generate properties for binding to Windows controls.</param>
        public GenerateWindowsBindingModelAttribute(Type modelType) {}

        /// <summary>
        /// Creates a new instance of the <see cref="GenerateWindowsBindingModelAttribute"/> class for the specified model type.
        /// </summary>
        /// <param name="modelType">The model type for which to generate properties for binding to Windows controls.</param>
        /// <param name="generationOptions">The options for generating the Windows binding model.</param>
        /// <remarks>
        /// The generation options is a class decorated with attributes that the source generator understands. Available attributes:
        /// <list type="bullet">
        /// <item>
        /// <description><see cref="StrongIdTemplateSetupAttribute"/>Configures strong ID template setup for the generation options.</description>
        /// </item>
        /// <item>
        /// <description><see cref="MapTypeAttribute"/>Maps a wrapper type onto the type it wraps.</description>
        /// </item>
        /// </list>
        /// </remarks>
        public GenerateWindowsBindingModelAttribute(Type modelType, Type generationOptions) {}
    }
}


