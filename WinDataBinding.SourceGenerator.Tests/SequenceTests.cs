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

                    /// <summary><c>Totals?.Count</c></summary>
                    /// <remarks><see cref="Demo.Model.Totals"/></remarks>
                    [global::System.ComponentModel.Description("Line totals (Count)")]
                    public int? Totals_Count => _source.Totals?.Count;

                    /// <summary><c>Totals is { } items ? global::System.String.Join(", ", global::System.Linq.Enumerable.Select(items, item =&gt; item.ToString(null, null))) : null</c></summary>
                    /// <remarks><see cref="Demo.Model.Totals"/></remarks>
                    [global::System.ComponentModel.Description("Line totals (Display)")]
                    public string? Totals_Display => _source.Totals is { } items ? global::System.String.Join(", ", global::System.Linq.Enumerable.Select(items, item => item.ToString(null, null))) : null;

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
        // A plain class has no text form of its own to join.
        var source = TestSources.Wrap("""
            using System.Collections.Generic;

            public class Line { public int Quantity { get; set; } }

            public class Model { public List<Line> Lines { get; set; } }
            """);

        var result = TestHarness.AssertCompiles(source);

        result.Source.Should().NotContain("Lines_Display");
        result.Source.Should().Contain(
            "public global::System.Collections.Generic.List<global::Demo.Line>? Lines => _source.Lines;");

        // It is still a collection, so it still says how many.
        result.Source.Should().Contain("public int? Lines_Count => _source.Lines?.Count;");
    }

    [Fact]
    public void Joins_a_sequence_of_strings_as_they_stand()
    {
        // A string is already its own display text, so it is joined without being projected first.
        var source = TestSources.Wrap("""
            using System.Collections.Generic;

            public class Model { public List<string> Names { get; set; } }
            """);

        var result = TestHarness.AssertCompiles(source);

        result.Source.Should().Contain(
            "public string? Names_Display => _source.Names is { } items ? "
            + "global::System.String.Join(\", \", items) : null;");
        result.Source.Should().Contain(
            "public string? Names_Array => Names_Display is { } display ? $\"[{display}]\" : null;");
    }

    [Fact]
    public void Counts_what_can_say_how_many_it_holds()
    {
        var source = TestSources.Wrap("""
            using System.Collections.Generic;
            using System.Collections.Immutable;

            public interface IBag : IReadOnlyCollection<int>;

            public class Model
            {
                public int[] Codes { get; set; }
                public IReadOnlyDictionary<string, int> Totals { get; set; }
                public ImmutableArray<int> Frozen { get; set; }
                public ImmutableArray<int>? Maybe { get; set; }
                public IBag Bag { get; set; }
                public IEnumerable<int> Stream { get; set; }
            }
            """);

        var result = TestHarness.AssertCompiles(source);

        // An array satisfies IReadOnlyCollection<T> through the runtime, but spells the count Length.
        result.Source.Should().Contain("public int? Codes_Count => _source.Codes?.Length;");

        // A dictionary is a collection of its pairs, so it inherits the count.
        result.Source.Should().Contain("public int? Totals_Count => _source.Totals?.Count;");

        // A struct collection cannot be null, so neither can its count. ImmutableArray<T> implements
        // IReadOnlyCollection<T>.Count explicitly and offers Length instead, so Length is what is read.
        result.Source.Should().Contain("public int Frozen_Count => _source.Frozen.Length;");
        result.Source.Should().Contain("public int? Maybe_Count => _source.Maybe?.Length;");

        // An interface reaches Count through the one it inherits rather than through a base type.
        result.Source.Should().Contain("public int? Bag_Count => _source.Bag?.Count;");

        // IEnumerable<T> alone knows no length without walking it.
        result.Source.Should().NotContain("Stream_Count");
    }

    [Fact]
    public void Reads_a_count_through_a_cast_when_it_is_implemented_explicitly()
    {
        // An explicit implementation satisfies the interface without offering a member to name, so the
        // count is read back through the interface itself.
        var source = TestSources.Wrap("""
            using System.Collections;
            using System.Collections.Generic;

            public class Hidden : IReadOnlyCollection<int>
            {
                int IReadOnlyCollection<int>.Count => 0;
                public IEnumerator<int> GetEnumerator() => null;
                IEnumerator IEnumerable.GetEnumerator() => null;
            }

            public class Model { public Hidden Items { get; set; } }
            """);

        var result = TestHarness.AssertCompiles(source);

        result.Source.Should().Contain(
            "public int? Items_Count => "
            + "((global::System.Collections.Generic.IReadOnlyCollection<int>)_source.Items)?.Count;");
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
