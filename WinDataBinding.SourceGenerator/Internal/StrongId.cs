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

/// <param name="Template">The built-in template's name, set only when <paramref name="Kind"/> is a template.</param>
internal readonly record struct StrongId(StrongIdKind Kind, string? Template)
{
    public static readonly StrongId None = new(StrongIdKind.None, null);
    public static readonly StrongId Custom = new(StrongIdKind.Custom, null);
}
