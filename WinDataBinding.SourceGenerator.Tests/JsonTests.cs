namespace WinDataBinding.SourceGenerator.Tests;

public class JsonTests
{
    [Fact]
    public void Renders_json_values_whole_without_traversing_them()
    {
        var source = TestSources.Wrap("""
            using System.Text.Json;
            using System.Text.Json.Nodes;

            public class Model
            {
                /// <summary>Raw payload</summary>
                public JsonElement Element { get; set; }

                public JsonNode Node { get; set; }

                public JsonObject Object { get; set; }
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

                    /// <summary><c>Element.GetRawText()</c></summary>
                    /// <remarks><see cref="Demo.Model.Element"/></remarks>
                    [global::System.ComponentModel.Description("Raw payload (Formatted)")]
                    public string? Element_Formatted => _source.Element.GetRawText();

                    /// <summary><c>Node?.ToJsonString()</c></summary>
                    /// <remarks><see cref="Demo.Model.Node"/></remarks>
                    public string? Node_Formatted => _source.Node?.ToJsonString();

                    /// <summary><c>Object?.ToJsonString()</c></summary>
                    /// <remarks><see cref="Demo.Model.Object"/></remarks>
                    public string? Object_Formatted => _source.Object?.ToJsonString();
                }
            }
            """;

        TestHarness.AssertGenerated(expected, source);
    }

    [Fact]
    public void Treats_a_json_array_as_a_value_but_a_sequence_of_nodes_as_a_sequence()
    {
        // JsonArray is IEnumerable<JsonNode?> too, but being a JsonNode wins: it renders whole.
        var source = TestSources.Wrap("""
            using System.Collections.Generic;
            using System.Text.Json.Nodes;

            public class Model
            {
                public JsonArray Array { get; set; }

                public List<JsonNode> Nodes { get; set; }
            }
            """);

        var result = TestHarness.AssertCompiles(source);

        result.Source.Should().Contain("public string? Array_Formatted => _source.Array?.ToJsonString();");
        result.Source.Should().NotContain("Array_Display");

        // The list keeps the collection rules, rendering each element as JSON.
        result.Source.Should().Contain(
            "public global::System.Collections.Generic.List<global::System.Text.Json.Nodes.JsonNode>? Nodes => _source.Nodes;");
        result.Source.Should().Contain(
            "global::System.Linq.Enumerable.Select(items, item => item?.ToJsonString())");
        result.Source.Should().Contain(
            "public string? Nodes_Array => Nodes_Display is { } display ? $\"[{display}]\" : null;");
    }

    [Fact]
    public void Renders_a_sequence_of_json_elements()
    {
        var source = TestSources.Wrap("""
            using System.Text.Json;

            public class Model { public JsonElement[] Elements { get; set; } }
            """);

        var result = TestHarness.AssertCompiles(source);

        result.Source.Should().Contain(
            "global::System.Linq.Enumerable.Select(items, item => item.GetRawText())");
    }

    [Fact]
    public void Lifts_a_json_value_reached_through_a_nullable_chain()
    {
        var source = TestSources.Wrap("""
            using System.Text.Json;
            using System.Text.Json.Nodes;

            public class Inner
            {
                public JsonElement Element { get; set; }
                public JsonNode Node { get; set; }
            }

            public class Model { public Inner Inner { get; set; } }
            """);

        var result = TestHarness.AssertCompiles(source);

        result.Source.Should().Contain(
            "public string? Inner_Element_Formatted => _source.Inner?.Element.GetRawText();");
        result.Source.Should().Contain(
            "public string? Inner_Node_Formatted => _source.Inner?.Node?.ToJsonString();");
    }
}
