using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace WinDataBinding.SourceGenerator.Internal;

/// <summary>
/// A <see cref="Location"/> reduced to equatable data. 
/// <see cref="Location"/> itself must never enter a pipeline model: it holds a <see cref="SyntaxTree"/> and breaks equality on every edit.
/// </summary>
internal sealed record LocationInfo(string FilePath, TextSpan Span, LinePositionSpan LineSpan)
{
    public static LocationInfo? From(Location location) =>
        location.SourceTree is null
            ? null
            : new LocationInfo(location.SourceTree.FilePath, location.SourceSpan, location.GetLineSpan().Span);

    public Location ToLocation() => Location.Create(FilePath, Span, LineSpan);
}

/// <summary>An equatable stand-in for <see cref="Diagnostic"/>, safe to carry in a pipeline model.</summary>
internal sealed record DiagnosticInfo(DiagnosticDescriptor Descriptor, LocationInfo? Location, EquatableArray<string> Arguments)
{
    public static DiagnosticInfo Create(DiagnosticDescriptor descriptor, LocationInfo? location, params string[] arguments) =>
        new(descriptor, location, EquatableArray.Create(arguments));

    public Diagnostic ToDiagnostic()
    {
        var args = new object?[Arguments.Count];
        for (var i = 0; i < Arguments.Count; i++) args[i] = Arguments[i];
        return Diagnostic.Create(Descriptor, Location?.ToLocation(), args);
    }
}

internal static class Diagnostics
{
    private const string Category = "WinDataBinding";

    public static readonly DiagnosticDescriptor CircularReference = new(
        "WGD001",
        "Circular reference skipped",
        "Property '{0}' is skipped because type '{1}' already appears in its property chain",
        Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NotPartial = new(
        "WGD002",
        "Binding model type must be partial",
        "The {0} '{1}' is marked with [GenerateWindowsBindingModel] and must be declared 'partial'",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor GenericType = new(
        "WGD003",
        "Open generic types are not supported",
        "The type '{0}' is an open generic; [GenerateWindowsBindingModel] needs a binding model type that is not generic, and a source type with its type arguments supplied",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ConstructorMustSetSource = new(
        "WGD006",
        "A hand-written constructor must set the source field",
        "This constructor of {0} '{1}' never assigns '_source', so every generated property will throw when it is read",
        Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ContainingTypeNotPartial = new(
        "WGD004",
        "Containing type must be partial",
        "The {0} '{1}' encloses a binding model and must be declared 'partial'",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true);
}
