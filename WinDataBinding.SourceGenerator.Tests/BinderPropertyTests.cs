namespace WinDataBinding.SourceGenerator.Tests;

/// <summary>
/// Properties the binder declares by hand are flattened just like the source object's, rooted at <c>this</c>.
/// The declared property itself is never re-emitted: it is already there.
/// </summary>
public class BinderPropertyTests
{
    /// <summary>Wraps declarations and gives the binder a hand-written body.</summary>
    private static string Wrap(string body, string binderBody) => $$"""
        using System.Collections.Generic;
        using NodaTime;
        using WinDataBinding;

        namespace Demo;

        {{body}}

        [GenerateWindowsBindingModel(typeof(Model))]
        public sealed partial class ModelBinder
        {
        {{binderBody}}
        }
        """;

    [Fact]
    public void Flattens_an_object_graph_the_binder_declares()
    {
        var source = Wrap("""
            public class Engine
            {
                public int Rpm { get; set; }
                public string Label { get; set; }
            }

            public class Model { public int Id { get; set; } }
            """,
            "    public Engine? Speed { get; set; }");

        var result = TestHarness.AssertCompiles(source);

        result.Source.Should().Contain("public int? Speed_Rpm => this.Speed?.Rpm;");
        result.Source.Should().Contain("public string? Speed_Label => this.Speed?.Label;");

        // The declared property is the root, and it already exists.
        result.Source.Should().NotContain("Speed => ");
    }

    [Fact]
    public void Reaches_through_a_non_nullable_declared_property_without_lifting()
    {
        var source = Wrap("""
            public struct Point
            {
                public int X { get; set; }
                public int Y { get; set; }
            }

            public class Model { public int Id { get; set; } }
            """,
            "    public Point Origin { get; set; }");

        var result = TestHarness.AssertCompiles(source);

        result.Source.Should().Contain("public int Origin_X => this.Origin.X;");
        result.Source.Should().Contain("public int Origin_Y => this.Origin.Y;");
    }

    [Fact]
    public void Ignores_a_declared_property_with_nothing_to_flatten()
    {
        var source = Wrap(
            "public class Model { public int Id { get; set; } }",
            """
                public bool IsSelected { get; set; }
                public bool? IsExpanded { get; set; }
                public string? Note { get; set; }
                public int Rank { get; set; }
                public System.DayOfWeek Day { get; set; }
            """);

        var result = TestHarness.AssertCompiles(source);

        // A simple value is already bindable as it stands, so nothing is generated from it.
        result.Source.Should().NotContain("IsSelected");
        result.Source.Should().NotContain("IsExpanded");
        result.Source.Should().NotContain("Note");
        result.Source.Should().NotContain("Rank");
        result.Source.Should().NotContain("Day");
    }

    [Fact]
    public void Names_a_declared_strong_ids_value_rather_than_leaving_it_bare()
    {
        // Standing alone on the source the value would take the bare name; here that name is taken.
        var source = $$"""
            using StronglyTypedIds;
            using WinDataBinding;

            namespace StronglyTypedIds
            {
                public enum Template { Guid, Int, String, Long }

                [System.AttributeUsage(System.AttributeTargets.Struct)]
                public sealed class StronglyTypedIdAttribute : System.Attribute
                {
                    public StronglyTypedIdAttribute(Template template, params string[] templateNames) { }
                    public StronglyTypedIdAttribute(params string[] templateNames) { }
                }
            }

            namespace Demo
            {
                [StronglyTypedId(Template.Guid)]
                public readonly partial struct OrderId { public System.Guid Value { get; } }

                public class Model { public int Id { get; set; } }

                [GenerateWindowsBindingModel(typeof(Model))]
                public sealed partial class ModelBinder
                {
                    public OrderId? Order { get; set; }
                }
            }
            """;

        var result = TestHarness.AssertCompiles(source);

        result.Should().HaveNoDiagnostics();
        result.Source.Should().Contain("public global::System.Guid? Order_Value => this.Order?.Value;");
        result.Source.Should().Contain(
            "public string? Order_Value_Formatted => this.Order?.Value.ToString(null, null);");
    }

    [Fact]
    public void Names_a_declared_conversions_value_rather_than_leaving_it_bare()
    {
        var source = Wrap(
            "public class Model { public int Id { get; set; } }",
            """
                public Duration? Elapsed { get; set; }
                public System.TimeZoneInfo? Zone { get; set; }
            """);

        var result = TestHarness.AssertCompiles(source);

        // Duration converts to a single unnamed value, which takes the _Value segment here.
        result.Source.Should().Contain(
            "public global::System.TimeSpan? Elapsed_Value => this.Elapsed?.ToTimeSpan();");

        // TimeZoneInfo names its own, so nothing changes for it.
        result.Source.Should().Contain("public string? Zone_Id => this.Zone?.Id;");
        result.Source.Should().Contain("public string? Zone_DisplayName => this.Zone?.DisplayName;");
    }

