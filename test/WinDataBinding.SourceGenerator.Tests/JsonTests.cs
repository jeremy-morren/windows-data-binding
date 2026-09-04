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

                    [global::System.ComponentModel.Description("Raw payload (Formatted)")]
                    public string? Element_Formatted => _source.Element.GetRawText();

                    public string? Node_Formatted => _source.Node?.ToJsonString();

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
