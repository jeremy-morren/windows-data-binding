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
    private const string MapAttribute = "WinDataBinding.MapTypeAttribute";

    public static readonly GeneratorOptions Empty =
        new(new Dictionary<ISymbol, TypeMapping>(SymbolEqualityComparer.Default), []);

    private readonly Dictionary<ISymbol, TypeMapping> _mappings;

    /// <summary>The same mappings in declaration order, for the walk up a type's interfaces and bases.</summary>
    private readonly List<(ITypeSymbol Source, TypeMapping Mapping)> _byHierarchy;

    private GeneratorOptions(
        Dictionary<ISymbol, TypeMapping> mappings,
        List<(ITypeSymbol Source, TypeMapping Mapping)> byHierarchy)
    {
        _mappings = mappings;
        _byHierarchy = byHierarchy;
    }

    public static GeneratorOptions From(INamedTypeSymbol? optionsType, KnownTypeSymbols known)
    {
        if (optionsType is null) return Empty;

        var mappings = new Dictionary<ISymbol, TypeMapping>(SymbolEqualityComparer.Default);
        var byHierarchy = new List<(ITypeSymbol Source, TypeMapping Mapping)>();

        for (var current = optionsType;
             current is not null && current.SpecialType != SpecialType.System_Object;
             current = current.BaseType)
        {
            foreach (var attribute in current.GetAttributes())
            {
                if (attribute.AttributeClass?.ToDisplayString(Formats.Match) == MapAttribute)
                    AddMapping(attribute, mappings, byHierarchy);
            }
        }

        return new GeneratorOptions(mappings, byHierarchy);
    }

    /// <summary>The type declared in place of a wrapper, if there is one.</summary>
    public bool TryGetMapping(ITypeSymbol type, out TypeMapping mapping)
    {
        if (_mappings.TryGetValue(type, out mapping)) return true;

        // A mapping named on an interface or a base type stands for everything deriving from it. That is what
        // makes a whole family of wrappers reachable in one line — and for some families it is the only thing
        // that can reach them at all, their own attributes never having been written to the assembly.
        foreach (var (source, candidate) in _byHierarchy)
        {
            if (!DerivesFrom(type, source)) continue;

            mapping = candidate;
            return true;
        }

        mapping = default;
        return false;
    }

    private static bool DerivesFrom(ITypeSymbol type, ITypeSymbol target)
    {
        if (target.TypeKind == TypeKind.Interface)
        {
            foreach (var candidate in type.AllInterfaces)
                if (SymbolEqualityComparer.Default.Equals(candidate, target))
                    return true;

            return false;
        }

        if (target.TypeKind != TypeKind.Class) return false;

        for (var current = type.BaseType; current is not null; current = current.BaseType)
            if (SymbolEqualityComparer.Default.Equals(current, target))
                return true;

        return false;
    }

    private static void AddMapping(
        AttributeData attribute,
        Dictionary<ISymbol, TypeMapping> mappings,
        List<(ITypeSymbol Source, TypeMapping Mapping)> byHierarchy)
    {
        if (attribute.ConstructorArguments.Length < 3) return;

        if (attribute.ConstructorArguments[0].Value is not ITypeSymbol sourceType ||
            attribute.ConstructorArguments[1].Value is not ITypeSymbol targetType ||
            attribute.ConstructorArguments[2].Value is not string expression)
            return;

        // The most derived mapping for a type wins.
        if (mappings.ContainsKey(sourceType)) return;

        var mapping = new TypeMapping(targetType, expression);
        mappings.Add(sourceType, mapping);
        byHierarchy.Add((sourceType, mapping));
    }
}