    [Fact]
    public void Keeps_a_mapped_value_of_any_shape_under_the_value_segment()
    {
        // A mapping yields something the declared property does not already give you, whatever it maps to,
        // so it is kept under _Value rather than dropped along with the name it cannot have.
        var source = """
            using System.Collections.Generic;
            using WinDataBinding;

            namespace Demo;

            public class Address { public string Street { get; set; } }

            public class Tags { public List<int> Values { get; } }

            public class Boxed { public Address Unwrap() => null; }

            [MapType(typeof(Tags), typeof(List<int>), "Values")]
            [MapType(typeof(Boxed), typeof(Address), "Unwrap()")]
            public class BindingOptions;

            public class Model { public int Id { get; set; } }

            [GenerateWindowsBindingModel(typeof(Model), typeof(BindingOptions))]
            public sealed partial class ModelBinder
            {
                public Tags? Labels { get; set; }
                public Boxed? Home { get; set; }
            }
            """;

        var result = TestHarness.AssertCompiles(source);

        result.Source.Should().Contain(
            "public global::System.Collections.Generic.List<int>? Labels_Value => this.Labels?.Values;");
        result.Source.Should().Contain("public string? Labels_Display =>");

        result.Source.Should().Contain("public global::Demo.Address? Home_Value => this.Home?.Unwrap();");
        result.Source.Should().Contain("public string? Home_Street => this.Home?.Unwrap()?.Street;");
    }

    [Fact]
    public void Renders_a_declared_sequence_without_rebinding_it()
    {
        var source = Wrap(
            "public class Model { public int Id { get; set; } }",
            "    public IEnumerable<int>? Scores { get; set; }");

        var result = TestHarness.AssertCompiles(source);

        result.Source.Should().Contain("public string? Scores_Display =>");
        result.Source.Should().Contain("public string? Scores_Array =>");
        result.Source.Should().NotContain("Scores => ");
    }

    [Fact]
    public void Carries_the_declared_propertys_own_summary_into_the_description()
    {
        var source = Wrap("""
            public class Engine
            {
                /// <summary>Revolutions</summary>
                public int Rpm { get; set; }
            }

            public class Model { public int Id { get; set; } }
            """,
            """
                /// <summary>The engine</summary>
                public Engine? Motor { get; set; }
            """);

        var result = TestHarness.AssertCompiles(source);

        result.Source.Should().Contain(
            """[global::System.ComponentModel.Description("The engine: Revolutions")]""");

        // The remark points at the binder's own property, which is where the chain starts.
        result.Source.Should().Contain("""<see cref="Demo.ModelBinder.Motor"/>""");
    }

    [Fact]
    public void Widens_a_generated_name_away_from_one_the_binder_declares()
    {
        var source = Wrap("""
            public class Model
            {
                public int Rank { get; set; }
                public string Note { get; set; }
            }
            """,
            "    public int Rank { get; set; }");

        var result = TestHarness.AssertCompiles(source);

        // A hand-written member wins its name outright: sharing it would be a duplicate, not a shadow.
        result.Source.Should().Contain("public int Rank_ => _source.Rank;");
        result.Source.Should().Contain("public string? Note => _source.Note;");
    }

    [Fact]
    public void Flattens_a_declared_property_recursively()
    {
        var source = Wrap("""
            public class Street { public string Name { get; set; } }

            public class Address { public Street? Street { get; set; } }

            public class Model { public int Id { get; set; } }
            """,
            "    public Address? Location { get; set; }");

        var result = TestHarness.AssertCompiles(source);

        result.Source.Should().Contain(
            "public global::Demo.Street? Location_Street => this.Location?.Street;");
        result.Source.Should().Contain(
            "public string? Location_Street_Name => this.Location?.Street?.Name;");
    }

    [Fact]
    public void Ignores_a_declared_member_it_cannot_read()
    {
        var source = Wrap("""
            public class Engine { public int Rpm { get; set; } }

            public class Model { public int Id { get; set; } }
            """,
            """
                private Engine? Hidden { get; set; }
                public static Engine? Shared { get; set; }
                public Engine? this[int index] => null;
            """);

        var result = TestHarness.AssertCompiles(source);

        result.Source.Should().NotContain("Hidden");
        result.Source.Should().NotContain("Shared");
    }

    [Fact]
    public void Reports_a_declared_property_that_points_back_at_the_binder()
    {
        var source = Wrap(
            "public class Model { public int Id { get; set; } }",
            "    public ModelBinder? Parent { get; set; }");

        var result = TestHarness.Run(source);

        result.Should().HaveDiagnostic("WGD001", Microsoft.CodeAnalysis.DiagnosticSeverity.Warning);
    }
}
