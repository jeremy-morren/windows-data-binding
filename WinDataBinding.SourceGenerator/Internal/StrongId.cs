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

/// <param name="Template">
/// The template's name: a built-in template's enum member, or the custom template's string. Null when the
/// type is not a strongly typed ID, or when a bare <c>[StronglyTypedId]</c> names no template at all.
/// </param>
internal readonly record struct StrongId(StrongIdKind Kind, string? Template)
{
    public static readonly StrongId None = new(StrongIdKind.None, null);
}

/// <summary>How a strongly typed ID exposes its underlying value.</summary>
/// <param name="ValueType">Fully qualified type of the value.</param>
/// <param name="IsReference">Whether that type is a reference type, so the property is always nullable.</param>
/// <param name="PropertyName">The property holding the value.</param>
/// <param name="Renderer">How that value renders as text, for the twin property.</param>
internal readonly record struct StrongIdBinding(
    string ValueType, bool IsReference, string PropertyName, Renderer Renderer);
