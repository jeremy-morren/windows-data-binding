using System.Collections.Immutable;

namespace WinDataBinding.SourceGenerator.Internal;

/// <summary>How a type relates to the <c>StronglyTypedId</c> generator.</summary>
internal enum StrongIdKind
{
    /// <summary>Not a strongly typed ID.</summary>
    None,

    /// <summary>Declared with one of the built-in templates, named by <see cref="StrongId.Template"/>.</summary>
    Template,

    /// <summary>Declared with a custom template, which this generator does not support.</summary>
    Custom,
}

/// <param name="Template">The built-in template's enum member, set only when <paramref name="Kind"/> is a template.</param>
/// <param name="CustomTemplates">
/// The custom templates named by the attribute, in the order given. A bare <c>[StronglyTypedId]</c> names none.
/// </param>
internal readonly record struct StrongId(
    StrongIdKind Kind, string? Template, ImmutableArray<string> CustomTemplates)
{
    public static readonly StrongId None = new(StrongIdKind.None, null, ImmutableArray<string>.Empty);
}

/// <summary>How a strongly typed ID exposes its underlying value.</summary>
/// <param name="ValueType">Fully qualified type of the value.</param>
/// <param name="IsReference">Whether that type is a reference type, so the property is always nullable.</param>
/// <param name="PropertyName">The property holding the value.</param>
/// <param name="Renderer">How that value renders as text, for the twin property.</param>
internal readonly record struct StrongIdBinding(
    string ValueType, bool IsReference, string PropertyName, Renderer Renderer);
