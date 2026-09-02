namespace WinDataBinding.SourceGenerator.Tests;

/// <summary>
/// <c>[MapType]</c> stands a wrapper type in for the type it wraps. The expression reaching the wrapped value
/// is written out verbatim — never parsed, resolved or checked — and the target is then classified exactly as
/// if the property had been declared with it.
/// </summary>
public class MapTypeTests
{
    private static string Wrap(string body) => $$"""
        using System.Collections.Generic;
        using NodaTime;
        using WinDataBinding;

        namespace Demo;

        {{body}}
        """;

    [Fact]
    public void Substitutes_the_target_type_for_the_wrapper()
    {
        var source = Wrap("""
            public readonly struct OrderId
            {
                public System.Guid Value { get; }
            }

            [MapType(typeof(OrderId), typeof(System.Guid), "Value")]
            public class BindingOptions;

            public class Model
            {
                /// <summary>The order</summary>
                public OrderId Order { get; set; }
            }

            [GenerateWindowsBindingModel(typeof(Model), typeof(BindingOptions))]
            public sealed partial class ModelBinder { }
            """);

        var result = TestHarness.AssertCompiles(source);

        result.Should().HaveNoDiagnostics();

        // The mapping stands in for the wrapper, so the property keeps the name the wrapper would have had.
        result.Source.Should().Contain("public global::System.Guid Order => _source.Order.Value;");
        result.Source.Should().Contain("""[global::System.ComponentModel.Description("The order")]""");
    }

    [Fact]
    public void Classifies_the_target_exactly_as_a_declared_property_of_that_type()
    {
        // The point of the mapping: a wrapper around a sequence gets the sequence treatment.
        var source = Wrap("""
            public class Tags
            {
                public List<int> Values { get; }
            }

            [MapType(typeof(Tags), typeof(List<int>), "Values")]
            public class BindingOptions;

            public class Model { public Tags Labels { get; set; } }

            [GenerateWindowsBindingModel(typeof(Model), typeof(BindingOptions))]
            public sealed partial class ModelBinder { }
            """);

        var result = TestHarness.AssertCompiles(source);

        result.Source.Should().Contain(
            "public global::System.Collections.Generic.List<int>? Labels => _source.Labels?.Values;");
        result.Source.Should().Contain("public string? Labels_Display =>");
        result.Source.Should().Contain("public string? Labels_Array =>");
    }

    [Fact]
    public void Traverses_the_target_when_it_is_an_object_graph()
    {
        var source = Wrap("""
            public class Address { public string Street { get; set; } }

            public class Boxed { public Address Unwrap() => null; }

            [MapType(typeof(Boxed), typeof(Address), "Unwrap()")]
            public class BindingOptions;

            public class Model { public Boxed Home { get; set; } }

            [GenerateWindowsBindingModel(typeof(Model), typeof(BindingOptions))]
            public sealed partial class ModelBinder { }
            """);

        var result = TestHarness.AssertCompiles(source);

        // A method is as good as a property: the expression is written straight out.
        result.Source.Should().Contain("public global::Demo.Address? Home => _source.Home?.Unwrap();");
        result.Source.Should().Contain("public string? Home_Street => _source.Home?.Unwrap()?.Street;");
    }

    [Fact]
    public void Writes_a_field_expression_out_unchanged()
    {
        var source = Wrap("""
            public class Counter { public int Raw; }

            [MapType(typeof(Counter), typeof(int), "Raw")]
            public class BindingOptions;

            public class Model { public Counter Hits { get; set; } }

            [GenerateWindowsBindingModel(typeof(Model), typeof(BindingOptions))]
            public sealed partial class ModelBinder { }
            """);

        var result = TestHarness.AssertCompiles(source);

        result.Source.Should().Contain("public int? Hits => _source.Hits?.Raw;");
    }

    [Fact]
    public void Reaches_through_a_struct_wrapper_without_lifting()
    {
        var source = Wrap("""
            public readonly struct Meters
            {
                public double Value { get; }
            }

            [MapType(typeof(Meters), typeof(double), "Value")]
            public class BindingOptions;

            public class Model
            {
                public Meters Depth { get; set; }
                public Meters? Height { get; set; }
            }

            [GenerateWindowsBindingModel(typeof(Model), typeof(BindingOptions))]
            public sealed partial class ModelBinder { }
            """);

        var result = TestHarness.AssertCompiles(source);

        result.Source.Should().Contain("public double Depth => _source.Depth.Value;");

        // Nullable<Meters> lifts on the way in, as any other nullable struct would.
        result.Source.Should().Contain("public double? Height => _source.Height?.Value;");
    }

