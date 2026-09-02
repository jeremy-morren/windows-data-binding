namespace WinDataBinding.SourceGenerator.Internal;

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
