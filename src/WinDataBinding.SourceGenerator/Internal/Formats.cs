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
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters);

    /// <summary>
    /// A symbol written as a doc-comment <c>cref</c>. A cref sits in an XML attribute, where an angle bracket
    /// is illegal, so a generic's parameters go in braces instead: <c>Base{T}</c>, never <c>Base&lt;T&gt;</c>.
    /// </summary>
    /// <remarks>
    /// The original definition, never the constructed type: a cref's type arguments have to be simple names,
    /// so <c>Base{Demo.Reading}</c> is rejected outright (CS1584) while <c>Base{T}</c> always binds.
    /// </remarks>
    public static string ToCref(ISymbol symbol) =>
        symbol.OriginalDefinition.ToDisplayString(Cref).Replace('<', '{').Replace('>', '}');

    /// <summary>Namespace-qualified, generics dropped, for matching against the known-type tables.</summary>
    public static readonly SymbolDisplayFormat Match = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.None);
}
