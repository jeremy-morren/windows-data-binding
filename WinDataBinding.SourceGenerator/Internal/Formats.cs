using Microsoft.CodeAnalysis;

namespace WinDataBinding.SourceGenerator.Internal;

/// <summary>The symbol display formats used across the generator.</summary>
internal static class Formats
{
    /// <summary>Fully qualified, with <c>global::</c> and language keywords, for emitted type names.</summary>
    public static readonly SymbolDisplayFormat Type = SymbolDisplayFormat.FullyQualifiedFormat;

    /// <summary>Namespace-qualified without <c>global::</c>, for XML doc <c>cref</c>s.</summary>
    public static readonly SymbolDisplayFormat Cref = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);

    /// <summary>Namespace-qualified, generics dropped, for matching against the known-type tables.</summary>
    public static readonly SymbolDisplayFormat Match = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.None);
}
