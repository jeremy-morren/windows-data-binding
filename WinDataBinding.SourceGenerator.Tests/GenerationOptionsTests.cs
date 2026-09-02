using Microsoft.CodeAnalysis;

namespace WinDataBinding.SourceGenerator.Tests;

public class GenerationOptionsTests
{
    /// <summary>Model, options and a stand-in for the StronglyTypedId package, wrapped in a block namespace.</summary>
    private static string Wrap(string body) => $$"""
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
        {{body}}
        }
        """;

    [Fact]
    public void Binds_a_custom_template_declared_on_the_options_type()
    {
        var source = Wrap("""
            [StrongIdTemplateSetup("my-guid", typeof(System.Guid), "Value")]
            public class BindingOptions;

            [StronglyTypedId("my-guid")]
            public readonly partial struct OrderId : System.IFormattable
            {
                public System.Guid Value { get; }
                public string ToString(string format, System.IFormatProvider provider) => "";
            }

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
        result.Source.Should().Contain("""
            [global::System.ComponentModel.Description("The order")]
            """.Trim());
        result.Source.Should().Contain("public global::System.Guid Order => _source.Order.Value;");
    }

    [Fact]
    public void Uses_the_declared_property_name_and_value_type()
    {
        // Neither the property name nor the type has to match the built-in templates.
        var source = Wrap("""
            [StrongIdTemplateSetup("my-code", typeof(string), "Code")]
            public class BindingOptions;

            [StronglyTypedId("my-code")]
            public readonly partial struct SkuId : System.IFormattable
            {
                public string Code { get; }
                public string ToString(string format, System.IFormatProvider provider) => "";
            }

            public class Model { public SkuId Sku { get; set; } }

            [GenerateWindowsBindingModel(typeof(Model), typeof(BindingOptions))]
            public sealed partial class ModelBinder { }
            """);

        var result = TestHarness.AssertCompiles(source);

        // string is a reference type, so the property is nullable.
        result.Source.Should().Contain("public string? Sku => _source.Sku.Code;");

        // isFormattable describes the ID, not the value, so the twin renders the ID itself even though
        // the underlying value is already text.
        result.Source.Should().Contain(
            "public string? Sku_Formatted => ((global::System.IFormattable)_source.Sku)?.ToString(null, null);");
    }

    [Fact]
    public void Defaults_isFormattable_to_true_when_it_is_not_passed()
    {
        var source = Wrap("""
            [StrongIdTemplateSetup("my-guid", typeof(System.Guid), "Value")]
            public class BindingOptions;

            [StronglyTypedId("my-guid")]
            public readonly partial struct OrderId : System.IFormattable
            {
                public System.Guid Value { get; }
                public string ToString(string format, System.IFormatProvider provider) => "";
            }

            public class Model { public OrderId Order { get; set; } }

            [GenerateWindowsBindingModel(typeof(Model), typeof(BindingOptions))]
            public sealed partial class ModelBinder { }
            """);

        var result = TestHarness.AssertCompiles(source);

        result.Should().HaveNoDiagnostics();
        result.Source.Should().Contain(
            "public string? Order_Formatted => ((global::System.IFormattable)_source.Order)?.ToString(null, null);");
    }

    [Fact]
    public void Emits_the_twin_when_isFormattable_is_passed_as_true()
    {
        var source = Wrap("""
            [StrongIdTemplateSetup("my-guid", typeof(System.Guid), "Value", true)]
            public class BindingOptions;

            [StronglyTypedId("my-guid")]
            public readonly partial struct OrderId : System.IFormattable
            {
                public System.Guid Value { get; }
                public string ToString(string format, System.IFormatProvider provider) => "";
            }

            public class Model { public OrderId Order { get; set; } }

            [GenerateWindowsBindingModel(typeof(Model), typeof(BindingOptions))]
            public sealed partial class ModelBinder { }
            """);

        var result = TestHarness.AssertCompiles(source);

        result.Should().HaveNoDiagnostics();
        result.Source.Should().Contain("public global::System.Guid Order => _source.Order.Value;");
        result.Source.Should().Contain(
            "public string? Order_Formatted => ((global::System.IFormattable)_source.Order)?.ToString(null, null);");
    }

    [Fact]
    public void Omits_the_twin_when_isFormattable_is_passed_as_false()
    {
        // The ID does not implement IFormattable, so the twin would not even compile. Declaring it is the
        // only way the generator can know: that part of the struct is written by another generator.
        var source = Wrap("""
            [StrongIdTemplateSetup("my-guid", typeof(System.Guid), "Value", false)]
            public class BindingOptions;

            [StronglyTypedId("my-guid")]
            public readonly partial struct OrderId { public System.Guid Value { get; } }

            public class Model { public OrderId Order { get; set; } }

            [GenerateWindowsBindingModel(typeof(Model), typeof(BindingOptions))]
            public sealed partial class ModelBinder { }
            """);

        var result = TestHarness.AssertCompiles(source);

        result.Should().HaveNoDiagnostics();
        result.Source.Should().Contain("public global::System.Guid Order => _source.Order.Value;");
        result.Source.Should().NotContain("Order_Formatted");
    }

    [Fact]
    public void Reads_setup_attributes_from_the_options_base_types()
    {
        var source = Wrap("""
            [StrongIdTemplateSetup("from-base", typeof(long), "Value", false)]
            public class SharedOptions;

            [StrongIdTemplateSetup("from-derived", typeof(int), "Value", false)]
            public class BindingOptions : SharedOptions;

            [StronglyTypedId("from-base")]
            public readonly partial struct BatchId { public long Value { get; } }

            [StronglyTypedId("from-derived")]
            public readonly partial struct LineId { public int Value { get; } }

            public class Model
            {
                public BatchId Batch { get; set; }
                public LineId Line { get; set; }
            }

            [GenerateWindowsBindingModel(typeof(Model), typeof(BindingOptions))]
            public sealed partial class ModelBinder { }
            """);

        var result = TestHarness.AssertCompiles(source);

        result.Should().HaveNoDiagnostics();
        result.Source.Should().Contain("public long Batch => _source.Batch.Value;");
        result.Source.Should().Contain("public int Line => _source.Line.Value;");
    }

    [Fact]
    public void Picks_the_only_named_template_that_is_described()
    {
        var source = Wrap("""
            [StrongIdTemplateSetup("Template1", typeof(int), "Value", false)]
            public class BindingOptions;

            [StronglyTypedId("Template2", "Template1")]
            public readonly partial struct OrderId { public int Value { get; } }

            public class Model { public OrderId Order { get; set; } }

            [GenerateWindowsBindingModel(typeof(Model), typeof(BindingOptions))]
            public sealed partial class ModelBinder { }
            """);

        var result = TestHarness.AssertCompiles(source);

        result.Should().HaveNoDiagnostics();
        result.Source.Should().Contain("public int Order => _source.Order.Value;");
    }

    [Fact]
    public void Takes_the_first_named_template_when_several_are_described()
    {
        // One configuration per ID: the order the ID names them decides, not the order they are set up.
        var source = Wrap("""
            [StrongIdTemplateSetup("Template1", typeof(int), "Value", false)]
            [StrongIdTemplateSetup("Template2", typeof(long), "Other", false)]
            public class BindingOptions;

            [StronglyTypedId("Template2", "Template1")]
            public readonly partial struct OrderId
            {
                public int Value { get; }
                public long Other { get; }
            }

            public class Model { public OrderId Order { get; set; } }

            [GenerateWindowsBindingModel(typeof(Model), typeof(BindingOptions))]
            public sealed partial class ModelBinder { }
            """);

        var result = TestHarness.AssertCompiles(source);

        result.Should().HaveNoDiagnostics();

        // Template2 is named first, so its long/Other configuration wins over Template1's.
        result.Source.Should().Contain("public long Order_Other => _source.Order.Other;");
        result.Source.Should().Contain("public int Order_Value => _source.Order.Value;");
    }

    [Fact]
    public void Matches_template_names_ordinally()
    {
        // Case differs, so nothing matches and the property is skipped.
        var source = Wrap("""
            [StrongIdTemplateSetup("template1", typeof(int), "Value", false)]
            public class BindingOptions;

            [StronglyTypedId("Template1")]
            public readonly partial struct OrderId { public int Value { get; } }

            public class Model { public OrderId Order { get; set; } }

            [GenerateWindowsBindingModel(typeof(Model), typeof(BindingOptions))]
            public sealed partial class ModelBinder { }
            """);

        var result = TestHarness.AssertCompiles(source);

        result.Should().HaveDiagnostic("WGD005", DiagnosticSeverity.Warning);
        result.Source.Should().NotContain("Order");
    }

    [Fact]
    public void Prefers_a_built_in_template_over_the_custom_ones_beside_it()
    {
        var source = Wrap("""
            [StrongIdTemplateSetup("OtherTemplate", typeof(int), "Value", false)]
            public class BindingOptions;

            [StronglyTypedId(Template.String, "OtherTemplate")]
            public readonly partial struct SkuId { public string Value { get; } }

            public class Model { public SkuId Sku { get; set; } }

            [GenerateWindowsBindingModel(typeof(Model), typeof(BindingOptions))]
            public sealed partial class ModelBinder { }
            """);

        var result = TestHarness.AssertCompiles(source);

        result.Should().HaveNoDiagnostics();

        // The built-in String template wins: bare property, no rendered twin, and not the int setup.
        result.Source.Should().Contain("public string? Sku => _source.Sku.Value;");
        result.Source.Should().NotContain("Sku_Formatted");
    }

    [Fact]
    public void Still_warns_for_a_custom_template_with_no_setup()
    {
        var source = Wrap("""
            [StrongIdTemplateSetup("declared", typeof(int), "Value", false)]
            public class BindingOptions;

            [StronglyTypedId("undeclared")]
            public readonly partial struct MysteryId { public int Value { get; } }

            public class Model { public MysteryId Mystery { get; set; } }

            [GenerateWindowsBindingModel(typeof(Model), typeof(BindingOptions))]
            public sealed partial class ModelBinder { }
            """);

        var result = TestHarness.AssertCompiles(source);

        result.Should().HaveDiagnostic("WGD005", DiagnosticSeverity.Warning);
        result.Source.Should().NotContain("Mystery");
    }

    [Fact]
    public void Ignores_attributes_it_does_not_understand()
    {
        var source = Wrap("""
            [System.Obsolete]
            [StrongIdTemplateSetup("my-int", typeof(int), "Value", false)]
            public class BindingOptions;

            [StronglyTypedId("my-int")]
            public readonly partial struct TicketId { public int Value { get; } }

            public class Model { public TicketId Ticket { get; set; } }

            [GenerateWindowsBindingModel(typeof(Model), typeof(BindingOptions))]
            public sealed partial class ModelBinder { }
            """);

        var result = TestHarness.AssertCompiles(source);

        result.Should().HaveNoDiagnostics();
        result.Source.Should().Contain("public int Ticket => _source.Ticket.Value;");
    }
}
