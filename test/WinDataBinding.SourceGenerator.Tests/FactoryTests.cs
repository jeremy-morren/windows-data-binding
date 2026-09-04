namespace WinDataBinding.SourceGenerator.Tests;

public class FactoryTests
{
    [Fact]
    public void Generates_a_factory_that_maps_a_null_source_to_a_null_binder()
    {
        var source = TestSources.Wrap("public class Model { public int Value { get; set; } }");

        var result = TestHarness.AssertCompiles(source);

        result.Source.Should().Contain(
            "public static ModelBinder? Create(global::Demo.Model? source) =>");
        result.Source.Should().Contain("source is not null ? new ModelBinder(source) : null;");
    }

    [Fact]
    public void Binds_a_struct_as_readily_as_a_class()
    {
        var source = """
            using WinDataBinding;

            namespace Demo;

            public class Model { public int Value { get; set; } }

            [GenerateWindowsBindingModel(typeof(Model))]
            public readonly partial struct ModelBinder;
            """;

        var result = TestHarness.AssertCompiles(source);

        result.Source.Should().Contain("partial struct ModelBinder");
        result.Source.Should().NotContain("partial class ModelBinder");

        // A struct cannot return null from its constructor, so the factory is the only way to express it.
        result.Source.Should().Contain("public static ModelBinder? Create(global::Demo.Model? source) =>");
        result.Source.Should().Contain("public int Value => _source.Value;");
    }

    [Fact]
    public void Nests_a_struct_binder_inside_its_parent()
    {
        var source = """
            using WinDataBinding;

            namespace Demo;

            public class Model { public int Value { get; set; } }

            public partial struct Outer
            {
                [GenerateWindowsBindingModel(typeof(Model))]
                public partial struct Binder { }
            }
            """;

        var result = TestHarness.AssertCompiles(source);

        result.Source.Should().Contain("partial struct Outer");
        result.Source.Should().Contain("partial struct Binder");
    }

    [Fact]
    public void Annotates_the_factory_only_with_what_the_compilation_can_resolve()
    {
        var source = TestSources.Wrap("public class Model { public int Value { get; set; } }");

        var result = TestHarness.AssertCompiles(source);

        // net8 has NotNullIfNotNull; nothing here references JetBrains.Annotations.
        result.Source.Should().Contain(
            "[return: global::System.Diagnostics.CodeAnalysis.NotNullIfNotNull(\"source\")]");
        result.Source.Should().NotContain("JetBrains.Annotations.ContractAnnotation");
    }

    [Fact]
    public void Adds_the_contract_annotation_when_JetBrains_annotations_are_available()
    {
        var source = """
            using WinDataBinding;

            namespace JetBrains.Annotations
            {
                [System.AttributeUsage(System.AttributeTargets.Method)]
                public sealed class ContractAnnotationAttribute : System.Attribute
                {
                    public ContractAnnotationAttribute(string contract) { }
                }
            }

            namespace Demo
            {
                public class Model { public int Value { get; set; } }

                [GenerateWindowsBindingModel(typeof(Model))]
                public sealed partial class ModelBinder { }
            }
            """;

        var result = TestHarness.AssertCompiles(source);

        result.Source.Should().Contain(
            "[global::JetBrains.Annotations.ContractAnnotation(\"null => null; notnull => notnull\")]");
    }
}
