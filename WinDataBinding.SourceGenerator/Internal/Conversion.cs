namespace WinDataBinding.SourceGenerator.Internal;

/// <summary>
/// One generated property produced from a source type that cannot be bound directly, such as a
/// <c>NodaTime</c> value. A source type may map to several of these.
/// </summary>
internal sealed class Conversion
{
    /// <summary>Chain segment appended to the property name, for source types that map to more than one property.</summary>
    public string? Suffix { get; init; }

    /// <summary>Return type, without the nullable annotation.</summary>
    public required string Type { get; init; }

    public required ExprBuilder Build { get; init; }

    /// <summary>Return type is a reference type, so the property is always nullable.</summary>
    public bool IsReference { get; init; }

    /// <summary>Guarded conversions always yield <c>T?</c>, even from a non-nullable chain.</summary>
    public bool ForceNullable { get; init; }

    /// <summary>Set when the conversion differs pre-NET6; both branches are then emitted under <c>#if</c>.</summary>
    public string? TypePre6 { get; init; }

    public ExprBuilder? BuildPre6 { get; init; }
}
