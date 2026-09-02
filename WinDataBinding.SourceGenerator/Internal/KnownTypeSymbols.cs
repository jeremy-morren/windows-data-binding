using Microsoft.CodeAnalysis;

namespace WinDataBinding.SourceGenerator.Internal;

/// <summary>
/// Types resolved from the compilation once and reused for a whole traversal, following the approach
/// <c>System.Text.Json</c>'s generator takes with its own <c>KnownTypeSymbols</c>. Classifying a member means
/// walking <see cref="ITypeSymbol.AllInterfaces"/>, which is the expensive part of the traversal and repeats
/// constantly in a deep graph, so the answers are memoised per type.
/// </summary>
/// <remarks>
/// These symbols never enter a pipeline model: an instance is created inside the transform, never escapes it,
/// and is dropped when it returns. That confinement is why a plain <see cref="Dictionary{TKey, TValue}"/> is
/// safe here even though Roslyn may run generators concurrently — nothing else can reach it. Hoisting an
/// instance into a static or a cache shared between transforms would need a concurrent collection instead.
/// </remarks>
internal sealed class KnownTypeSymbols(Compilation compilation)
{
    [Flags]
    private enum Traits
    {
        None = 0,
        Computed = 1,
        Sequence = 2,
        FormattableSequence = 4,
    }

    private readonly Dictionary<ISymbol, Traits> _traits = new(SymbolEqualityComparer.Default);

    private INamedTypeSymbol? _formattable;
    private bool _formattableResolved;

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
    public bool IsSequence(ITypeSymbol type) => Get(type).HasFlag(Traits.Sequence);

    /// <summary>Whether the type is a sequence whose elements can be rendered as text.</summary>
    public bool IsFormattableSequence(ITypeSymbol type) => Get(type).HasFlag(Traits.FormattableSequence);

    private Traits Get(ITypeSymbol type)
    {
        if (_traits.TryGetValue(type, out var cached)) return cached;

        var traits = Compute(type);
        _traits[type] = traits;
        return traits;
    }

    private Traits Compute(ITypeSymbol type)
    {
        // string is IEnumerable<char>, but it binds as a leaf.
        if (type.SpecialType == SpecialType.System_String) return Traits.Computed;

        if (ElementType(type) is not { } element) return Traits.Computed;

        var traits = Traits.Computed | Traits.Sequence;
        if (Formattable is { } formattable && Implements(element, formattable))
            traits |= Traits.FormattableSequence;

        return traits;
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
