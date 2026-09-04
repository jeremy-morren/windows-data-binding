namespace WinDataBinding.SourceGenerator.Tests;

public class FormattedTests
{
    private const string Temperature = """
        public struct Temperature : System.IFormattable
        {
            public double Degrees { get; set; }
            public string ToString(string format, System.IFormatProvider provider) => "";
        }
        """;

    [Fact]
    public void Renders_a_formattable_value_at_the_root_and_inside_the_graph()
    {
        var source = TestSources.Wrap($$"""
            {{Temperature}}

            public class Inner { public Temperature Reading { get; set; } }

            public class Model
            {
                /// <summary>Outside air</summary>
                public Temperature Outside { get; set; }

                public Inner Inner { get; set; }
            }
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

                    /// <summary><c>Outside</c></summary>
                    /// <remarks><see cref="Demo.Model.Outside"/></remarks>
                    [global::System.ComponentModel.Description("Outside air")]
                    public global::Demo.Temperature Outside => _source.Outside;

                    /// <summary><c>Outside.Degrees</c></summary>
                    /// <remarks><see cref="Demo.Model.Outside"/> <see cref="Demo.Temperature.Degrees"/></remarks>
                    [global::System.ComponentModel.Description("Outside air")]
                    public double Outside_Degrees => _source.Outside.Degrees;

                    /// <summary><c>Outside.ToString(null, null)</c></summary>
                    /// <remarks><see cref="Demo.Model.Outside"/></remarks>
                    [global::System.ComponentModel.Description("Outside air (Formatted)")]
                    public string? Outside_Formatted => _source.Outside.ToString(null, null);

                    /// <summary><c>Inner</c></summary>
                    /// <remarks><see cref="Demo.Model.Inner"/></remarks>
                    public global::Demo.Inner? Inner => _source.Inner;

                    /// <summary><c>Inner?.Reading</c></summary>
                    /// <remarks><see cref="Demo.Model.Inner"/> <see cref="Demo.Inner.Reading"/></remarks>
                    public global::Demo.Temperature? Inner_Reading => _source.Inner?.Reading;

                    /// <summary><c>Inner?.Reading.Degrees</c></summary>
                    /// <remarks><see cref="Demo.Model.Inner"/> <see cref="Demo.Inner.Reading"/> <see cref="Demo.Temperature.Degrees"/></remarks>
                    public double? Inner_Reading_Degrees => _source.Inner?.Reading.Degrees;

                    /// <summary><c>Inner?.Reading.ToString(null, null)</c></summary>
                    /// <remarks><see cref="Demo.Model.Inner"/> <see cref="Demo.Inner.Reading"/></remarks>
                    public string? Inner_Reading_Formatted => _source.Inner?.Reading.ToString(null, null);
                }
            }
            """;

        TestHarness.AssertGenerated(expected, source);
    }

    [Fact]
    public void Leaves_primitives_and_enums_without_a_formatted_twin()
    {
        // int and DateTime are IFormattable, and every enum is too, but a grid renders those already.
        var source = TestSources.Wrap("""
            public enum Colour { Red, Green }

            public class Model
            {
                public int Count { get; set; }
                public System.DateTime When { get; set; }
                public decimal Total { get; set; }
                public Colour Colour { get; set; }
            }
            """);

        var result = TestHarness.AssertCompiles(source);

        result.Source.Should().NotContain("_Formatted");
    }

    [Fact]
    public void Widens_the_separator_when_formatted_collides_with_a_real_member()
    {
        // The real member is declared first, so it keeps the plain name and the generated one widens.
        var source = TestSources.Wrap($$"""
            {{Temperature}}

            public class Model
            {
                public string Reading_Formatted { get; set; }

                public Temperature Reading { get; set; }
            }
            """);

        var result = TestHarness.AssertCompiles(source);

        result.Source.Should().Contain("public string? Reading_Formatted => _source.Reading_Formatted;");
        result.Source.Should().Contain(
            "public string? Reading__Formatted => _source.Reading.ToString(null, null);");
    }

    [Fact]
    public void Asks_a_reference_type_for_its_text_through_the_lifted_chain()
    {
        var source = TestSources.Wrap("""
            public class Money : System.IFormattable
            {
                public string ToString(string format, System.IFormatProvider provider) => "";
            }

            public class Model { public Money Price { get; set; } }
            """);

        var result = TestHarness.AssertCompiles(source);

        // The method is public, so it is called directly; the property can be null, so the call lifts.
        result.Source.Should().Contain("public string? Price_Formatted => _source.Price?.ToString(null, null);");
    }

    [Fact]
    public void Casts_to_reach_a_formattable_implemented_explicitly()
    {
        var source = TestSources.Wrap("""
            public class Money : System.IFormattable
            {
                string System.IFormattable.ToString(string format, System.IFormatProvider provider) => "";
            }

            public class Model { public Money Price { get; set; } }
            """);

        var result = TestHarness.AssertCompiles(source);

        // Nothing here can be named without a cast, and the cast works either way.
        result.Source.Should().Contain(
            "public string? Price_Formatted => "
            + "((global::System.IFormattable)_source.Price)?.ToString(null, null);");
    }

    [Fact]
    public void Casts_rather_than_risk_an_ambiguous_call()
    {
        // A second two-parameter ToString would make ToString(null, null) ambiguous, so the cast picks the
        // overload for us.
        var source = TestSources.Wrap("""
            public class Money : System.IFormattable
            {
                public string ToString(string format, System.IFormatProvider provider) => "";
                public string ToString(string format, string culture) => "";
            }

            public class Model { public Money Price { get; set; } }
            """);

        var result = TestHarness.AssertCompiles(source);

        result.Source.Should().Contain(
            "public string? Price_Formatted => "
            + "((global::System.IFormattable)_source.Price)?.ToString(null, null);");
    }

    [Fact]
    public void Joins_a_sequence_of_formattable_elements_without_casting_each_one()
    {
        var source = TestSources.Wrap("""
            using System.Collections.Generic;

            public class Model { public List<int> Codes { get; set; } }
            """);

        var result = TestHarness.AssertCompiles(source);

        result.Source.Should().Contain("item => item.ToString(null, null)");
        result.Source.Should().NotContain("(global::System.IFormattable)item");
    }
}