    [Fact]
    public void Maps_the_target_of_a_wrapper_to_a_type_that_needs_converting()
    {
        var source = Wrap("""
            public class Stamp { public Instant Moment { get; } }

            [MapType(typeof(Stamp), typeof(Instant), "Moment")]
            public class BindingOptions;

            public class Model { public Stamp Created { get; set; } }

            [GenerateWindowsBindingModel(typeof(Model), typeof(BindingOptions))]
            public sealed partial class ModelBinder { }
            """);

        var result = TestHarness.AssertCompiles(source);

        // Instant is converted after the substitution, so both steps show up in one expression.
        result.Source.Should().Contain(
            "public global::System.DateTime? Created => _source.Created?.Moment.ToDateTimeUtc();");
        result.Source.Should().Contain(
            "public string? Created_Formatted => _source.Created?.Moment.ToString(null, null);");
    }

    [Fact]
    public void Overrides_a_type_the_generator_already_understands()
    {
        // The mapping is consulted before anything else, so it wins over the built-in handling.
        var source = Wrap("""
            [MapType(typeof(Instant), typeof(long), "ToUnixTimeMilliseconds()")]
            public class BindingOptions;

            public class Model { public Instant Created { get; set; } }

            [GenerateWindowsBindingModel(typeof(Model), typeof(BindingOptions))]
            public sealed partial class ModelBinder { }
            """);

        var result = TestHarness.AssertCompiles(source);

        result.Source.Should().Contain("public long Created => _source.Created.ToUnixTimeMilliseconds();");
        result.Source.Should().NotContain("ToDateTimeUtc");
    }

    [Fact]
    public void Reads_mappings_from_the_options_base_types()
    {
        var source = Wrap("""
            public class Boxed { public int Value { get; } }
            public class Wrapped { public string Value { get; } }

            [MapType(typeof(Boxed), typeof(int), "Value")]
            public class SharedOptions;

            [MapType(typeof(Wrapped), typeof(string), "Value")]
            public class BindingOptions : SharedOptions;

            public class Model
            {
                public Boxed Count { get; set; }
                public Wrapped Label { get; set; }
            }

            [GenerateWindowsBindingModel(typeof(Model), typeof(BindingOptions))]
            public sealed partial class ModelBinder { }
            """);

        var result = TestHarness.AssertCompiles(source);

        result.Source.Should().Contain("public int? Count => _source.Count?.Value;");
        result.Source.Should().Contain("public string? Label => _source.Label?.Value;");
    }

    [Fact]
    public void Takes_the_most_derived_mapping_for_a_type()
    {
        var source = Wrap("""
            public class Boxed
            {
                public int Near { get; }
                public int Far { get; }
            }

            [MapType(typeof(Boxed), typeof(int), "Far")]
            public class SharedOptions;

            [MapType(typeof(Boxed), typeof(int), "Near")]
            public class BindingOptions : SharedOptions;

            public class Model { public Boxed Count { get; set; } }

            [GenerateWindowsBindingModel(typeof(Model), typeof(BindingOptions))]
            public sealed partial class ModelBinder { }
            """);

        var result = TestHarness.AssertCompiles(source);

        result.Source.Should().Contain("public int? Count => _source.Count?.Near;");
        result.Source.Should().NotContain("Far");
    }

    [Fact]
    public void Applies_a_mapping_wherever_the_wrapper_appears()
    {
        var source = Wrap("""
            public class Boxed { public int Value { get; } }

            public class Inner { public Boxed Depth { get; set; } }

            [MapType(typeof(Boxed), typeof(int), "Value")]
            public class BindingOptions;

            public class Model
            {
                public Boxed Top { get; set; }
                public Inner Nested { get; set; }
            }

            [GenerateWindowsBindingModel(typeof(Model), typeof(BindingOptions))]
            public sealed partial class ModelBinder
            {
                public Boxed? Declared { get; set; }
            }
            """);

        var result = TestHarness.AssertCompiles(source);

        result.Source.Should().Contain("public int? Top => _source.Top?.Value;");
        result.Source.Should().Contain("public int? Nested_Depth => _source.Nested?.Depth?.Value;");

        // A property the binder declares itself is mapped too, and keeps its own name.
        result.Source.Should().Contain("public int? Declared_Value => this.Declared?.Value;");
    }

