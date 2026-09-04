namespace WinDataBinding.SourceGenerator.Tests;

public class EqualityTests
{
    [Fact]
    public void Equates_binders_by_the_sources_they_wrap()
    {
        var source = TestSources.Wrap("public class Model { public int Value { get; set; } }");

        var result = TestHarness.AssertCompiles(source);

        result.Source.Should().Contain("global::System.IEquatable<ModelBinder>");
        result.Source.Should().Contain("public bool Equals(ModelBinder? other) =>");
        result.Source.Should().Contain(
            "other is not null && global::System.Collections.Generic.EqualityComparer<global::Demo.Model>.Default.Equals(_source, other._source);");

        // Without these the two notions of equality could disagree.
        result.Source.Should().Contain(
            "public override bool Equals(object? obj) => obj is ModelBinder other && Equals(other);");
        result.Source.Should().Contain("public override int GetHashCode() =>");
    }

    [Fact]
    public void Compares_binders_when_the_source_is_comparable()
    {
        var source = TestSources.Wrap("""
            public class Model : System.IComparable<Model>
            {
                public int Value { get; set; }
                public int CompareTo(Model other) => 0;
            }
            """);

        var result = TestHarness.AssertCompiles(source);

        result.Source.Should().Contain("global::System.IComparable<ModelBinder>");
        result.Source.Should().Contain("public int CompareTo(ModelBinder? other) =>");
        result.Source.Should().Contain(
            "other is null ? 1 : global::System.Collections.Generic.Comparer<global::Demo.Model>.Default.Compare(_source, other._source);");
    }

    [Fact]
    public void Compares_binders_when_the_source_implements_only_the_non_generic_interface()
    {
        var source = TestSources.Wrap("""
            public class Model : System.IComparable
            {
                public int Value { get; set; }
                public int CompareTo(object obj) => 0;
            }
            """);

        var result = TestHarness.AssertCompiles(source);

        result.Source.Should().Contain("global::System.IComparable<ModelBinder>");
    }

    [Fact]
    public void Leaves_out_comparison_when_the_source_cannot_order_itself()
    {
        // Comparable to something else entirely is no use: Comparer<Model>.Default would throw.
        var source = TestSources.Wrap("""
            public class Other { }

            public class Model : System.IComparable<Other>
            {
                public int Value { get; set; }
                public int CompareTo(Other other) => 0;
            }
            """);

        var result = TestHarness.AssertCompiles(source);

        result.Source.Should().NotContain("IComparable");
        result.Source.Should().NotContain("CompareTo");
    }

    [Fact]
    public void Gives_a_struct_binder_value_equality_and_ordering()
    {
        var source = """
            using WinDataBinding;

            namespace Demo;

            public class Model : System.IComparable<Model>
            {
                public int Value { get; set; }
                public int CompareTo(Model other) => 0;
            }

            [GenerateWindowsBindingModel(typeof(Model))]
            public readonly partial struct ModelBinder;
            """;

        var result = TestHarness.AssertCompiles(source);

        result.Source.Should().Contain("readonly partial struct ModelBinder");

        // A struct binder takes its counterpart by value, so neither side needs a null check.
        result.Source.Should().Contain("public bool Equals(ModelBinder other) =>");
        result.Source.Should().Contain("public int CompareTo(ModelBinder other) =>");
        result.Source.Should().NotContain("other is not null");
        result.Source.Should().NotContain("other is null ? 1");
    }

    [Fact]
    public void Guards_the_hash_of_a_default_struct_binder()
    {
        var source = """
            using WinDataBinding;

            namespace Demo;

            public class Model { public int Value { get; set; } }

            [GenerateWindowsBindingModel(typeof(Model))]
            public readonly partial struct ModelBinder;
            """;

        var result = TestHarness.AssertCompiles(source);

        // default(ModelBinder) has no source, and the default comparer will not hash null.
        result.Source.Should().Contain("public override int GetHashCode() => _source is null ? 0 :");
    }
}
