namespace WinDataBinding.SourceGenerator.Tests;

public class NodaTimeTests
{
    private static readonly string AllTypes = TestSources.Wrap("""
        public class Model
        {
            public Instant Instant { get; set; }
            public DateTimeZone Zone { get; set; }
            public OffsetDateTime OffsetDateTime { get; set; }
            public ZonedDateTime Zoned { get; set; }
            public LocalDateTime LocalDateTime { get; set; }
            public LocalDate LocalDate { get; set; }
            public LocalTime LocalTime { get; set; }
            public Duration Duration { get; set; }
            public Offset Offset { get; set; }
            public YearMonth YearMonth { get; set; }
            public Interval Interval { get; set; }
            public Period Period { get; set; }
            public System.TimeZoneInfo TimeZone { get; set; }
        }
        """);

    [Fact]
    public void Converts_every_type_in_the_table()
    {
        const string expected = """
            namespace Demo
            {
                [global::System.CodeDom.Compiler.GeneratedCode("WinDataBinding.SourceGenerator", "1.0.0")]
                partial class ModelBinder : global::System.IEquatable<ModelBinder>
                {
                    private readonly global::Demo.Model _source;

                    public ModelBinder(global::Demo.Model source)
                    {
            #if NET6_0_OR_GREATER
                        global::System.ArgumentNullException.ThrowIfNull(source);
                        _source = source;
            #else
                        _source = source ?? throw new global::System.ArgumentNullException(nameof(source));
            #endif
                    }

                    /// <summary>Wraps <paramref name="source"/>, or returns null when it is null.</summary>
                    [return: global::System.Diagnostics.CodeAnalysis.NotNullIfNotNull("source")]
                    public static ModelBinder? Create(global::Demo.Model? source) =>
                        source is not null ? new ModelBinder(source) : null;

                    /// <summary>Compares this binder to another for equality.</summary>
                    /// <remarks>Two binders are equal when the sources they wrap are.</remarks>
                    public bool Equals(ModelBinder? other) =>
                        other is not null && global::System.Collections.Generic.EqualityComparer<global::Demo.Model>.Default.Equals(_source, other._source);

                    /// <inheritdoc/>
                    public override bool Equals(object? obj) => obj is ModelBinder other && Equals(other);

                    /// <inheritdoc/>
                    public override int GetHashCode() => _source is null ? 0 : global::System.Collections.Generic.EqualityComparer<global::Demo.Model>.Default.GetHashCode(_source);

                    /// <summary><c>Instant.ToDateTimeUtc()</c></summary>
                    /// <remarks><see cref="Demo.Model.Instant"/></remarks>
                    public global::System.DateTime Instant => _source.Instant.ToDateTimeUtc();

                    /// <summary><c>Instant.ToString(null, null)</c></summary>
                    /// <remarks><see cref="Demo.Model.Instant"/></remarks>
                    public string? Instant_Formatted => _source.Instant.ToString(null, null);

                    /// <summary><c>Zone?.Id</c></summary>
                    /// <remarks><see cref="Demo.Model.Zone"/></remarks>
                    public string? Zone => _source.Zone?.Id;

                    /// <summary><c>OffsetDateTime.ToDateTimeOffset()</c></summary>
                    /// <remarks><see cref="Demo.Model.OffsetDateTime"/></remarks>
                    public global::System.DateTimeOffset OffsetDateTime => _source.OffsetDateTime.ToDateTimeOffset();

                    /// <summary><c>OffsetDateTime.ToInstant().ToDateTimeUtc()</c></summary>
                    /// <remarks><see cref="Demo.Model.OffsetDateTime"/></remarks>
                    public global::System.DateTime OffsetDateTime_Utc => _source.OffsetDateTime.ToInstant().ToDateTimeUtc();

                    /// <summary><c>OffsetDateTime.LocalDateTime.ToDateTimeUnspecified()</c></summary>
                    /// <remarks><see cref="Demo.Model.OffsetDateTime"/></remarks>
                    public global::System.DateTime OffsetDateTime_Local => _source.OffsetDateTime.LocalDateTime.ToDateTimeUnspecified();

                    /// <summary><c>OffsetDateTime.Offset.ToTimeSpan()</c></summary>
                    /// <remarks><see cref="Demo.Model.OffsetDateTime"/></remarks>
                    public global::System.TimeSpan OffsetDateTime_Offset => _source.OffsetDateTime.Offset.ToTimeSpan();

                    /// <summary><c>OffsetDateTime.ToString(null, null)</c></summary>
                    /// <remarks><see cref="Demo.Model.OffsetDateTime"/></remarks>
                    public string? OffsetDateTime_Formatted => _source.OffsetDateTime.ToString(null, null);

                    /// <summary><c>Zoned.ToDateTimeOffset()</c></summary>
                    /// <remarks><see cref="Demo.Model.Zoned"/></remarks>
                    public global::System.DateTimeOffset Zoned => _source.Zoned.ToDateTimeOffset();

                    /// <summary><c>Zoned.ToDateTimeUtc()</c></summary>
                    /// <remarks><see cref="Demo.Model.Zoned"/></remarks>
                    public global::System.DateTime Zoned_Utc => _source.Zoned.ToDateTimeUtc();

                    /// <summary><c>Zoned.ToDateTimeUnspecified()</c></summary>
                    /// <remarks><see cref="Demo.Model.Zoned"/></remarks>
                    public global::System.DateTime Zoned_Local => _source.Zoned.ToDateTimeUnspecified();

                    /// <summary><c>Zoned.Offset.ToTimeSpan()</c></summary>
                    /// <remarks><see cref="Demo.Model.Zoned"/></remarks>
                    public global::System.TimeSpan Zoned_Offset => _source.Zoned.Offset.ToTimeSpan();

                    /// <summary><c>Zoned.Zone.Id</c></summary>
                    /// <remarks><see cref="Demo.Model.Zoned"/></remarks>
                    public string? Zoned_Timezone => _source.Zoned.Zone.Id;

                    /// <summary><c>Zoned.ToString(null, null)</c></summary>
                    /// <remarks><see cref="Demo.Model.Zoned"/></remarks>
                    public string? Zoned_Formatted => _source.Zoned.ToString(null, null);

                    /// <summary><c>LocalDateTime.ToDateTimeUnspecified()</c></summary>
                    /// <remarks><see cref="Demo.Model.LocalDateTime"/></remarks>
                    public global::System.DateTime LocalDateTime => _source.LocalDateTime.ToDateTimeUnspecified();

                    /// <summary><c>LocalDateTime.ToString(null, null)</c></summary>
                    /// <remarks><see cref="Demo.Model.LocalDateTime"/></remarks>
                    public string? LocalDateTime_Formatted => _source.LocalDateTime.ToString(null, null);

            #if NET6_0_OR_GREATER
                    /// <summary><c>LocalDate.ToDateOnly()</c></summary>
            #else
                    /// <summary><c>LocalDate.ToDateTimeUnspecified()</c></summary>
            #endif
                    /// <remarks><see cref="Demo.Model.LocalDate"/></remarks>
            #if NET6_0_OR_GREATER
                    public global::System.DateOnly LocalDate => _source.LocalDate.ToDateOnly();
            #else
                    public global::System.DateTime LocalDate => _source.LocalDate.ToDateTimeUnspecified();
            #endif

                    /// <summary><c>LocalDate.ToString(null, null)</c></summary>
                    /// <remarks><see cref="Demo.Model.LocalDate"/></remarks>
                    public string? LocalDate_Formatted => _source.LocalDate.ToString(null, null);

            #if NET6_0_OR_GREATER
                    /// <summary><c>LocalTime.ToTimeOnly()</c></summary>
            #else
                    /// <summary><c>global::System.TimeSpan.FromTicks(LocalTime.TickOfDay)</c></summary>
            #endif
                    /// <remarks><see cref="Demo.Model.LocalTime"/></remarks>
            #if NET6_0_OR_GREATER
                    public global::System.TimeOnly LocalTime => _source.LocalTime.ToTimeOnly();
            #else
                    public global::System.TimeSpan LocalTime => global::System.TimeSpan.FromTicks(_source.LocalTime.TickOfDay);
            #endif

                    /// <summary><c>LocalTime.ToString(null, null)</c></summary>
                    /// <remarks><see cref="Demo.Model.LocalTime"/></remarks>
                    public string? LocalTime_Formatted => _source.LocalTime.ToString(null, null);

                    /// <summary><c>Duration.ToTimeSpan()</c></summary>
                    /// <remarks><see cref="Demo.Model.Duration"/></remarks>
                    public global::System.TimeSpan Duration => _source.Duration.ToTimeSpan();

                    /// <summary><c>Duration.ToString(null, null)</c></summary>
                    /// <remarks><see cref="Demo.Model.Duration"/></remarks>
                    public string? Duration_Formatted => _source.Duration.ToString(null, null);

                    /// <summary><c>Offset.ToTimeSpan()</c></summary>
                    /// <remarks><see cref="Demo.Model.Offset"/></remarks>
                    public global::System.TimeSpan Offset => _source.Offset.ToTimeSpan();

                    /// <summary><c>Offset.ToString(null, null)</c></summary>
                    /// <remarks><see cref="Demo.Model.Offset"/></remarks>
                    public string? Offset_Formatted => _source.Offset.ToString(null, null);

            #if NET6_0_OR_GREATER
                    /// <summary><c>YearMonth.OnDayOfMonth(1).ToDateOnly()</c></summary>
            #else
                    /// <summary><c>YearMonth.OnDayOfMonth(1).ToDateTimeUnspecified()</c></summary>
            #endif
                    /// <remarks><see cref="Demo.Model.YearMonth"/></remarks>
            #if NET6_0_OR_GREATER
                    public global::System.DateOnly YearMonth => _source.YearMonth.OnDayOfMonth(1).ToDateOnly();
            #else
                    public global::System.DateTime YearMonth => _source.YearMonth.OnDayOfMonth(1).ToDateTimeUnspecified();
            #endif

                    /// <summary><c>YearMonth.ToString(null, null)</c></summary>
                    /// <remarks><see cref="Demo.Model.YearMonth"/></remarks>
                    public string? YearMonth_Formatted => _source.YearMonth.ToString(null, null);

                    /// <summary><c>Interval.HasStart ? Interval.Start.ToDateTimeUtc() : null</c></summary>
                    /// <remarks><see cref="Demo.Model.Interval"/></remarks>
                    public global::System.DateTime? Interval_Start => _source.Interval.HasStart ? _source.Interval.Start.ToDateTimeUtc() : null;

                    /// <summary><c>Interval.HasEnd ? Interval.End.ToDateTimeUtc() : null</c></summary>
                    /// <remarks><see cref="Demo.Model.Interval"/></remarks>
                    public global::System.DateTime? Interval_End => _source.Interval.HasEnd ? _source.Interval.End.ToDateTimeUtc() : null;

                    /// <summary><c>Interval.HasStart &amp;&amp; Interval.HasEnd ? Interval.Duration.ToTimeSpan() : null</c></summary>
                    /// <remarks><see cref="Demo.Model.Interval"/></remarks>
                    public global::System.TimeSpan? Interval_Duration => _source.Interval.HasStart && _source.Interval.HasEnd ? _source.Interval.Duration.ToTimeSpan() : null;

                    /// <summary><c>Period?.ToString()</c></summary>
                    /// <remarks><see cref="Demo.Model.Period"/></remarks>
                    public string? Period => _source.Period?.ToString();

                    /// <summary><c>TimeZone?.Id</c></summary>
                    /// <remarks><see cref="Demo.Model.TimeZone"/></remarks>
                    public string? TimeZone_Id => _source.TimeZone?.Id;

                    /// <summary><c>TimeZone?.DisplayName</c></summary>
                    /// <remarks><see cref="Demo.Model.TimeZone"/></remarks>
                    public string? TimeZone_DisplayName => _source.TimeZone?.DisplayName;
                }
            }
            """;

        TestHarness.AssertGenerated(expected, AllTypes);
    }

