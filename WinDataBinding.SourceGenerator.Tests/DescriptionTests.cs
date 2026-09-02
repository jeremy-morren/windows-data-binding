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

                    /// <summary>Wraps <paramref name="source"/>, or returns null when it is null.</summary>
                    [return: global::System.Diagnostics.CodeAnalysis.NotNullIfNotNull("source")]
                    public static ModelBinder? Create(global::Demo.Model? source) =>
                        source is not null ? new ModelBinder(source) : null;

                    /// <summary>Compares this binder to another for equality.</summary>
                    /// <remarks>Two binders are equal when the sources they wrap are.</remarks>
                    public bool Equals(ModelBinder? other) =>
                        other is not null && global::System.Collections.Generic.EqualityComparer<global::Demo.Model>.Default.Equals(_source, other._source);

                    /// <inheritdoc/>
                    public override bool Equals(object? obj) => obj is ModelBinder other && Equals(other);

                    /// <inheritdoc/>
                    public override int GetHashCode() => _source is null ? 0 : global::System.Collections.Generic.EqualityComparer<global::Demo.Model>.Default.GetHashCode(_source);

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
    public void Follows_inheritdoc_to_the_inherited_summary()
    {
        var source = TestSources.Wrap("""
            public interface INamed
            {
                /// <summary>Name from the interface</summary>
                string Name { get; }
            }

            public abstract class Base
            {
                /// <summary>Total from the base</summary>
                public abstract int Total { get; }
            }

            public class Model : Base, INamed
            {
                /// <inheritdoc/>
                public string Name { get; set; }

                /// <inheritdoc/>
                public override int Total => 0;

                /// <inheritdoc cref="INamed.Name"/>
                public string Alias { get; set; }
            }
            """);

        var result = TestHarness.AssertCompiles(source);

        result.Source.Should().Contain("""[global::System.ComponentModel.Description("Name from the interface")]""");
        result.Source.Should().Contain("""[global::System.ComponentModel.Description("Total from the base")]""");
    }

    [Fact]
    public void Follows_a_chain_of_inheritdoc_without_looping()
    {
        var source = TestSources.Wrap("""
            public interface IRoot
            {
                /// <summary>The original text</summary>
                string Label { get; }
            }

            public interface IMiddle : IRoot
            {
                /// <inheritdoc/>
                new string Label { get; }
            }

            public class Model : IMiddle
            {
                /// <inheritdoc/>
                public string Label { get; set; }

                /// <inheritdoc/>
                public int Orphan { get; set; }
            }
            """);

        var result = TestHarness.AssertCompiles(source);

        result.Source.Should().Contain("""[global::System.ComponentModel.Description("The original text")]""");

        // Orphan inherits from nothing, so it contributes no description rather than hanging.
        result.Source.Should().Contain("public int Orphan => _source.Orphan;");
        result.Source.Should().NotContain("""Description("")""");
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
