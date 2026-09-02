namespace WinDataBinding.SourceGenerator.Tests;

/// <summary>
/// A source type may be generic as long as its type arguments are supplied. Roslyn hands back the members of
/// a constructed type with the substitution already applied, so the traversal sees concrete types throughout
/// and every rule that turns on a type — formattable, countable, leaf — lands on the substituted one.
/// </summary>
public class GenericSourceTests
{
    private const string Graph = """
        using System.Collections.Generic;
        using NodaTime;
        using WinDataBinding;

        namespace Demo;

        public class Reading : System.IFormattable
        {
            public int Depth { get; set; }
            public string ToString(string format, System.IFormatProvider provider) => "";
        }

        public class Base<T>
        {
            /// <summary>The current one</summary>
            public T Current { get; set; }

            public IReadOnlyList<T> History { get; set; }

            public IReadOnlyDictionary<string, T> ByName { get; set; }
        }
        """;

    [Fact]
    public void Substitutes_the_type_argument_through_a_base_class()
    {
        var source = $$"""
            {{Graph}}

            public sealed class Inherited : Base<Reading>
            {
                public string Label { get; set; }
            }

            [GenerateWindowsBindingModel(typeof(Inherited))]
            public sealed partial class InheritedBinder { }
            """;

        var result = TestHarness.AssertCompiles(source);

        result.Should().HaveNoDiagnostics();

        // The member declared as T comes back as Reading, and is flattened like any other object graph.
        result.Source.Should().Contain("public global::Demo.Reading? Current => _source.Current;");
        result.Source.Should().Contain("public int? Current_Depth => _source.Current?.Depth;");

        // Reading is formattable, so the substituted member gets the twin, called directly.
        result.Source.Should().Contain(
            "public string? Current_Formatted => _source.Current?.ToString(null, null);");

        // IReadOnlyList<T> becomes IReadOnlyList<Reading>: countable, and renderable element by element.
        result.Source.Should().Contain(
            "public global::System.Collections.Generic.IReadOnlyList<global::Demo.Reading>? History "
            + "=> _source.History;");
        result.Source.Should().Contain("public int? History_Count => _source.History?.Count;");
        // Reading is a class, so a null element renders as an empty entry rather than throwing.
        result.Source.Should().Contain("item => item?.ToString(null, null)");

        // A dictionary of T counts, but its pairs have no text form to join.
        result.Source.Should().Contain("public int? ByName_Count => _source.ByName?.Count;");
        result.Source.Should().NotContain("ByName_Display");

        // The derived type's own members come first, and the base's summaries still travel.
        result.Source.Should().Contain("public string? Label => _source.Label;");
        result.Source.Should().Contain("""[global::System.ComponentModel.Description("The current one")]""");

        // A cref lives in an XML attribute, where an angle bracket is illegal, so the type arguments go in
        // braces. The member is declared on the constructed base, so that is what the cref names.
        result.Source.Should().Contain("""<see cref="Demo.Base{T}.Current"/>""");
        result.Source.Should().Contain("""<see cref="Demo.Inherited.Label"/>""");
        result.Source.Should().NotContain("cref=\"Demo.Base<");
    }

    [Fact]
    public void Binds_a_closed_generic_named_by_the_attribute()
    {
        var source = $$"""
            {{Graph}}

            [GenerateWindowsBindingModel(typeof(Base<Reading>))]
            public sealed partial class ReadingBinder { }
            """;

        var result = TestHarness.AssertCompiles(source);

        result.Should().HaveNoDiagnostics();

        result.Source.Should().Contain("private readonly global::Demo.Base<global::Demo.Reading> _source;");
        result.Source.Should().Contain("public global::Demo.Reading? Current => _source.Current;");
        result.Source.Should().Contain("public int? Current_Depth => _source.Current?.Depth;");
        result.Source.Should().Contain("public int? History_Count => _source.History?.Count;");
        result.Source.Should().Contain("""<see cref="Demo.Base{T}.Current"/>""");
    }

    [Fact]
    public void Substitutes_a_type_argument_that_is_a_value_type()
    {
        // A struct argument changes what every rule decides: no lifting, and a leaf rather than a graph.
        var source = $$"""
            {{Graph}}

            public sealed class Counted : Base<int> { }

            [GenerateWindowsBindingModel(typeof(Counted))]
            public sealed partial class CountedBinder { }
            """;

        var result = TestHarness.AssertCompiles(source);

        result.Should().HaveNoDiagnostics();

        result.Source.Should().Contain("public int Current => _source.Current;");

        // int is a leaf, so it is bound as it stands with no twin of its own.
        result.Source.Should().NotContain("Current_Formatted");

        // As an element it still formats itself, so the sequence renders.
        result.Source.Should().Contain("public int? History_Count => _source.History?.Count;");
        result.Source.Should().Contain("public string? History_Display =>");
    }

    [Fact]
    public void Substitutes_a_type_argument_that_needs_converting()
    {
        var source = $$"""
            {{Graph}}

            public sealed class Stamped : Base<Instant> { }

            [GenerateWindowsBindingModel(typeof(Stamped))]
            public sealed partial class StampedBinder { }
            """;

        var result = TestHarness.AssertCompiles(source);

        result.Should().HaveNoDiagnostics();

        // The conversion table is reached through the substituted type, not the type parameter.
        result.Source.Should().Contain(
            "public global::System.DateTime Current => _source.Current.ToDateTimeUtc();");
        result.Source.Should().Contain(
            "public string? Current_Formatted => _source.Current.ToString(null, null);");
    }
}
