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
                [global::System.CodeDom.Compiler.GeneratedCode("WinDataBinding.SourceGenerator", "1.0.0.0")]
                partial class ModelBinder
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

                    /// <summary><c>Instant.ToDateTimeUtc()</c></summary>
                    /// <remarks><see cref="Demo.Model.Instant"/></remarks>
                    public global::System.DateTime Instant => _source.Instant.ToDateTimeUtc();

                    /// <summary><c>((global::System.IFormattable)Instant)?.ToString(null, null)</c></summary>
                    /// <remarks><see cref="Demo.Model.Instant"/></remarks>
                    public string? Instant_Formatted => ((global::System.IFormattable)_source.Instant)?.ToString(null, null);

                    /// <summary><c>Zone?.Id</c></summary>
                    /// <remarks><see cref="Demo.Model.Zone"/></remarks>
                    public string? Zone => _source.Zone?.Id;

                    /// <summary><c>OffsetDateTime.ToDateTimeOffset()</c></summary>
                    /// <remarks><see cref="Demo.Model.OffsetDateTime"/></remarks>
                    public global::System.DateTimeOffset OffsetDateTime => _source.OffsetDateTime.ToDateTimeOffset();

                    /// <summary><c>((global::System.IFormattable)OffsetDateTime)?.ToString(null, null)</c></summary>
                    /// <remarks><see cref="Demo.Model.OffsetDateTime"/></remarks>
                    public string? OffsetDateTime_Formatted => ((global::System.IFormattable)_source.OffsetDateTime)?.ToString(null, null);

                    /// <summary><c>Zoned.ToDateTimeOffset()</c></summary>
                    /// <remarks><see cref="Demo.Model.Zoned"/></remarks>
                    public global::System.DateTimeOffset Zoned_Value => _source.Zoned.ToDateTimeOffset();

                    /// <summary><c>Zoned.Zone.Id</c></summary>
                    /// <remarks><see cref="Demo.Model.Zoned"/></remarks>
                    public string? Zoned_Timezone => _source.Zoned.Zone.Id;

                    /// <summary><c>((global::System.IFormattable)Zoned)?.ToString(null, null)</c></summary>
                    /// <remarks><see cref="Demo.Model.Zoned"/></remarks>
                    public string? Zoned_Formatted => ((global::System.IFormattable)_source.Zoned)?.ToString(null, null);

                    /// <summary><c>LocalDateTime.ToDateTimeUnspecified()</c></summary>
                    /// <remarks><see cref="Demo.Model.LocalDateTime"/></remarks>
                    public global::System.DateTime LocalDateTime => _source.LocalDateTime.ToDateTimeUnspecified();

                    /// <summary><c>((global::System.IFormattable)LocalDateTime)?.ToString(null, null)</c></summary>
                    /// <remarks><see cref="Demo.Model.LocalDateTime"/></remarks>
                    public string? LocalDateTime_Formatted => ((global::System.IFormattable)_source.LocalDateTime)?.ToString(null, null);

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

                    /// <summary><c>((global::System.IFormattable)LocalDate)?.ToString(null, null)</c></summary>
                    /// <remarks><see cref="Demo.Model.LocalDate"/></remarks>
                    public string? LocalDate_Formatted => ((global::System.IFormattable)_source.LocalDate)?.ToString(null, null);

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

                    /// <summary><c>((global::System.IFormattable)LocalTime)?.ToString(null, null)</c></summary>
                    /// <remarks><see cref="Demo.Model.LocalTime"/></remarks>
                    public string? LocalTime_Formatted => ((global::System.IFormattable)_source.LocalTime)?.ToString(null, null);

                    /// <summary><c>Duration.ToTimeSpan()</c></summary>
                    /// <remarks><see cref="Demo.Model.Duration"/></remarks>
                    public global::System.TimeSpan Duration => _source.Duration.ToTimeSpan();

                    /// <summary><c>((global::System.IFormattable)Duration)?.ToString(null, null)</c></summary>
                    /// <remarks><see cref="Demo.Model.Duration"/></remarks>
                    public string? Duration_Formatted => ((global::System.IFormattable)_source.Duration)?.ToString(null, null);

                    /// <summary><c>Offset.ToTimeSpan()</c></summary>
                    /// <remarks><see cref="Demo.Model.Offset"/></remarks>
                    public global::System.TimeSpan Offset => _source.Offset.ToTimeSpan();

                    /// <summary><c>((global::System.IFormattable)Offset)?.ToString(null, null)</c></summary>
                    /// <remarks><see cref="Demo.Model.Offset"/></remarks>
                    public string? Offset_Formatted => ((global::System.IFormattable)_source.Offset)?.ToString(null, null);

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

                    /// <summary><c>((global::System.IFormattable)YearMonth)?.ToString(null, null)</c></summary>
                    /// <remarks><see cref="Demo.Model.YearMonth"/></remarks>
                    public string? YearMonth_Formatted => ((global::System.IFormattable)_source.YearMonth)?.ToString(null, null);

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
                [global::System.CodeDom.Compiler.GeneratedCode("WinDataBinding.SourceGenerator", "1.0.0.0")]
                partial class ModelBinder
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

                    /// <summary><c>((global::System.IFormattable)Inner?.Time)?.ToString(null, null)</c></summary>
                    /// <remarks><see cref="Demo.Model.Inner"/> <see cref="Demo.Inner.Time"/></remarks>
                    public string? Inner_Time_Formatted => ((global::System.IFormattable)_source.Inner?.Time)?.ToString(null, null);
                }
            }
            """;

        TestHarness.AssertGenerated(expected, source);
        TestHarness.AssertCompiles(source, Target.NetStandard20);
    }
}
