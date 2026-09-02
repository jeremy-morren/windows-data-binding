namespace WinDataBinding.SourceGenerator.Internal;

/// <summary>Factories for the <see cref="Conversion"/> shapes the type table is built from.</summary>
internal static class Conversions
{
    /// <summary>A plain member access appended to the chain, e.g. <c>.ToDateTimeUtc()</c>.</summary>
    public static Conversion Tail(string? suffix, string type, string tail, bool isReference = false) => new()
    {
        Suffix = suffix,
        Type = type,
        IsReference = isReference,
        Build = context => context.Safe + context.Accessor + tail,
    };

    /// <summary>
    /// A member reached through a cast, for a type that implements the declaring interface explicitly.
    /// </summary>
    public static Conversion Cast(string? suffix, string type, string target, string tail) => new()
    {
        Suffix = suffix,
        Type = type,
        Build = context => $"(({target}){context.Safe}){context.Accessor}{tail}",
    };

    /// <summary>A tail whose target type only exists on NET6+, so both branches are emitted.</summary>
    public static Conversion TfmTail(string? suffix, string type, string tail, string typePre6, string tailPre6) => new()
    {
        Suffix = suffix,
        Type = type,
        Build = context => context.Safe + context.Accessor + tail,
        TypePre6 = typePre6,
        BuildPre6 = context => context.Safe + context.Accessor + tailPre6,
    };

    /// <summary>
    /// A conversion that must test a flag before reading the value, e.g. <c>Interval.HasStart</c>.
    /// Inside the guarded branch the unchecked chain is used: reaching it proves every link was non-null.
    /// </summary>
    public static Conversion Guarded(string suffix, string type, string[] guards, string value) => new()
    {
        Suffix = suffix,
        Type = type,
        ForceNullable = true,
        Build = context =>
        {
            var conditions = new List<string>(guards.Length);
            for (var i = 0; i < guards.Length; i++)
            {
                // Only the first guard reads through the lifted chain, so only it compares against true.
                conditions.Add(i == 0
                    ? context.Safe + context.Accessor + guards[i] + (context.Nullable ? " == true" : "")
                    : (context.Nullable ? context.Unchecked : context.Safe) + context.Accessor + guards[i]);
            }

            var target = context.Nullable ? context.Unchecked : context.Safe;
            return $"{string.Join(" && ", conditions)} ? {target}{context.Accessor}{value} : null";
        },
    };

    /// <summary>
    /// <c>NodaTime.LocalTime</c> has no <c>ToTimeSpan()</c>, so pre-NET6 we go through <c>TickOfDay</c>.
    /// That needs a pattern match rather than a tail when the chain can be null.
    /// </summary>
    public static Conversion LocalTime() => new()
    {
        Type = "global::System.TimeOnly",
        Build = context => context.Safe + context.Accessor + "ToTimeOnly()",
        TypePre6 = "global::System.TimeSpan",
        BuildPre6 = context => context.Nullable
            ? $"{context.Safe}{context.Accessor}TickOfDay is {{ }} ticks ? global::System.TimeSpan.FromTicks(ticks) : null"
            : $"global::System.TimeSpan.FromTicks({context.Safe}{context.Accessor}TickOfDay)",
    };
}
