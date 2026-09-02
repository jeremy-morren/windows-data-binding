using Microsoft.CodeAnalysis;

namespace WinDataBinding.SourceGenerator.Internal;

/// <summary>
/// Types resolved from the compilation once and reused for a whole traversal, following the approach
/// <c>System.Text.Json</c>'s generator takes with its own <c>KnownTypeSymbols</c>. Classifying a member means
/// walking <see cref="ITypeSymbol.AllInterfaces"/> or its attributes, which is the expensive part of the
/// traversal and repeats constantly in a deep graph, so the answers are memoised per type.
/// </summary>
/// <remarks>
/// These symbols never enter a pipeline model: an instance is created inside the transform, never escapes it,
/// and is dropped when it returns. That confinement is why a plain <see cref="Dictionary{TKey, TValue}"/> is
/// safe here even though Roslyn may run generators concurrently — nothing else can reach it. Hoisting an
/// instance into a static or a cache shared between transforms would need a concurrent collection instead.
/// </remarks>
internal sealed class KnownTypeSymbols(Compilation compilation)
{
    private const string StrongIdAttribute = "StronglyTypedIds.StronglyTypedIdAttribute";
    private const string StrongIdTemplate = "StronglyTypedIds.Template";

    [Flags]
    private enum Traits
    {
        None = 0,
        Computed = 1,
        Sequence = 2,
        FormattableSequence = 4,
        Formattable = 8,
    }

    private readonly Dictionary<ISymbol, Traits> _traits = new(SymbolEqualityComparer.Default);
    private readonly Dictionary<ISymbol, StrongId> _strongIds = new(SymbolEqualityComparer.Default);

    private INamedTypeSymbol? _formattable;
    private bool _formattableResolved;

    public Compilation Compilation => compilation;

    /// <summary><c>System.IFormattable</c>, resolved lazily and only once.</summary>
    private INamedTypeSymbol? Formattable
    {
        get
        {
            if (!_formattableResolved)
            {
                _formattable = compilation.GetTypeByMetadataName("System.IFormattable");
                _formattableResolved = true;
            }

            return _formattable;
        }
    }

    /// <summary>Whether the type is a sequence, which is bound as-is rather than traversed.</summary>
    public bool IsSequence(ITypeSymbol type) => Traits_(type).HasFlag(Traits.Sequence);

    /// <summary>Whether the type is a sequence whose elements can be rendered as text.</summary>
    public bool IsFormattableSequence(ITypeSymbol type) => Traits_(type).HasFlag(Traits.FormattableSequence);

    /// <summary>Whether the type itself can be rendered as text. Never true for a sequence.</summary>
    public bool IsFormattable(ITypeSymbol type) => Traits_(type).HasFlag(Traits.Formattable);

    /// <summary>Whether the type is a strongly typed ID, and if so which template declared it.</summary>
    public StrongId GetStrongId(ITypeSymbol type)
    {
        if (_strongIds.TryGetValue(type, out var cached)) return cached;

        var strongId = ComputeStrongId(type);
        _strongIds[type] = strongId;
        return strongId;
    }

    private Traits Traits_(ITypeSymbol type)
    {
        if (_traits.TryGetValue(type, out var cached)) return cached;

        var traits = ComputeTraits(type);
        _traits[type] = traits;
        return traits;
    }

    private Traits ComputeTraits(ITypeSymbol type)
    {
        var traits = Traits.Computed;

        // string is IEnumerable<char>, but it binds as a leaf.
        if (type.SpecialType == SpecialType.System_String) return traits;

        var formattable = Formattable;

        // A sequence is rendered through its elements, so the two traits are mutually exclusive.
        if (ElementType(type) is { } element)
        {
            traits |= Traits.Sequence;
            if (formattable is not null && Implements(element, formattable))
                traits |= Traits.FormattableSequence;

            return traits;
        }

        if (formattable is not null && Implements(type, formattable))
            traits |= Traits.Formattable;

        return traits;
    }

    private static StrongId ComputeStrongId(ITypeSymbol type)
    {
        foreach (var attribute in type.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString(Formats.Match) != StrongIdAttribute) continue;

            // A built-in template is an enum argument; anything else names a custom template.
            if (attribute.ConstructorArguments.Length > 0 &&
                attribute.ConstructorArguments[0] is { Kind: TypedConstantKind.Enum } argument &&
                argument.Type?.ToDisplayString(Formats.Match) == StrongIdTemplate &&
                TemplateName(argument) is { } template)
                return new StrongId(StrongIdKind.Template, template);

            return StrongId.Custom;
        }

        return StrongId.None;
    }

    /// <summary>The enum member's name, which is what the template table is keyed by.</summary>
    private static string? TemplateName(TypedConstant argument)
    {
        if (argument.Type is not INamedTypeSymbol enumType) return null;

        foreach (var member in enumType.GetMembers())
            if (member is IFieldSymbol { HasConstantValue: true } field && Equals(field.ConstantValue, argument.Value))
                return field.Name;

        return null;
    }

    /// <summary>The <c>T</c> of the sequence, or null when the type is not one.</summary>
    private static ITypeSymbol? ElementType(ITypeSymbol type)
    {
        if (type is IArrayTypeSymbol array) return array.ElementType;

        if (type.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T)
            return ((INamedTypeSymbol)type).TypeArguments[0];

        foreach (var candidate in type.AllInterfaces)
            if (candidate.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T)
                return candidate.TypeArguments[0];

        return null;
    }

    private static bool Implements(ITypeSymbol type, INamedTypeSymbol interfaceSymbol)
    {
        if (SymbolEqualityComparer.Default.Equals(type, interfaceSymbol)) return true;

        foreach (var candidate in type.AllInterfaces)
            if (SymbolEqualityComparer.Default.Equals(candidate, interfaceSymbol))
                return true;

        return false;
    }
}