    [Fact]
    public void Leaves_a_wrapper_alone_when_no_options_are_given()
    {
        var source = Wrap("""
            public class Boxed { public int Value { get; } }

            public class Model { public Boxed Count { get; set; } }

            [GenerateWindowsBindingModel(typeof(Model))]
            public sealed partial class ModelBinder { }
            """);

        var result = TestHarness.AssertCompiles(source);

        // Without a mapping it is an ordinary object graph.
        result.Source.Should().Contain("public global::Demo.Boxed? Count => _source.Count;");
        result.Source.Should().Contain("public int? Count_Value => _source.Count?.Value;");
    }

    [Fact]
    public void Maps_every_type_that_implements_a_mapped_interface()
    {
        // The case this exists for: StronglyTypedId marks its attribute [Conditional], so the compiler never
        // writes it to the assembly. A generator in another project sees those IDs only as metadata, with no
        // attribute left to read — but the marker interface the template adds is still there.
        var source = Wrap("""
            public interface IStringId : System.IFormattable
            {
                string Value { get; }
            }

            public readonly partial struct JobId : IStringId
            {
                public string Value => "";
                public string ToString(string format, System.IFormatProvider provider) => "";
            }

            public readonly partial struct ColourId : IStringId
            {
                public string Value => "";
                public string ToString(string format, System.IFormatProvider provider) => "";
            }

            [MapType(typeof(IStringId), typeof(string), "Value")]
            public class BindingOptions;

            public class Model
            {
                public JobId Job { get; set; }
                public ColourId Colour { get; set; }
            }

            [GenerateWindowsBindingModel(typeof(Model), typeof(BindingOptions))]
            public sealed partial class ModelBinder { }
            """);

        var result = TestHarness.AssertCompiles(source);

        result.Should().HaveNoDiagnostics();

        // One declaration covers the whole family, and each reads as its underlying value under the bare name.
        result.Source.Should().Contain("public string? Job => _source.Job.Value;");
        result.Source.Should().Contain("public string? Colour => _source.Colour.Value;");

        // The mapping replaces the wrapper outright, so the struct is neither bound nor walked.
        result.Source.Should().NotContain("Job_Value");
        result.Source.Should().NotContain("Job_Formatted");
    }

    [Fact]
    public void Maps_every_type_that_derives_from_a_mapped_base()
    {
        var source = Wrap("""
            public abstract class Wrapper { public int Value => 0; }

            public sealed class Counter : Wrapper;

            [MapType(typeof(Wrapper), typeof(int), "Value")]
            public class BindingOptions;

            public class Model { public Counter Hits { get; set; } }

            [GenerateWindowsBindingModel(typeof(Model), typeof(BindingOptions))]
            public sealed partial class ModelBinder { }
            """);

        var result = TestHarness.AssertCompiles(source);

        result.Should().HaveNoDiagnostics();
        result.Source.Should().Contain("public int? Hits => _source.Hits?.Value;");
    }

    [Fact]
    public void Prefers_the_mapping_named_on_the_type_itself()
    {
        var source = Wrap("""
            public interface IValue { int Near { get; } int Far { get; } }

            public readonly struct Special : IValue
            {
                public int Near => 0;
                public int Far => 0;
            }

            public readonly struct Ordinary : IValue
            {
                public int Near => 0;
                public int Far => 0;
            }

            [MapType(typeof(IValue), typeof(int), "Far")]
            [MapType(typeof(Special), typeof(int), "Near")]
            public class BindingOptions;

            public class Model
            {
                public Special One { get; set; }
                public Ordinary Two { get; set; }
            }

            [GenerateWindowsBindingModel(typeof(Model), typeof(BindingOptions))]
            public sealed partial class ModelBinder { }
            """);

        var result = TestHarness.AssertCompiles(source);

        // An exact match beats an interface one, wherever the two are declared.
        result.Source.Should().Contain("public int One => _source.One.Near;");
        result.Source.Should().Contain("public int Two => _source.Two.Far;");
    }

