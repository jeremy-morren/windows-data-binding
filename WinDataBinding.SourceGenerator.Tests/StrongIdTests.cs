using Microsoft.CodeAnalysis;

namespace WinDataBinding.SourceGenerator.Tests;

public class StrongIdTests
{
    /// <summary>
    /// Declares the model inside a block namespace alongside a stand-in for the StronglyTypedId package.
    /// The attribute is matched by name, so the tests need no reference to the real assembly — which is
    /// also what this proves.
    /// </summary>
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
                public StronglyTypedIdAttribute(Template template, string custom) { }
            }
        }

        namespace Demo
        {
        {{body}}

            [GenerateWindowsBindingModel(typeof(Model))]
            public sealed partial class ModelBinder { }
        }
        """;

    [Fact]
    public void Unwraps_each_built_in_template_to_its_underlying_value()
    {
        var source = Wrap("""
            [StronglyTypedId(Template.Guid)]
            public readonly partial struct OrderId { public System.Guid Value { get; } }

            [StronglyTypedId(Template.Int)]
            public readonly partial struct LineId { public int Value { get; } }

            [StronglyTypedId(Template.Long)]
            public readonly partial struct BatchId { public long Value { get; } }

            [StronglyTypedId(Template.String)]
            public readonly partial struct SkuId { public string Value { get; } }

            public class Model
            {
                /// <summary>The order</summary>
                public OrderId Order { get; set; }
                public LineId Line { get; set; }
                public BatchId Batch { get; set; }
                public SkuId Sku { get; set; }
            }
            """);

        const string expected = """
            namespace Demo
            {
                [global::System.CodeDom.Compiler.GeneratedCode("WinDataBinding.SourceGenerator", "1.0.0.0")]
                partial class ModelBinder
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

                    /// <summary><c>Order.Value</c></summary>
                    /// <remarks><see cref="Demo.Model.Order"/></remarks>
                    [global::System.ComponentModel.Description("The order")]
                    public global::System.Guid Order => _source.Order.Value;

                    /// <summary><c>((global::System.IFormattable)Order.Value)?.ToString(null, null)</c></summary>
                    /// <remarks><see cref="Demo.Model.Order"/></remarks>
                    [global::System.ComponentModel.Description("The order (Formatted)")]
                    public string? Order_Formatted => ((global::System.IFormattable)_source.Order.Value)?.ToString(null, null);

                    /// <summary><c>Line.Value</c></summary>
                    /// <remarks><see cref="Demo.Model.Line"/></remarks>
                    public int Line => _source.Line.Value;

                    /// <summary><c>((global::System.IFormattable)Line.Value)?.ToString(null, null)</c></summary>
                    /// <remarks><see cref="Demo.Model.Line"/></remarks>
                    public string? Line_Formatted => ((global::System.IFormattable)_source.Line.Value)?.ToString(null, null);

                    /// <summary><c>Batch.Value</c></summary>
                    /// <remarks><see cref="Demo.Model.Batch"/></remarks>
                    public long Batch => _source.Batch.Value;

                    /// <summary><c>((global::System.IFormattable)Batch.Value)?.ToString(null, null)</c></summary>
                    /// <remarks><see cref="Demo.Model.Batch"/></remarks>
                    public string? Batch_Formatted => ((global::System.IFormattable)_source.Batch.Value)?.ToString(null, null);

                    /// <summary><c>Sku.Value</c></summary>
                    /// <remarks><see cref="Demo.Model.Sku"/></remarks>
                    public string? Sku => _source.Sku.Value;
                }
            }
            """;

        TestHarness.AssertGenerated(expected, source);
    }

    [Fact]
    public void Skips_the_rendered_twin_for_a_string_backed_id()
    {
        // The template hands back the string verbatim, so a rendered twin of it would say nothing new.
        // It would not compile either: string is sealed and does not implement IFormattable.
        var source = Wrap("""
            [StronglyTypedId(Template.String)]
            public readonly partial struct SkuId { public string Value { get; } }

            [StronglyTypedId(Template.Guid)]
            public readonly partial struct OrderId { public System.Guid Value { get; } }

            public class Model
            {
                public SkuId Sku { get; set; }
                public OrderId Order { get; set; }
            }
            """);

        var result = TestHarness.AssertCompiles(source);

        result.Source.Should().Contain("public string? Sku => _source.Sku.Value;");
        result.Source.Should().NotContain("Sku_Formatted");

        // Every other template still gets one.
        result.Source.Should().Contain("Order_Formatted");
    }

    [Fact]
    public void Keeps_a_string_backed_id_bare_until_it_has_company()
    {
        var source = Wrap("""
            [StronglyTypedId(Template.String)]
            public readonly partial struct PlainId { public string Value { get; } }

            [StronglyTypedId(Template.String)]
            public readonly partial struct LabelledId
            {
                public string Value { get; }
                public int Length { get; }
            }

            public class Model
            {
                public PlainId Plain { get; set; }
                public LabelledId Labelled { get; set; }
            }
            """);

        var result = TestHarness.AssertCompiles(source);

        // Alone: one bare property, no suffix and no rendered twin.
        result.Source.Should().Contain("public string? Plain => _source.Plain.Value;");
        result.Source.Should().NotContain("Plain_");

        // With company: the value takes the suffix so it sits alongside, but still no rendered twin.
        result.Source.Should().Contain("public string? Labelled_Value => _source.Labelled.Value;");
        result.Source.Should().Contain("public int Labelled_Length => _source.Labelled.Length;");
        result.Source.Should().NotContain("Labelled_Formatted");
        result.Source.Should().NotContain("Labelled_Value_Formatted");
    }

    [Fact]
    public void Takes_the_underlying_type_from_the_template_not_from_the_struct()
    {
        // StronglyTypedId writes the Value member with its own generator, and generators cannot see each
        // other's output, so the struct here deliberately has no Value at all.
        var source = Wrap("""
            [StronglyTypedId(Template.Long)]
            public readonly partial struct AccountId;

            public class Model { public AccountId Account { get; set; } }
            """);

        var result = TestHarness.Run(source);

        result.Source.Should().Contain("public long Account => _source.Account.Value;");
    }

    [Fact]
    public void Lifts_a_strong_id_reached_through_a_nullable_chain()
    {
        var source = Wrap("""
            [StronglyTypedId(Template.Guid)]
            public readonly partial struct OrderId { public System.Guid Value { get; } }

            public class Inner { public OrderId Order { get; set; } }

            public class Model { public Inner Inner { get; set; } }
            """);

        var result = TestHarness.AssertCompiles(source);

        result.Source.Should().Contain(
            "public global::System.Guid? Inner_Order => _source.Inner?.Order.Value;");
    }

    [Fact]
    public void Inspects_the_part_of_the_id_that_is_visible_in_source()
    {
        // Only the hand-written part of the struct is reachable: StronglyTypedId's own generator writes the
        // rest, and generators cannot see each other's output. Whatever is there is walked like any other
        // struct or class graph.
        var source = Wrap("""
            [StronglyTypedId(Template.Int)]
            public readonly partial struct CustomerId { public int Value { get; } }

            public class Detail { public int Count { get; set; } }

            [StronglyTypedId(Template.Guid)]
            public readonly partial struct OrderId
            {
                public System.Guid Value { get; }

                /// <summary>Who ordered</summary>
                public CustomerId Customer { get; }

                public string Label { get; }

                public Detail Detail { get; }
            }

            public class Model { public OrderId Order { get; set; } }
            """);

        var result = TestHarness.AssertCompiles(source);

        result.Should().HaveNoDiagnostics();

        // Sharing with members, the value takes the value-property suffix rather than the bare name.
        result.Source.Should().Contain("public global::System.Guid Order_Value => _source.Order.Value;");
        result.Source.Should().Contain(
            "public string? Order_Value_Formatted => ((global::System.IFormattable)_source.Order.Value)?.ToString(null, null);");

        // A nested strongly typed ID unwraps through its own template.
        result.Source.Should().Contain("public int Order_Customer => _source.Order.Customer.Value;");
        result.Source.Should().Contain("""
            [global::System.ComponentModel.Description("Who ordered")]
            """.Trim());

        // Simple types and object graphs behave exactly as they do anywhere else.
        result.Source.Should().Contain("public string? Order_Label => _source.Order.Label;");
        result.Source.Should().Contain("public global::Demo.Detail? Order_Detail => _source.Order.Detail;");
        result.Source.Should().Contain("public int? Order_Detail_Count => _source.Order.Detail?.Count;");

        // The value property is bound once, through the template, never again by traversal.
        result.Source.Should().Contain("Order_Value =>").And.NotContain("Order_Value_Value");
    }

    [Fact]
    public void Binds_nothing_extra_for_an_id_with_no_visible_members()
    {
        var source = Wrap("""
            [StronglyTypedId(Template.Guid)]
            public readonly partial struct OrderId;

            public class Model { public OrderId Order { get; set; } }
            """);

        var result = TestHarness.Run(source);

        // Standing alone, the value keeps the bare name; only its rendered twin joins it.
        result.Source.Should().Contain("public global::System.Guid Order => _source.Order.Value;");
        result.Source.Should().Contain(
            "public string? Order_Formatted => ((global::System.IFormattable)_source.Order.Value)?.ToString(null, null);");
        result.Source.Should().NotContain("Order_Value");
    }

    [Fact]
    public void Warns_and_skips_a_custom_template()
    {
        var source = Wrap("""
            [StronglyTypedId("my-guid")]
            public readonly partial struct CustomId { public System.Guid Value { get; } }

            public class Model
            {
                public CustomId Custom { get; set; }
                public int Kept { get; set; }
            }
            """);

        var result = TestHarness.AssertCompiles(source);

        result.Should().HaveDiagnostic("WGD005", DiagnosticSeverity.Warning);
        result.Source.Should().NotContain("Custom");
        result.Source.Should().Contain("public int Kept => _source.Kept;");
    }

    [Fact]
    public void Accepts_a_built_in_template_alongside_a_custom_one()
    {
        var source = Wrap("""
            [StronglyTypedId(Template.Int, "int-efcore")]
            public readonly partial struct TicketId { public int Value { get; } }

            public class Model { public TicketId Ticket { get; set; } }
            """);

        var result = TestHarness.AssertCompiles(source);

        result.Should().HaveNoDiagnostics();
        result.Source.Should().Contain("public int Ticket => _source.Ticket.Value;");
    }
}
