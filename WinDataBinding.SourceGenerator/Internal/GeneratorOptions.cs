using Microsoft.CodeAnalysis;

namespace WinDataBinding.SourceGenerator.Internal;

/// <summary>
/// A wrapper type rewritten as the type it wraps.
/// </summary>
/// <param name="TargetType">The type the wrapper stands for, which is classified in its place.</param>
/// <param name="Expression">
/// The member reaching the wrapped value, written out exactly as declared. It is never parsed or resolved:
/// whether it names a property, a field or a method is the consuming compilation's business, not ours.
/// </param>
internal readonly record struct TypeMapping(ITypeSymbol TargetType, string Expression);

/// <summary>
/// Configuration scraped off the options type named by the attribute.
/// The type need not derive from anything and is never instantiated;
/// only its attributes matter, and those on its base types count too.
/// </summary>
internal sealed class GeneratorOptions
{
    private const string SetupAttribute = "WinDataBinding.StrongIdTemplateSetupAttribute";
    private const string MapAttribute = "WinDataBinding.MapTypeAttribute";

    public static readonly GeneratorOptions Empty = new([], new Dictionary<ISymbol, TypeMapping>(SymbolEqualityComparer.Default));

    private readonly Dictionary<string, StrongIdBinding> _strongIdTemplates;
    private readonly Dictionary<ISymbol, TypeMapping> _mappings;

    private GeneratorOptions(
        Dictionary<string, StrongIdBinding> strongIdTemplates, Dictionary<ISymbol, TypeMapping> mappings)
    {
        _strongIdTemplates = strongIdTemplates;
        _mappings = mappings;
    }

    public static GeneratorOptions From(INamedTypeSymbol? optionsType, KnownTypeSymbols known)
    {
        if (optionsType is null) return Empty;

        var templates = new Dictionary<string, StrongIdBinding>(StringComparer.Ordinal);
        var mappings = new Dictionary<ISymbol, TypeMapping>(SymbolEqualityComparer.Default);

        for (var current = optionsType;
             current is not null && current.SpecialType != SpecialType.System_Object;
             current = current.BaseType)
        {
            foreach (var attribute in current.GetAttributes())
            {
                switch (attribute.AttributeClass?.ToDisplayString(Formats.Match))
                {
                    case SetupAttribute:
                        AddTemplate(attribute, templates);
                        break;
                    case MapAttribute:
                        AddMapping(attribute, mappings);
                        break;
                }
            }
        }

        return new GeneratorOptions(templates, mappings);
    }

    /// <summary>The conversion declared for a custom strongly typed ID template, if there is one.</summary>
    public bool TryGetStrongIdTemplate(string name, out StrongIdBinding binding) =>
        _strongIdTemplates.TryGetValue(name, out binding);

    /// <summary>The type declared in place of a wrapper, if there is one.</summary>
    public bool TryGetMapping(ITypeSymbol type, out TypeMapping mapping) =>
        _mappings.TryGetValue(type, out mapping);

    private static void AddTemplate(AttributeData attribute, Dictionary<string, StrongIdBinding> templates)
    {
        if (attribute.ConstructorArguments.Length < 3) return;

        if (attribute.ConstructorArguments[0].Value is not string name ||
            attribute.ConstructorArguments[1].Value is not ITypeSymbol valueType ||
            attribute.ConstructorArguments[2].Value is not string property)
            return;

        // The most derived setup for a template name wins.
        if (templates.ContainsKey(name)) return;

        // Whether the ID implements IFormattable cannot be checked here:
        // that part of the struct is written by StronglyTypedId's generator.
        // The setup declares it instead.
        var formattable = attribute.ConstructorArguments.Length < 4 ||
                          attribute.ConstructorArguments[3].Value is not bool declared || declared;

        templates.Add(name, new StrongIdBinding(
            valueType.ToDisplayString(Formats.Type),
            valueType.IsReferenceType,
            property,
            // The twin renders the ID itself, and the half of it that implements IFormattable is written by
            // another generator. Nothing here can see how, so it is reached through a cast.
            formattable ? Renderer.FormattableByCast : Renderer.None,
            RendersSelf: true));
    }

    private static void AddMapping(AttributeData attribute, Dictionary<ISymbol, TypeMapping> mappings)
    {
        if (attribute.ConstructorArguments.Length < 3) return;

        if (attribute.ConstructorArguments[0].Value is not ITypeSymbol sourceType ||
            attribute.ConstructorArguments[1].Value is not ITypeSymbol targetType ||
            attribute.ConstructorArguments[2].Value is not string expression)
            return;

        // The most derived mapping for a type wins.
        if (!mappings.ContainsKey(sourceType))
            mappings.Add(sourceType, new TypeMapping(targetType, expression));
    }
}
