using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace WinDataBinding.SourceGenerator.Internal;

/// <summary>
/// Types resolved from the compilation once and reused for a whole traversal.
/// Classifying a member means walking <see cref="ITypeSymbol.AllInterfaces"/> or its attributes,
/// which is the expensive part of the traversal and repeats constantly in a deep graph, so the answers are memoised per type.
/// Same approach used by <c>System.Text.Json</c> source generator.
/// </summary>
/// <remarks>
/// These symbols never enter a pipeline model: an instance is created inside the transform, never escapes it,
/// and is dropped when it returns. That confinement is why a plain <see cref="Dictionary{TKey, TValue}"/> is
/// safe here even though Roslyn may run generators concurrently — nothing else can reach it. 
/// Hoisting an instance into a static or a cache shared between transforms would need a concurrent collection instead.
/// </remarks>
internal sealed class KnownTypeSymbols(Compilation compilation)
{
    private const string StrongIdAttribute = "StronglyTypedIds.StronglyTypedIdAttribute";
    private const string StrongIdTemplate = "StronglyTypedIds.Template";
    private const string BinderAttribute = "WinDataBinding.GenerateWindowsBindingModelAttribute";

    private readonly Dictionary<ISymbol, TypeTraits> _traits = new(SymbolEqualityComparer.Default);
    private readonly Dictionary<ISymbol, StrongId> _strongIds = new(SymbolEqualityComparer.Default);

    private readonly Dictionary<string, INamedTypeSymbol?> _resolved = new(StringComparer.Ordinal);

    private readonly Dictionary<ISymbol, (INamedTypeSymbol? Source, INamedTypeSymbol? Options)> _binders =
        new(SymbolEqualityComparer.Default);

    private readonly Dictionary<ISymbol, FlattenedBinder> _flattened = new(SymbolEqualityComparer.Default);

    public Compilation Compilation => compilation;

    private INamedTypeSymbol? Formattable => Resolve("System.IFormattable");
    private INamedTypeSymbol? JsonNode => Resolve("System.Text.Json.Nodes.JsonNode");
    private INamedTypeSymbol? JsonElement => Resolve("System.Text.Json.JsonElement");

    /// <summary>Whether the generated code can apply JetBrains' contract annotation.</summary>
    public bool HasContractAnnotation => IsUsable("JetBrains.Annotations.ContractAnnotationAttribute");

    /// <summary>Whether the generated code can apply the BCL's conditional null annotation.</summary>
    public bool HasNotNullIfNotNull => IsUsable("System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute");

    /// <summary>
    /// Whether the type exists and the generated code may actually name it. 
    /// Existing is not enough: plenty of libraries, NodaTime among them, embed their own internal copy of the JetBrains annotations, 
    /// and those resolve by name while being inaccessible from outside.
    /// </summary>
    private bool IsUsable(string metadataName) =>
        Resolve(metadataName) is { } symbol && compilation.IsSymbolAccessibleWithin(symbol, compilation.Assembly);

    /// <summary>
    /// Whether the type can order itself against its own kind, so a binder wrapping it can too.
    /// <c>Comparer{T}.Default</c> needs exactly this and throws at runtime without it.
    /// </summary>
    public static bool IsComparable(ITypeSymbol type)
    {
        foreach (var candidate in type.AllInterfaces)
        {
            if (candidate.ToDisplayString(Formats.Match) != "System.IComparable") continue;

            // The non-generic interface, or the generic one closed over this very type.
            if (candidate.TypeArguments.IsDefaultOrEmpty) return true;
            if (SymbolEqualityComparer.Default.Equals(candidate.TypeArguments[0], type)) return true;
        }

        return false;
    }

    /// <summary>
    /// Whether the type is declared in the compilation being generated for rather than referenced from elsewhere.
    /// A binder in a referenced assembly is already compiled, generated half and all, so its flattened properties
    /// are ordinary members there and bind as any other member would.
    /// </summary>
    public bool IsLocal(ITypeSymbol type) =>
        SymbolEqualityComparer.Default.Equals(type.ContainingAssembly, compilation.Assembly);