    [Fact]
    public void Both_target_framework_branches_compile()
    {
        // The generator emits both sides of every #if, so each has to build against its own BCL.
        TestHarness.AssertCompiles(AllTypes, Target.Net8);
        TestHarness.AssertCompiles(AllTypes, Target.NetStandard20);
    }

    [Fact]
    public void Guards_and_lifts_conversions_reached_through_a_nullable_chain()
    {
        var source = TestSources.Wrap("""
            public class Inner
            {
                public Interval Interval { get; set; }
                public LocalTime Time { get; set; }
            }

            public class Model { public Inner Inner { get; set; } }
            """);

        const string expected = """
            namespace Demo
            {
                [global::System.CodeDom.Compiler.GeneratedCode("WinDataBinding.SourceGenerator", "1.0.0")]
                partial class ModelBinder : global::System.IEquatable<ModelBinder>
                {
                    private readonly global::Demo.Model _source;

                    public ModelBinder(global::Demo.Model source)
                    {
            #if NET6_0_OR_GREATER
                        global::System.ArgumentNullException.ThrowIfNull(source);
                        _source = source;
            #else
                        _source = source ?? throw new global::System.ArgumentNullException(nameof(source));
            #endif
                    }

                    /// <summary>Wraps <paramref name="source"/>, or returns null when it is null.</summary>
                    [return: global::System.Diagnostics.CodeAnalysis.NotNullIfNotNull("source")]
                    public static ModelBinder? Create(global::Demo.Model? source) =>
                        source is not null ? new ModelBinder(source) : null;

                    /// <summary>Compares this binder to another for equality.</summary>
                    /// <remarks>Two binders are equal when the sources they wrap are.</remarks>
                    public bool Equals(ModelBinder? other) =>
                        other is not null && global::System.Collections.Generic.EqualityComparer<global::Demo.Model>.Default.Equals(_source, other._source);

                    /// <inheritdoc/>
                    public override bool Equals(object? obj) => obj is ModelBinder other && Equals(other);

                    /// <inheritdoc/>
                    public override int GetHashCode() => _source is null ? 0 : global::System.Collections.Generic.EqualityComparer<global::Demo.Model>.Default.GetHashCode(_source);

                    /// <summary><c>Inner</c></summary>
                    /// <remarks><see cref="Demo.Model.Inner"/></remarks>
                    public global::Demo.Inner? Inner => _source.Inner;

                    /// <summary><c>Inner?.Interval.HasStart == true ? Inner.Interval.Start.ToDateTimeUtc() : null</c></summary>
                    /// <remarks><see cref="Demo.Model.Inner"/> <see cref="Demo.Inner.Interval"/></remarks>
                    public global::System.DateTime? Inner_Interval_Start => _source.Inner?.Interval.HasStart == true ? _source.Inner.Interval.Start.ToDateTimeUtc() : null;

                    /// <summary><c>Inner?.Interval.HasEnd == true ? Inner.Interval.End.ToDateTimeUtc() : null</c></summary>
                    /// <remarks><see cref="Demo.Model.Inner"/> <see cref="Demo.Inner.Interval"/></remarks>
                    public global::System.DateTime? Inner_Interval_End => _source.Inner?.Interval.HasEnd == true ? _source.Inner.Interval.End.ToDateTimeUtc() : null;

                    /// <summary><c>Inner?.Interval.HasStart == true &amp;&amp; Inner.Interval.HasEnd ? Inner.Interval.Duration.ToTimeSpan() : null</c></summary>
                    /// <remarks><see cref="Demo.Model.Inner"/> <see cref="Demo.Inner.Interval"/></remarks>
                    public global::System.TimeSpan? Inner_Interval_Duration => _source.Inner?.Interval.HasStart == true && _source.Inner.Interval.HasEnd ? _source.Inner.Interval.Duration.ToTimeSpan() : null;

            #if NET6_0_OR_GREATER
                    /// <summary><c>Inner?.Time.ToTimeOnly()</c></summary>
            #else
                    /// <summary><c>Inner?.Time.TickOfDay is { } ticks ? global::System.TimeSpan.FromTicks(ticks) : null</c></summary>
            #endif
                    /// <remarks><see cref="Demo.Model.Inner"/> <see cref="Demo.Inner.Time"/></remarks>
            #if NET6_0_OR_GREATER
                    public global::System.TimeOnly? Inner_Time => _source.Inner?.Time.ToTimeOnly();
            #else
                    public global::System.TimeSpan? Inner_Time => _source.Inner?.Time.TickOfDay is { } ticks ? global::System.TimeSpan.FromTicks(ticks) : null;
            #endif

                    /// <summary><c>Inner?.Time.ToString(null, null)</c></summary>
                    /// <remarks><see cref="Demo.Model.Inner"/> <see cref="Demo.Inner.Time"/></remarks>
                    public string? Inner_Time_Formatted => _source.Inner?.Time.ToString(null, null);
                }
            }
            """;

        TestHarness.AssertGenerated(expected, source);
        TestHarness.AssertCompiles(source, Target.NetStandard20);
    }

