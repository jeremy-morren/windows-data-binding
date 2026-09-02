using System.Collections.Immutable;

namespace WinDataBinding.SourceGenerator.Internal;

/// <summary>
/// What one binder flattens, in the order it emits: the properties drawn from its source object first, then those
/// drawn from the properties it declares by hand.
/// </summary>
/// <param name="FromSource">
/// How many of <paramref name="Properties"/> came from the source object. A binder nested inside another already
/// binds the declared half through its own members, so only this half is spliced in.
/// </param>
internal readonly record struct FlattenedBinder(ImmutableArray<GeneratedProperty> Properties, int FromSource);

/// <summary>
/// One generated property. When <see cref="TypePre6"/> is set the property is emitted twice, 
/// under <c>#if NET6_0_OR_GREATER</c>, because its type only exists on NET6+.
/// </summary>
internal sealed record GeneratedProperty(
    string Name,
    string Type,
    string Expression,
    string Summary,
    string? TypePre6,
    string? ExpressionPre6,
    string? SummaryPre6,
    EquatableArray<string> Remarks,
    string? Description);

/// <summary>Everything needed to emit one binding model, as value-equatable data only.</summary>
internal sealed record BinderModel(
    string? Namespace,
    EquatableArray<string> ContainingTypes,
    string Keyword,
    string ClassName,
    string SourceType,
    bool SourceIsReference,
    bool SourceIsComparable,
    string CtorAccessibility,
    string HintName,
    bool ContractAnnotation,
    bool NotNullIfNotNull,
    EquatableArray<GeneratedProperty> Properties,
    EquatableArray<DiagnosticInfo> Diagnostics);
