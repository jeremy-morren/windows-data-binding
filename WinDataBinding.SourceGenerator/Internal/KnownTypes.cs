using static WinDataBinding.SourceGenerator.Internal.Conversions;

namespace WinDataBinding.SourceGenerator.Internal;

/// <summary>
/// The type table: which types are bound directly, and which need converting first. Types are matched by
/// namespace-qualified name, so no reference to NodaTime is required.
/// </summary>
/// <remarks>
/// Both tables are built once by the type initialiser and never written to afterwards, so the concurrent
/// reads Roslyn may make of them need no synchronisation. Keep them read-only.
/// </remarks>
internal static class KnownTypes
{
    /// <summary>Types bound directly, with no further traversal.</summary>
    private static readonly HashSet<string> LeafTypes = new(StringComparer.Ordinal)
    {
        "System.String", "System.Boolean", "System.Char",
        "System.Half", "System.Single", "System.Double", "System.Decimal",
        "System.Byte", "System.SByte", 
        "System.Int16", "System.Int32", "System.Int64", "System.Int128", 
        "System.UInt16", "System.UInt32", "System.UInt64", "System.UInt128",
        "System.Uri", "System.Guid", "System.Version",
        "System.DateTime", "System.DateTimeOffset", "System.TimeSpan",
        "System.DateOnly", "System.TimeOnly",
    };

    private static readonly Dictionary<string, Conversion[]> ConversionsByType = new(StringComparer.Ordinal)
    {
        ["System.TimeZoneInfo"] =
        [
            Tail("Id", "string", "Id", isReference: true),
            Tail("DisplayName", "string", "DisplayName", isReference: true),
        ],
        ["NodaTime.DateTimeZone"] = [Tail(null, "string", "Id", isReference: true)],
        ["NodaTime.Instant"] = [Tail(null, "global::System.DateTime", "ToDateTimeUtc()")],
        ["NodaTime.OffsetDateTime"] = [Tail(null, "global::System.DateTimeOffset", "ToDateTimeOffset()")],
        ["NodaTime.ZonedDateTime"] =
        [
            Tail("Value", "global::System.DateTimeOffset", "ToDateTimeOffset()"),
            Tail("Timezone", "string", "Zone.Id", isReference: true),
        ],
        ["NodaTime.LocalDateTime"] = [Tail(null, "global::System.DateTime", "ToDateTimeUnspecified()")],
        ["NodaTime.LocalDate"] =
        [
            TfmTail(null, "global::System.DateOnly", "ToDateOnly()",
                          "global::System.DateTime", "ToDateTimeUnspecified()"),
        ],
        ["NodaTime.LocalTime"] = [LocalTime()],
        ["NodaTime.Duration"] = [Tail(null, "global::System.TimeSpan", "ToTimeSpan()")],
        ["NodaTime.Offset"] = [Tail(null, "global::System.TimeSpan", "ToTimeSpan()")],
        ["NodaTime.YearMonth"] =
        [
            TfmTail(null, "global::System.DateOnly", "OnDayOfMonth(1).ToDateOnly()",
                          "global::System.DateTime", "OnDayOfMonth(1).ToDateTimeUnspecified()"),
        ],
        ["NodaTime.Interval"] =
        [
            Guarded("Start", "global::System.DateTime", ["HasStart"], "Start.ToDateTimeUtc()"),
            Guarded("End", "global::System.DateTime", ["HasEnd"], "End.ToDateTimeUtc()"),
            Guarded("Duration", "global::System.TimeSpan", ["HasStart", "HasEnd"], "Duration.ToTimeSpan()"),
        ],
        ["NodaTime.Period"] = [Tail(null, "string", "ToString()", isReference: true)],
    };

    /// <summary>
    /// The four built-in StronglyTypedId templates. The underlying type comes from the template rather than
    /// from the struct's own Value member: that member is written by another source generator, and generators
    /// cannot see each other's output.
    /// </summary>
    private static readonly Dictionary<string, StrongIdBinding> StrongIdTemplates = new(StringComparer.Ordinal)
    {
        ["Guid"] = new("global::System.Guid", false, "Value", Renderer.Formattable),
        ["Int"] = new("int", false, "Value", Renderer.Formattable),
        ["Long"] = new("long", false, "Value", Renderer.Formattable),
        // string is already text, so it gets no rendered twin.
        ["String"] = new("string", true, "Value", Renderer.None),
    };

    /// <summary>
    /// Renders a whole value as text. <c>JsonElement</c> has no <c>ToJsonString()</c>; <c>GetRawText()</c> is
    /// its equivalent.
    /// </summary>
    public static string RenderValue(Renderer renderer, string safe, string accessor) => renderer switch
    {
        Renderer.Formattable => $"((global::System.IFormattable){safe})?.ToString(null, null)",
        Renderer.JsonNode => $"{safe}{accessor}ToJsonString()",
        Renderer.JsonElement => $"{safe}{accessor}GetRawText()",
        _ => throw new ArgumentOutOfRangeException(nameof(renderer)),
    };

    /// <summary>Renders one element of a sequence, inside the lambda that joins them.</summary>
    public static string RenderElement(Renderer renderer, string item) => renderer switch
    {
        Renderer.Formattable => $"((global::System.IFormattable){item}).ToString(null, null)",
        Renderer.JsonNode => $"{item}?.ToJsonString()",
        Renderer.JsonElement => $"{item}.GetRawText()",
        _ => throw new ArgumentOutOfRangeException(nameof(renderer)),
    };

    public static bool IsLeaf(string fullName) => LeafTypes.Contains(fullName);

    public static bool TryGetStrongIdTemplate(string template, out StrongIdBinding binding) =>
        StrongIdTemplates.TryGetValue(template, out binding);

    public static bool TryGetConversions(string fullName, out Conversion[] conversions) =>
        ConversionsByType.TryGetValue(fullName, out conversions!);
}