    [Theory]
    [InlineData(Target.Net8)]
    [InlineData(Target.NetStandard20)]
    public void Splits_an_ip_address_into_its_parts(Target target)
    {
        var source = TestSources.Wrap("""
            public class Model
            {
                /// <summary>Where it came from</summary>
                public System.Net.IPAddress Host { get; set; }
            }
            """);

        var result = TestHarness.AssertCompiles(source, target);

        result.Should().HaveNoDiagnostics();

        result.Source.Should().Contain("public global::System.Net.IPAddress? Host => _source.Host;");
        result.Source.Should().Contain("public string? Host_Formatted => _source.Host?.ToString();");

        result.Source.Should().Contain(
            "public global::System.Net.Sockets.AddressFamily? Host_AddressFamily => "
            + "_source.Host?.AddressFamily;");
        result.Source.Should().Contain(
            "public string? Host_AddressFamily_Formatted => _source.Host?.AddressFamily.ToString();");

        // IPAddress implements IFormattable explicitly on NET6+, and not at all before it. Either way the
        // automatic twin must not appear beside the one the table names.
        result.Source.Should().NotContain("Host__Formatted");
        result.Source.Should().NotContain("(global::System.IFormattable)_source.Host");

        // The description travels down every one of them.
        result.Source.Should().Contain(
            """[global::System.ComponentModel.Description("Where it came from")]""");
        result.Source.Should().Contain(
            """[global::System.ComponentModel.Description("Where it came from (AddressFamily)")]""");
    }

