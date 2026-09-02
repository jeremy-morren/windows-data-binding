// Compiler-required attributes that netstandard2.0 does not ship.
// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices;

/// <summary>Enables <c>init</c> accessors and records.</summary>
internal static class IsExternalInit;

/// <summary>Enables <c>required</c> members.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property, Inherited = false)]
internal sealed class RequiredMemberAttribute : Attribute;

/// <summary>Marks members that need a specific compiler feature, as <c>required</c> does.</summary>
[AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
internal sealed class CompilerFeatureRequiredAttribute(string featureName) : Attribute
{
    public string FeatureName { get; } = featureName;
}
