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

                    [return: global::System.Diagnostics.CodeAnalysis.NotNullIfNotNull("source")]
                    public static ModelBinder? Create(global::Demo.Model? source) =>
                        source is not null ? new ModelBinder(source) : null;

                    public bool Equals(ModelBinder? other) =>
                        other is not null && global::System.Collections.Generic.EqualityComparer<global::Demo.Model>.Default.Equals(_source, other._source);

                    public override bool Equals(object? obj) => obj is ModelBinder other && Equals(other);

                    public override int GetHashCode() => _source is null ? 0 : global::System.Collections.Generic.EqualityComparer<global::Demo.Model>.Default.GetHashCode(_source);

                    public global::System.DateTime Instant => _source.Instant.ToDateTimeUtc();

                    public string? Instant_Formatted => _source.Instant.ToString(null, null);

                    public string? Zone => _source.Zone?.Id;

                    public global::System.DateTimeOffset OffsetDateTime => _source.OffsetDateTime.ToDateTimeOffset();

                    public global::System.DateTime OffsetDateTime_Utc => _source.OffsetDateTime.ToInstant().ToDateTimeUtc();

                    public global::System.DateTime OffsetDateTime_Local => _source.OffsetDateTime.LocalDateTime.ToDateTimeUnspecified();

                    public global::System.TimeSpan OffsetDateTime_Offset => _source.OffsetDateTime.Offset.ToTimeSpan();

                    public string? OffsetDateTime_Formatted => _source.OffsetDateTime.ToString(null, null);

                    public global::System.DateTimeOffset Zoned => _source.Zoned.ToDateTimeOffset();

                    public global::System.DateTime Zoned_Utc => _source.Zoned.ToDateTimeUtc();

                    public global::System.DateTime Zoned_Local => _source.Zoned.ToDateTimeUnspecified();

                    public global::System.TimeSpan Zoned_Offset => _source.Zoned.Offset.ToTimeSpan();

                    public string? Zoned_Timezone => _source.Zoned.Zone.Id;

                    public string? Zoned_Formatted => _source.Zoned.ToString(null, null);

                    public global::System.DateTime LocalDateTime => _source.LocalDateTime.ToDateTimeUnspecified();

                    public string? LocalDateTime_Formatted => _source.LocalDateTime.ToString(null, null);

            #if NET6_0_OR_GREATER
                    public global::System.DateOnly LocalDate => _source.LocalDate.ToDateOnly();
            #else
                    public global::System.DateTime LocalDate => _source.LocalDate.ToDateTimeUnspecified();
            #endif

                    public string? LocalDate_Formatted => _source.LocalDate.ToString(null, null);

            #if NET6_0_OR_GREATER
                    public global::System.TimeOnly LocalTime => _source.LocalTime.ToTimeOnly();
            #else
                    public global::System.TimeSpan LocalTime => global::System.TimeSpan.FromTicks(_source.LocalTime.TickOfDay);
            #endif

                    public string? LocalTime_Formatted => _source.LocalTime.ToString(null, null);

                    public global::System.TimeSpan Duration => _source.Duration.ToTimeSpan();

                    public string? Duration_Formatted => _source.Duration.ToString(null, null);

                    public global::System.TimeSpan Offset => _source.Offset.ToTimeSpan();

                    public string? Offset_Formatted => _source.Offset.ToString(null, null);

            #if NET6_0_OR_GREATER
                    public global::System.DateOnly YearMonth => _source.YearMonth.OnDayOfMonth(1).ToDateOnly();
            #else
                    public global::System.DateTime YearMonth => _source.YearMonth.OnDayOfMonth(1).ToDateTimeUnspecified();
            #endif

                    public string? YearMonth_Formatted => _source.YearMonth.ToString(null, null);

                    public global::System.DateTime? Interval_Start => _source.Interval.HasStart ? _source.Interval.Start.ToDateTimeUtc() : null;

                    public global::System.DateTime? Interval_End => _source.Interval.HasEnd ? _source.Interval.End.ToDateTimeUtc() : null;

                    public global::System.TimeSpan? Interval_Duration => _source.Interval.HasStart && _source.Interval.HasEnd ? _source.Interval.Duration.ToTimeSpan() : null;

                    public string? Period => _source.Period?.ToString();

                    public string? TimeZone_Id => _source.TimeZone?.Id;

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

                    [return: global::System.Diagnostics.CodeAnalysis.NotNullIfNotNull("source")]
                    public static ModelBinder? Create(global::Demo.Model? source) =>
                        source is not null ? new ModelBinder(source) : null;

                    public bool Equals(ModelBinder? other) =>
                        other is not null && global::System.Collections.Generic.EqualityComparer<global::Demo.Model>.Default.Equals(_source, other._source);

                    public override bool Equals(object? obj) => obj is ModelBinder other && Equals(other);

                    public override int GetHashCode() => _source is null ? 0 : global::System.Collections.Generic.EqualityComparer<global::Demo.Model>.Default.GetHashCode(_source);

                    public global::Demo.Inner? Inner => _source.Inner;

                    public global::System.DateTime? Inner_Interval_Start => _source.Inner?.Interval.HasStart == true ? _source.Inner.Interval.Start.ToDateTimeUtc() : null;

                    public global::System.DateTime? Inner_Interval_End => _source.Inner?.Interval.HasEnd == true ? _source.Inner.Interval.End.ToDateTimeUtc() : null;

                    public global::System.TimeSpan? Inner_Interval_Duration => _source.Inner?.Interval.HasStart == true && _source.Inner.Interval.HasEnd ? _source.Inner.Interval.Duration.ToTimeSpan() : null;

            #if NET6_0_OR_GREATER
                    public global::System.TimeOnly? Inner_Time => _source.Inner?.Time.ToTimeOnly();
            #else
                    public global::System.TimeSpan? Inner_Time => _source.Inner?.Time.TickOfDay is { } ticks ? global::System.TimeSpan.FromTicks(ticks) : null;
            #endif

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

    [Fact]
    public void Binds_an_exception_to_its_display_text()
    {
        var source = TestSources.Wrap("""
            public class Model
            {
                /// <summary>What went wrong</summary>
                public System.Exception Error { get; set; }

                public System.InvalidOperationException Specific { get; set; }
            }
            """);

        var result = TestHarness.AssertCompiles(source);

        result.Should().HaveNoDiagnostics();

        result.Source.Should().Contain("public global::System.Exception? Error => _source.Error;");
        result.Source.Should().Contain("public string? Error_Message => _source.Error?.Message;");
        result.Source.Should().Contain("public string? Error_StackTrace => _source.Error?.StackTrace;");
        result.Source.Should().Contain("public string? Error_Display => _source.Error?.ToString();");

        // An entry stops the traversal, so none of the reflection graph behind TargetSite is reached.
        result.Source.Should().NotContain("TargetSite");
        result.Source.Should().NotContain("Error_InnerException");
        result.Source.Should().NotContain("Error_HResult");

        // The entry is written for the base, and a property declared as a derived exception is the usual
        // case, so the nearest base with an entry answers for it.
        result.Source.Should().Contain("public global::System.Exception? Specific => _source.Specific;");
        result.Source.Should().Contain("public string? Specific_Display => _source.Specific?.ToString();");
        result.Source.Should().Contain("public string? Specific_Message => _source.Specific?.Message;");

        result.Source.Should().Contain(
            """[global::System.ComponentModel.Description("What went wrong (Display)")]""");
    }

    [Fact]
    public void Adds_what_a_particular_exception_knows_to_what_every_exception_knows()
    {
        var source = TestSources.Wrap("""
            public class Model
            {
                public System.ArgumentNullException Missing { get; set; }
                public System.ArgumentOutOfRangeException Range { get; set; }
            }
            """);

        var result = TestHarness.AssertCompiles(source);

        result.Should().HaveNoDiagnostics();

        // ArgumentNullException derives from ArgumentException, whose row adds ParamName to Exception's.
        result.Source.Should().Contain("public string? Missing_Message => _source.Missing?.Message;");
        result.Source.Should().Contain("public string? Missing_ParamName => _source.Missing?.ParamName;");

        // And one more level down, all three rows apply.
        result.Source.Should().Contain("public string? Range_Display => _source.Range?.ToString();");
        result.Source.Should().Contain("public string? Range_ParamName => _source.Range?.ParamName;");
        result.Source.Should().Contain("public object? Range_ActualValue => _source.Range?.ActualValue;");
    }

    [Theory]
    [InlineData(Target.Net8)]
    [InlineData(Target.NetStandard20)]
    public void Reads_a_status_code_only_where_the_framework_has_one(Target target)
    {
        var source = TestSources.Wrap("""
            public class Model { public System.Net.Http.HttpRequestException Failure { get; set; } }
            """);

        var result = TestHarness.AssertCompiles(source, target);

        result.Should().HaveNoDiagnostics();

        // Every framework has these.
        result.Source.Should().Contain("public string? Failure_Message => _source.Failure?.Message;");

        // HttpRequestException.StatusCode arrived in NET5. Emitting it against netstandard2.0 would not
        // compile, so the row's own member is dropped there rather than guessed at.
        if (target == Target.Net8)
        {
            result.Source.Should().Contain(
                "public global::System.Net.HttpStatusCode? Failure_StatusCode => _source.Failure?.StatusCode;");
        }
        else
        {
            result.Source.Should().NotContain("StatusCode");
        }
    }
}
