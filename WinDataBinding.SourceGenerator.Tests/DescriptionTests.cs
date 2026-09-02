namespace WinDataBinding.SourceGenerator.Tests;

public class DescriptionTests
{
    [Fact]
    public void Reduces_markup_in_a_summary_to_its_displayed_text()
    {
        var source = TestSources.Wrap("""
            public class Model
            {
                /// <summary>Rendered with <c>ToString()</c> for display</summary>
                public string Rendered { get; set; }

                /// <summary>See <see cref="Model.Rendered"/> for the text form</summary>
                public int Raw { get; set; }

                /// <summary>
                /// Spread over
                /// several <c>lines</c>
                /// </summary>
                public int Wrapped { get; set; }
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

                    /// <summary><c>Rendered</c></summary>
                    /// <remarks><see cref="Demo.Model.Rendered"/></remarks>
                    [global::System.ComponentModel.Description("Rendered with ToString() for display")]
                    public string? Rendered => _source.Rendered;

                    /// <summary><c>Raw</c></summary>
                    /// <remarks><see cref="Demo.Model.Raw"/></remarks>
                    [global::System.ComponentModel.Description("See for the text form")]
                    public int Raw => _source.Raw;

                    /// <summary><c>Wrapped</c></summary>
                    /// <remarks><see cref="Demo.Model.Wrapped"/></remarks>
                    [global::System.ComponentModel.Description("Spread over several lines")]
                    public int Wrapped => _source.Wrapped;
                }
            }
            """;

        TestHarness.AssertGenerated(expected, source);
    }

    [Fact]
    public void Decodes_xml_entities_in_a_summary()
    {
        var source = TestSources.Wrap("""
            public class Model
            {
                /// <summary>Values where <c>a &gt; b</c> &amp;&amp; b &lt; c</summary>
                public int Compared { get; set; }
            }
            """);

        var result = TestHarness.AssertCompiles(source);

        result.Source.Should().Contain(
            """[global::System.ComponentModel.Description("Values where a > b && b < c")]""");
    }

    [Fact]
    public void Escapes_a_quote_in_a_summary_when_writing_the_attribute()
    {
        var source = TestSources.Wrap("""
            public class Model
            {
                /// <summary>The "display" name</summary>
                public string Name { get; set; }
            }
            """);

        var result = TestHarness.AssertCompiles(source);

        result.Source.Should().Contain("""
            [global::System.ComponentModel.Description("The \"display\" name")]
            """.Trim());
    }
}
