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

    private readonly Dictionary<ISymbol, TypeTraits> _traits = new(SymbolEqualityComparer.Default);
    private readonly Dictionary<ISymbol, StrongId> _strongIds = new(SymbolEqualityComparer.Default);

    private readonly Dictionary<string, INamedTypeSymbol?> _resolved = new(StringComparer.Ordinal);

    public Compilation Compilation => compilation;

    private INamedTypeSymbol? Formattable => Resolve("System.IFormattable");
    private INamedTypeSymbol? JsonNode => Resolve("System.Text.Json.Nodes.JsonNode");
    private INamedTypeSymbol? JsonElement => Resolve("System.Text.Json.JsonElement");

    /// <summary>Whether the type is a sequence, which is bound as-is rather than traversed.</summary>
    public bool IsSequence(ITypeSymbol type) => Get(type).IsSequence;

    /// <summary>How the type itself renders as text. <see cref="Renderer.None"/> for a sequence.</summary>
    public Renderer GetRenderer(ITypeSymbol type) => Get(type).Renderer;

    /// <summary>How a sequence's elements render as text.</summary>
    public Renderer GetElementRenderer(ITypeSymbol type) => Get(type).ElementRenderer;

    /// <summary>Whether the type is a strongly typed ID, and if so which template declared it.</summary>
    public StrongId GetStrongId(ITypeSymbol type)
    {
        if (_strongIds.TryGetValue(type, out var cached)) return cached;

        var strongId = ComputeStrongId(type);
        _strongIds[type] = strongId;
        return strongId;
    }

    private INamedTypeSymbol? Resolve(string metadataName)
    {
        if (_resolved.TryGetValue(metadataName, out var cached)) return cached;

        var symbol = compilation.GetTypeByMetadataName(metadataName);
        _resolved[metadataName] = symbol;
        return symbol;
    }

    private TypeTraits Get(ITypeSymbol type)
    {
        if (_traits.TryGetValue(type, out var cached)) return cached;

        var traits = ComputeTraits(type);
        _traits[type] = traits;
        return traits;
    }

    private TypeTraits ComputeTraits(ITypeSymbol type)
    {
        // string is IEnumerable<char>, but it binds as a leaf.
        if (type.SpecialType == SpecialType.System_String) return default;

        // JSON values are checked first: JsonArray and JsonObject are enumerable, but they render whole.
        if (JsonRenderer(type) is var json && json != Renderer.None)
            return new TypeTraits(false, json, Renderer.None);

        if (ElementType(type) is { } element)
            return new TypeTraits(true, Renderer.None, RendererFor(element));

        return new TypeTraits(false, RendererFor(type), Renderer.None);
    }

    private Renderer RendererFor(ITypeSymbol type)
    {
        if (JsonRenderer(type) is var json && json != Renderer.None) return json;

        return Formattable is { } formattable && Implements(type, formattable)
            ? Renderer.Formattable
            : Renderer.None;
    }

    private Renderer JsonRenderer(ITypeSymbol type)
    {
        if (JsonElement is { } element && SymbolEqualityComparer.Default.Equals(type, element))
            return Renderer.JsonElement;

        if (JsonNode is { } node && InheritsFrom(type, node))
            return Renderer.JsonNode;

        return Renderer.None;
    }

    private static bool InheritsFrom(ITypeSymbol type, INamedTypeSymbol baseType)
    {
        for (var current = type; current is not null; current = current.BaseType)
            if (SymbolEqualityComparer.Default.Equals(current, baseType))
                return true;

        return false;
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

            // A custom template is named by a string argument; a bare [StronglyTypedId] names nothing.
            return new StrongId(StrongIdKind.Custom,
                attribute.ConstructorArguments.Length > 0
                    ? attribute.ConstructorArguments[0].Value as string
                    : null);
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
