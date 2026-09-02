using Microsoft.CodeAnalysis;

namespace WinDataBinding.SourceGenerator.Internal;

/// <summary>
/// Configuration scraped off the options type named by the attribute. 
/// The type need not derive from anything and is never instantiated; 
/// only its attributes matter, and those on its base types count too.
/// </summary>
internal sealed class GeneratorOptions
{
    private const string SetupAttribute = "WinDataBinding.StrongIdTemplateSetupAttribute";

    public static readonly GeneratorOptions Empty = new([]);

    private readonly Dictionary<string, StrongIdBinding> _strongIdTemplates;

    private GeneratorOptions(Dictionary<string, StrongIdBinding> strongIdTemplates) =>
        _strongIdTemplates = strongIdTemplates;

    public static GeneratorOptions From(INamedTypeSymbol? optionsType, KnownTypeSymbols known)
    {
        if (optionsType is null) return Empty;

        var templates = new Dictionary<string, StrongIdBinding>(StringComparer.Ordinal);

        for (var current = optionsType;
             current is not null && current.SpecialType != SpecialType.System_Object;
             current = current.BaseType)
        {
            foreach (var attribute in current.GetAttributes())
            {
                if (attribute.AttributeClass?.ToDisplayString(Formats.Match) != SetupAttribute) continue;
                if (attribute.ConstructorArguments.Length < 3) continue;

                if (attribute.ConstructorArguments[0].Value is not string name ||
                    attribute.ConstructorArguments[1].Value is not ITypeSymbol valueType ||
                    attribute.ConstructorArguments[2].Value is not string property)
                    continue;

                // The most derived setup for a template name wins.
                if (!templates.ContainsKey(name))
                {
                    // Whether the ID implements IFormattable cannot be checked here: 
                    // that part of the struct is written by StronglyTypedId's generator. 
                    // The setup declares it instead.
                    var formattable = attribute.ConstructorArguments.Length < 4 ||
                                      attribute.ConstructorArguments[3].Value is not bool declared || declared;

                    templates.Add(name, new StrongIdBinding(
                        valueType.ToDisplayString(Formats.Type), 
                        valueType.IsReferenceType, 
                        property,
                        formattable ? Renderer.Formattable : Renderer.None, 
                        RendersSelf: true));
                }
            }
        }

        return new GeneratorOptions(templates);
    }

    /// <summary>The conversion declared for a custom strongly typed ID template, if there is one.</summary>
    public bool TryGetStrongIdTemplate(string name, out StrongIdBinding binding) =>
        _strongIdTemplates.TryGetValue(name, out binding);
}
