using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace WinDataBinding.SourceGenerator.Internal;

/// <summary>
/// A <see cref="Location"/> reduced to equatable data. <see cref="Location"/> itself must never enter a
/// pipeline model: it holds a <see cref="SyntaxTree"/> and breaks equality on every edit.
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
        "Binding model class must be partial",
        "Class '{0}' is marked with [GenerateWindowsBindingModel] and must be declared 'partial'",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor GenericType = new(
        "WGD003",
        "Generic types are not supported",
        "Type '{0}' is generic; [GenerateWindowsBindingModel] supports neither generic binding model classes nor generic source types",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ContainingTypeNotPartial = new(
        "WGD004",
        "Containing type must be partial",
        "Class '{0}' encloses a binding model class and must be declared 'partial'",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true);
}