    [Fact]
    public void Flattens_an_ip_network_through_the_address_it_is_built_on()
    {
        // IPNetwork is NET8 and later, so this one target only.
        var source = TestSources.Wrap("""
            public class Model
            {
                /// <summary>The subnet</summary>
                public System.Net.IPNetwork Range { get; set; }
            }
            """);

        var result = TestHarness.AssertCompiles(source);

        result.Should().HaveNoDiagnostics();

        // The network itself takes the bare name.
        result.Source.Should().Contain("public global::System.Net.IPNetwork Range => _source.Range;");
        result.Source.Should().Contain("public int Range_PrefixLength => _source.Range.PrefixLength;");

        // BaseAddress lands on IPAddress, whose own entry takes over from there — the bare name of that
        // entry being the segment which reached it.
        result.Source.Should().Contain(
            "public global::System.Net.IPAddress? Range_BaseAddress => _source.Range.BaseAddress;");
        result.Source.Should().Contain(
            "public string? Range_BaseAddress_Formatted => _source.Range.BaseAddress?.ToString();");
        result.Source.Should().Contain(
            "public global::System.Net.Sockets.AddressFamily? Range_BaseAddress_AddressFamily => "
            + "_source.Range.BaseAddress?.AddressFamily;");
        result.Source.Should().Contain(
            "public string? Range_BaseAddress_AddressFamily_Formatted => "
            + "_source.Range.BaseAddress?.AddressFamily.ToString();");
        // IPNetwork implements IFormattable explicitly, so the ordinary rule reaches it through a cast.
        result.Source.Should().Contain(
            "public string? Range_Formatted => "
            + "((global::System.IFormattable)_source.Range)?.ToString(null, null);");

        // The description follows every one of them down.
        result.Source.Should().Contain(
            """[global::System.ComponentModel.Description("The subnet (PrefixLength)")]""");
        result.Source.Should().Contain(
            """[global::System.ComponentModel.Description("The subnet (BaseAddress)")]""");
        result.Source.Should().Contain(
            """[global::System.ComponentModel.Description("The subnet (BaseAddress_AddressFamily)")]""");
    }

    [Fact]
    public void Lifts_an_ip_network_reached_through_a_nullable_chain()
    {
        var source = TestSources.Wrap("""
            public class Inner { public System.Net.IPNetwork Range { get; set; } }

            public class Model { public Inner Inner { get; set; } }
            """);

        var result = TestHarness.AssertCompiles(source);

        result.Source.Should().Contain(
            "public global::System.Net.IPNetwork? Inner_Range => _source.Inner?.Range;");
        result.Source.Should().Contain(
            "public int? Inner_Range_PrefixLength => _source.Inner?.Range.PrefixLength;");
        result.Source.Should().Contain(
            "public global::System.Net.IPAddress? Inner_Range_BaseAddress => "
            + "_source.Inner?.Range.BaseAddress;");
    }
}
