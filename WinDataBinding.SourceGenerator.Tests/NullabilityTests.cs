namespace WinDataBinding.SourceGenerator.Tests;

public class NullabilityTests
{
    [Fact]
    public void Keeps_a_chain_of_structs_non_nullable()
    {
        // _source is null-checked in the constructor and every link is a non-nullable struct, so nothing
        // in this chain can be null: no lifting, no null-conditional accessors.
        var source = TestSources.Wrap("""
            public struct Money { public decimal Amount { get; set; } }

            public struct Line { public Money Price { get; set; } }

            public class Model
            {
                public Line Line { get; set; }

                public Money? Maybe { get; set; }
            }
            """);

        const string expected = """
            namespace Demo
            {
                [global::System.CodeDom.Compiler.GeneratedCode("WinDataBinding.SourceGenerator", "1.0.0.0")]
                partial class ModelBinder : global::System.IEquatable<ModelBinder>, global::System.Collections.Generic.IEqualityComparer<global::Demo.Model>
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

                    /// <summary>Compares two sources with the default comparer for their type.</summary>
                    bool global::System.Collections.Generic.IEqualityComparer<global::Demo.Model>.Equals(global::Demo.Model? x, global::Demo.Model? y) => global::System.Collections.Generic.EqualityComparer<global::Demo.Model>.Default.Equals(x, y);

                    /// <summary>Hashes a source with the default comparer for its type.</summary>
                    int global::System.Collections.Generic.IEqualityComparer<global::Demo.Model>.GetHashCode(global::Demo.Model obj) => global::System.Collections.Generic.EqualityComparer<global::Demo.Model>.Default.GetHashCode(obj);

                    /// <summary><c>Line</c></summary>
                    /// <remarks><see cref="Demo.Model.Line"/></remarks>
                    public global::Demo.Line Line => _source.Line;

                    /// <summary><c>Line.Price</c></summary>
                    /// <remarks><see cref="Demo.Model.Line"/> <see cref="Demo.Line.Price"/></remarks>
                    public global::Demo.Money Line_Price => _source.Line.Price;

                    /// <summary><c>Line.Price.Amount</c></summary>
                    /// <remarks><see cref="Demo.Model.Line"/> <see cref="Demo.Line.Price"/> <see cref="Demo.Money.Amount"/></remarks>
                    public decimal Line_Price_Amount => _source.Line.Price.Amount;

                    /// <summary><c>Maybe</c></summary>
                    /// <remarks><see cref="Demo.Model.Maybe"/></remarks>
                    public global::Demo.Money? Maybe => _source.Maybe;

                    /// <summary><c>Maybe?.Amount</c></summary>
                    /// <remarks><see cref="Demo.Model.Maybe"/> <see cref="Demo.Money.Amount"/></remarks>
                    public decimal? Maybe_Amount => _source.Maybe?.Amount;
                }
            }
            """;

        TestHarness.AssertGenerated(expected, source);
    }

    [Fact]
    public void Lifts_everything_after_the_first_reference_type()
    {
        // Nullable annotations are ignored: a reference type is treated as nullable whatever it claims.
        var source = TestSources.Wrap("""
            public struct Point { public int X { get; set; } }

            public class Leaf { public Point Point { get; set; } }

            public class Node { public Point Point { get; set; } public Leaf Leaf { get; set; } }

            public class Model { public Node Node { get; set; } }
            """);

        var result = TestHarness.AssertCompiles(source);

        // One '?.' per reference-typed link; the struct in between needs none.
        result.Source.Should().Contain("public int? Node_Point_X => _source.Node?.Point.X;");
        result.Source.Should().Contain("public int? Node_Leaf_Point_X => _source.Node?.Leaf?.Point.X;");
    }
}
