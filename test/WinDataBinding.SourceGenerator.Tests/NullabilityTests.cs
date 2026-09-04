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

                    public global::Demo.Line Line => _source.Line;

                    public global::Demo.Money Line_Price => _source.Line.Price;

                    public decimal Line_Price_Amount => _source.Line.Price.Amount;

                    public global::Demo.Money? Maybe => _source.Maybe;

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
