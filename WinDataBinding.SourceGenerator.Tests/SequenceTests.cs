namespace WinDataBinding.SourceGenerator.Tests;

public class SequenceTests
{
    [Fact]
    public void Renders_a_sequence_of_formattable_elements()
    {
        var source = TestSources.Wrap("""
            using System.Collections.Generic;

            public class Model
            {
                /// <summary>Line totals</summary>
                public List<decimal> Totals { get; set; }
            }
            """);

        const string expected = """
            namespace Demo
            {
                [global::System.CodeDom.Compiler.GeneratedCode("WinDataBinding.SourceGenerator", "1.0.0.0")]
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

                    /// <summary><c>Totals</c></summary>
                    /// <remarks><see cref="Demo.Model.Totals"/></remarks>
                    [global::System.ComponentModel.Description("Line totals")]
                    public global::System.Collections.Generic.List<decimal>? Totals => _source.Totals;

                    /// <summary><c>Totals is { } items ? global::System.String.Join(", ", global::System.Linq.Enumerable.Select(items, item =&gt; ((global::System.IFormattable)item).ToString(null, null))) : null</c></summary>
                    /// <remarks><see cref="Demo.Model.Totals"/></remarks>
                    [global::System.ComponentModel.Description("Line totals (Display)")]
                    public string? Totals_Display => _source.Totals is { } items ? global::System.String.Join(", ", global::System.Linq.Enumerable.Select(items, item => ((global::System.IFormattable)item).ToString(null, null))) : null;

                    /// <summary><c>Totals_Display is { } display ? $"[{display}]" : null</c></summary>
                    /// <remarks><see cref="Demo.Model.Totals"/></remarks>
                    [global::System.ComponentModel.Description("Line totals (Array)")]
                    public string? Totals_Array => Totals_Display is { } display ? $"[{display}]" : null;
                }
            }
            """;

        TestHarness.AssertGenerated(expected, source);
    }

    [Fact]
    public void Leaves_a_sequence_of_unformattable_elements_alone()
    {
        // string is not IFormattable, and neither is a plain class.
        var source = TestSources.Wrap("""
            using System.Collections.Generic;

            public class Line { public int Quantity { get; set; } }

            public class Model
            {
                public List<string> Names { get; set; }
                public List<Line> Lines { get; set; }
            }
            """);

        var result = TestHarness.AssertCompiles(source);

        result.Source.Should().NotContain("Names_Display");
        result.Source.Should().NotContain("Lines_Display");
        result.Source.Should().Contain("public global::System.Collections.Generic.List<string>? Names => _source.Names;");
    }

    [Fact]
    public void Renders_a_sequence_reached_through_a_nullable_chain()
    {
        var source = TestSources.Wrap("""
            using System.Collections.Generic;

            public class Inner { public int[] Codes { get; set; } }

            public class Model { public Inner Inner { get; set; } }
            """);

        var result = TestHarness.AssertCompiles(source);

        result.Source.Should().Contain(
            "public string? Inner_Codes_Display => _source.Inner?.Codes is { } items ?");
        result.Source.Should().Contain(
            "public string? Inner_Codes_Array => Inner_Codes_Display is { } display ? $\"[{display}]\" : null;");
    }
}