    /// <summary>
    /// The type a binder flattens and the options type it was given, when the type is a binder at all.
    /// </summary>
    public bool TryGetBinder(ITypeSymbol type, out INamedTypeSymbol source, out INamedTypeSymbol? options)
    {
        if (!_binders.TryGetValue(type, out var target))
        {
            target = ComputeBinder(type);
            _binders[type] = target;
        }

        source = target.Source!;
        options = target.Options;
        return target.Source is not null;
    }

    /// <summary>What a nested binder flattens, worked out once and reused wherever else it appears.</summary>
    public bool TryGetFlattened(ISymbol binder, out FlattenedBinder flattened) =>
        _flattened.TryGetValue(binder, out flattened);

    public void SetFlattened(ISymbol binder, FlattenedBinder flattened) => _flattened[binder] = flattened;

    private static (INamedTypeSymbol? Source, INamedTypeSymbol? Options) ComputeBinder(ITypeSymbol type)
    {
        foreach (var attribute in type.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString(Formats.Match) != BinderAttribute) continue;

            var arguments = attribute.ConstructorArguments;
            if (arguments.Length is not (1 or 2) || arguments[0].Value is not INamedTypeSymbol source) continue;

            return (source, arguments.Length == 2 ? arguments[1].Value as INamedTypeSymbol : null);
        }