    [Fact]
    public void Leaves_a_type_outside_the_mapped_hierarchy_alone()
    {
        var source = Wrap("""
            public interface IStringId { string Value { get; } }

            public readonly struct Plain { public int Value => 0; }

            [MapType(typeof(IStringId), typeof(string), "Value")]
            public class BindingOptions;

            public class Model { public Plain Other { get; set; } }

            [GenerateWindowsBindingModel(typeof(Model), typeof(BindingOptions))]
            public sealed partial class ModelBinder { }
            """);

        var result = TestHarness.AssertCompiles(source);

        // Not an IStringId, so it stays an ordinary graph.
        result.Source.Should().Contain("public int Other_Value => _source.Other.Value;");
    }

    [Fact]
    public void Gives_the_mapped_value_every_transformation_its_type_earns()
    {
        // The substitution is total: from here on the target is treated exactly as a property declared with
        // that type would be, so each one collects whatever its own shape earns it.
        var source = Wrap("""
            public class ToList { public List<int> V => null; }
            public class ToFormattable { public Money V => null; }
            public class ToGraph { public Temp V => default; }
            public class ToJson { public System.Text.Json.Nodes.JsonNode V => null; }
            public class ToInstant { public Instant V => default; }
            public class ToEnum { public Grade V => default; }
            public class ToDecimal { public decimal V => 0; }

            public enum Grade { Low, High }

            public class Money : System.IFormattable
            {
                public string ToString(string f, System.IFormatProvider p) => "";
            }

            public struct Temp : System.IFormattable
            {
                public int Degrees => 0;
                public string ToString(string f, System.IFormatProvider p) => "";
            }

            [MapType(typeof(ToList), typeof(List<int>), "V")]
            [MapType(typeof(ToFormattable), typeof(Money), "V")]
            [MapType(typeof(ToGraph), typeof(Temp), "V")]
            [MapType(typeof(ToJson), typeof(System.Text.Json.Nodes.JsonNode), "V")]
            [MapType(typeof(ToInstant), typeof(Instant), "V")]
            [MapType(typeof(ToEnum), typeof(Grade), "V")]
            [MapType(typeof(ToDecimal), typeof(decimal), "V")]
            public class BindingOptions;

            public class Model
            {
                public ToList A { get; set; }
                public ToFormattable B { get; set; }
                public ToGraph C { get; set; }
                public ToJson D { get; set; }
                public ToInstant E { get; set; }
                public ToEnum F { get; set; }
                public ToDecimal G { get; set; }
            }

            [GenerateWindowsBindingModel(typeof(Model), typeof(BindingOptions))]
            public sealed partial class ModelBinder { }
            """);

        var result = TestHarness.AssertCompiles(source);

        result.Should().HaveNoDiagnostics();

        // A sequence counts and renders, element by element.
        result.Source.Should().Contain("public int? A_Count => _source.A?.V?.Count;");
        result.Source.Should().Contain("public string? A_Display => _source.A?.V is { } items ?");
        result.Source.Should().Contain("public string? A_Array =>");

        // A formattable reference type gets the rendered twin.
        result.Source.Should().Contain("public string? B_Formatted => _source.B?.V?.ToString(null, null);");

        // A formattable struct is flattened as a graph and rendered.
        result.Source.Should().Contain("public int? C_Degrees => _source.C?.V.Degrees;");
        result.Source.Should().Contain("public string? C_Formatted => _source.C?.V.ToString(null, null);");

        // JSON renders whole rather than being traversed.
        result.Source.Should().Contain("public string? D_Formatted => _source.D?.V?.ToJsonString();");

        // A type with a conversion converts, then renders.
        result.Source.Should().Contain(
            "public global::System.DateTime? E => _source.E?.V.ToDateTimeUtc();");
        result.Source.Should().Contain("public string? E_Formatted => _source.E?.V.ToString(null, null);");

        // An enum and a leaf bind as they stand: a grid renders those already, so neither gets a twin.
        result.Source.Should().Contain("public global::Demo.Grade? F => _source.F?.V;");
        result.Source.Should().NotContain("F_Formatted");
        result.Source.Should().Contain("public decimal? G => _source.G?.V;");
        result.Source.Should().NotContain("G_Formatted");
    }
}
