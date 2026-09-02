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
            public enum Template { Guid, Int, Long, String }

            [System.AttributeUsage(System.AttributeTargets.Struct)]
            public sealed class StronglyTypedIdAttribute : System.Attribute
            {
                public StronglyTypedIdAttribute(Template template) { }
                public StronglyTypedIdAttribute(string template) { }
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
            public readonly partial struct OrderId { public System.Guid Value { get; } }

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
            public readonly partial struct SkuId { public string Code { get; } }

            public class Model { public SkuId Sku { get; set; } }

            [GenerateWindowsBindingModel(typeof(Model), typeof(BindingOptions))]
            public sealed partial class ModelBinder { }
            """);

        var result = TestHarness.AssertCompiles(source);

        // string is a reference type, so the property is nullable.
        result.Source.Should().Contain("public string? Sku => _source.Sku.Code;");
    }

    [Fact]
    public void Reads_setup_attributes_from_the_options_base_types()
    {
        var source = Wrap("""
            [StrongIdTemplateSetup("from-base", typeof(long), "Value")]
            public class SharedOptions;

            [StrongIdTemplateSetup("from-derived", typeof(int), "Value")]
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
    public void Still_warns_for_a_custom_template_with_no_setup()
    {
        var source = Wrap("""
            [StrongIdTemplateSetup("declared", typeof(int), "Value")]
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
            [StrongIdTemplateSetup("my-int", typeof(int), "Value")]
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