        return (null, null);
    }

    /// <summary>Whether the type is a sequence, which is bound as-is rather than traversed.</summary>
    public bool IsSequence(ITypeSymbol type) => Get(type).IsSequence;

    /// <summary>How the type itself renders as text. <see cref="Renderer.None"/> for a sequence.</summary>
    public Renderer GetRenderer(ITypeSymbol type) => Get(type).Renderer;

    /// <summary>How a sequence's elements render as text.</summary>
    public Renderer GetElementRenderer(ITypeSymbol type) => Get(type).ElementRenderer;

    /// <summary>Whether an element can be null, so rendering it has to be lifted.</summary>
    public bool IsElementLifted(ITypeSymbol type) => Get(type).ElementIsLifted;

    /// <summary>How to read the number of items the type holds.</summary>
    public CountAccess GetCount(ITypeSymbol type) => Get(type).Count;

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
        {
            // Nullable<T> implements nothing itself, so the renderer comes from what it wraps and the call is
            // lifted instead. Without that a sequence of T? would render nothing at all.
            var lifted = element.IsReferenceType || IsNullableValue(element);
            return new TypeTraits(
                true, Renderer.None, RendererFor(Unwrap(element)), CountAccessFor(type), lifted);
        }

        return new TypeTraits(false, RendererFor(type), Renderer.None);
    }

    private Renderer RendererFor(ITypeSymbol type)
    {
        // Only reached for a sequence's elements; a string property itself is a leaf and never gets here.
        if (type.SpecialType == SpecialType.System_String) return Renderer.Text;

        // An enum implements IFormattable, but its two-argument ToString is obsolete: the provider is
        // ignored, and calling it would hand the consumer a CS0618. The plain overload gives the same text.
        if (type.TypeKind == TypeKind.Enum) return Renderer.Enum;

        if (JsonRenderer(type) is var json && json != Renderer.None) return json;

        if (Formattable is not { } formattable || !Implements(type, formattable)) return Renderer.None;

        return Offers(type, FormatsItself) ? Renderer.FormattableDirect : Renderer.FormattableByCast;
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
                return new StrongId(StrongIdKind.Template, template, ImmutableArray<string>.Empty);

            // Otherwise custom templates name it. 
            // Both of the attribute's constructors take those as a 'params string[]',
            // so the names arrive as an array argument, not as strings.
            // A bare [StronglyTypedId] passes an empty array and so names none.
            return new StrongId(StrongIdKind.Custom, null, CustomTemplates(attribute.ConstructorArguments));
        }

        return StrongId.None;
    }

    /// <summary>
    /// Every custom template named by the attribute, in the order given. 
    /// Which of them applies depends on the generation options, which are not known here.
    /// </summary>
    private static ImmutableArray<string> CustomTemplates(ImmutableArray<TypedConstant> arguments)
    {
        var names = ImmutableArray.CreateBuilder<string>();

        foreach (var argument in arguments)
        {
            // Kind has to be tested first: reading Value on an array argument throws.
            if (argument.Kind == TypedConstantKind.Array)
            {
                if (argument.Values.IsDefaultOrEmpty) continue;

                foreach (var element in argument.Values)
                    if (element.Value is string name)
                        names.Add(name);
            }
            else if (argument.Value is string name)
            {
                names.Add(name);
            }
        }

        return names.ToImmutable();
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

    /// <summary>
    /// How many items the type holds, read the way the type actually spells it.
    /// <c>IReadOnlyDictionary{TKey, TValue}</c> needs no check of its own: it derives from
    /// <c>IReadOnlyCollection{T}</c> of its pairs, so implementing one implements the other.
    /// </summary>
    private static CountAccess CountAccessFor(ITypeSymbol type)
    {
        // An array satisfies IReadOnlyCollection<T> through the runtime rather than its own members.
        if (type is IArrayTypeSymbol) return new CountAccess("Length", null);

        // A dictionary needs no case of its own: IReadOnlyDictionary<TKey, TValue> derives from
        // IReadOnlyCollection<T> of its pairs, so the one interface answers for both.
        var countable = IsCountable(type) ? type : type.AllInterfaces.FirstOrDefault(IsCountable);
        if (countable is null) return default;

        // Implementing the interface is not the same as offering the member under a name we can write.
        // ImmutableArray<T> spells it Length and implements Count explicitly; others implement it
        // explicitly and offer nothing, and those are read back through the interface itself.
        if (Reads(type, "Count")) return new CountAccess("Count", null);
        if (Reads(type, "Length")) return new CountAccess("Length", null);

        return new CountAccess(null, countable.ToDisplayString(Formats.Type));
    }

    private static bool IsCountable(ITypeSymbol type) =>
        type.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IReadOnlyCollection_T;

    /// <summary>Whether the type offers <paramref name="name"/> as a public instance <c>int</c> property.</summary>
    private static bool Reads(ITypeSymbol type, string name) => Offers(type, candidate => Declares(candidate, name));

    /// <summary>
    /// Whether the member is there to be named, rather than only satisfying an interface. An explicit
    /// implementation is private to everything but a cast, so it never answers this.
    /// </summary>
    private static bool Offers(ITypeSymbol type, Func<ITypeSymbol, bool> declares)
    {
        // An interface-typed property reaches the member through the interfaces it inherits, not a base chain.
        if (type.TypeKind == TypeKind.Interface)
        {
            if (declares(type)) return true;

            foreach (var candidate in type.AllInterfaces)
                if (declares(candidate))
                    return true;

            return false;
        }

        for (var current = type; current is not null; current = current.BaseType)
            if (declares(current))
                return true;

        return false;
    }

    /// <summary>
    /// Whether the type declares exactly the <c>ToString(string, IFormatProvider)</c> that
    /// <c>ToString(null, null)</c> would bind to. A second two-parameter overload would make that call
    /// ambiguous, so a type carrying one is left to the cast.
    /// </summary>
    private static bool FormatsItself(ITypeSymbol type)
    {
        var found = false;

        foreach (var member in type.GetMembers("ToString"))
        {
            if (member is not IMethodSymbol
                {
                    IsStatic: false,
                    DeclaredAccessibility: Accessibility.Public,
                    Parameters.Length: 2,
                    ReturnType.SpecialType: SpecialType.System_String,
                } method)
                continue;

            if (method.Parameters[0].Type.SpecialType != SpecialType.System_String ||
                method.Parameters[1].Type.ToDisplayString(Formats.Match) != "System.IFormatProvider")
                return false;

            found = true;
        }

        return found;
    }

    private static bool Declares(ITypeSymbol type, string name)
    {
        foreach (var member in type.GetMembers(name))
            if (member is IPropertySymbol
                {
                    IsStatic: false,
                    DeclaredAccessibility: Accessibility.Public,
                    Type.SpecialType: SpecialType.System_Int32,
                })
                return true;

        return false;
    }

    private static ITypeSymbol Unwrap(ITypeSymbol type) =>
        IsNullableValue(type) ? ((INamedTypeSymbol)type).TypeArguments[0] : type;

    private static bool IsNullableValue(ITypeSymbol type) =>
        type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;

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
