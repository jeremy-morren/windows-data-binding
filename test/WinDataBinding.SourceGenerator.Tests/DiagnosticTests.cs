using Microsoft.CodeAnalysis;

namespace WinDataBinding.SourceGenerator.Tests;

public class DiagnosticTests
{
    [Fact]
    public void Flattens_one_repeat_of_a_circular_reference()
    {
        var source = TestSources.Wrap("""
            public class Node
            {
                public Model Owner { get; set; }
                public int Value { get; set; }
            }

            public class Model { public Node Node { get; set; } }
            """);

        const string expected = """
            namespace Demo;

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

                public global::Demo.Model GetSource() => _source;

                [return: global::System.Diagnostics.CodeAnalysis.NotNullIfNotNull("source")]
                public static ModelBinder? Create(global::Demo.Model? source) =>
                    source is not null ? new ModelBinder(source) : null;

                public bool Equals(ModelBinder? other) =>
                    other is not null && global::System.Collections.Generic.EqualityComparer<global::Demo.Model>.Default.Equals(_source, other._source);

                public override bool Equals(object? obj) => obj is ModelBinder other && Equals(other);

                public override int GetHashCode() => _source is null ? 0 : global::System.Collections.Generic.EqualityComparer<global::Demo.Model>.Default.GetHashCode(_source);

                public global::Demo.Node? Node => _source.Node;

                public global::Demo.Model? Node_Owner => _source.Node?.Owner;

                public global::Demo.Node? Node_Owner_Node => _source.Node?.Owner?.Node;

                public int? Node_Owner_Node_Value => _source.Node?.Owner?.Node?.Value;

                public int? Node_Value => _source.Node?.Value;
            }
            """;

        // The repeat is worth binding; only the copy after it is dropped, and that is not worth saying.
        TestHarness.AssertGenerated(expected, source).Should().HaveNoDiagnostics();
    }

    [Fact]
    public void Flattens_one_repeat_of_a_cycle_that_does_not_pass_through_the_root()
    {
        var source = TestSources.Wrap("""
            public class A { public B B { get; set; } }
            public class B { public A A { get; set; } public int Value { get; set; } }

            public class Model { public A Root { get; set; } }
            """);

        var result = TestHarness.AssertCompiles(source);

        result.Should().HaveNoDiagnostics();
        Assert.Contains("public int? Root_B_Value => _source.Root?.B?.Value;", result.Body);
        Assert.Contains("public int? Root_B_A_B_Value => _source.Root?.B?.A?.B?.Value;", result.Body);

        // Each type comes round once; the chain stops before A would appear a third time.
        Assert.DoesNotContain("Root_B_A_B_A", result.Body);
    }

    [Fact]
    public void Requires_the_binding_model_class_to_be_partial()
    {
        const string source = """
            using WinDataBinding;

            namespace Demo;

            public class Model { public int Value { get; set; } }

            [GenerateWindowsBindingModel(typeof(Model))]
            public sealed class ModelBinder { }
            """;

        var result = TestHarness.Run(source);

        TestHarness.AssertDiagnostic(result, "WGD002", DiagnosticSeverity.Error);
        Assert.Empty(result.Source);
    }

    [Fact]
    public void Requires_every_containing_type_to_be_partial()
    {
        const string source = """
            using WinDataBinding;

            namespace Demo;

            public class Model { public int Value { get; set; } }

            public class Outer
            {
                [GenerateWindowsBindingModel(typeof(Model))]
                public sealed partial class Binder { }
            }
            """;

        var result = TestHarness.Run(source);

        TestHarness.AssertDiagnostic(result, "WGD004", DiagnosticSeverity.Error);
        Assert.Empty(result.Source);
    }

    [Fact]
    public void Names_the_kind_of_type_that_is_not_partial()
    {
        var classSource = """
            using WinDataBinding;

            namespace Demo;

            public class Model { public int Value { get; set; } }

            [GenerateWindowsBindingModel(typeof(Model))]
            public sealed class ClassBinder { }
            """;

        var structSource = """
            using WinDataBinding;

            namespace Demo;

            public class Model { public int Value { get; set; } }

            [GenerateWindowsBindingModel(typeof(Model))]
            public readonly struct StructBinder { }
            """;

        TestHarness.Run(classSource).GeneratorDiagnostics
            .Should().ContainSingle(d => d.Id == "WGD002")
            .Which.GetMessage().Should().Contain("The class 'ClassBinder'");

        TestHarness.Run(structSource).GeneratorDiagnostics
            .Should().ContainSingle(d => d.Id == "WGD002")
            .Which.GetMessage().Should().Contain("The struct 'StructBinder'");
    }

    [Fact]
    public void Names_the_kind_of_containing_type_that_is_not_partial()
    {
        var source = """
            using WinDataBinding;

            namespace Demo;

            public class Model { public int Value { get; set; } }

            public struct Outer
            {
                [GenerateWindowsBindingModel(typeof(Model))]
                public sealed partial class Binder { }
            }
            """;

        TestHarness.Run(source).GeneratorDiagnostics
            .Should().ContainSingle(d => d.Id == "WGD004")
            .Which.GetMessage().Should().Contain("The struct 'Outer'");
    }

    [Fact]
    public void Rejects_a_generic_binding_model_class()
    {
        const string source = """
            using WinDataBinding;

            namespace Demo;

            public class Model { public int Value { get; set; } }

            [GenerateWindowsBindingModel(typeof(Model))]
            public sealed partial class ModelBinder<T> { }
            """;

        var result = TestHarness.Run(source);

        TestHarness.AssertDiagnostic(result, "WGD003", DiagnosticSeverity.Error);
        Assert.Empty(result.Source);
    }

    [Fact]
    public void Rejects_an_open_generic_source_type()
    {
        // Nothing is substituted into Model<>, so there is nothing to flatten.
        const string source = """
            using WinDataBinding;

            namespace Demo;

            public class Model<T> { public T Value { get; set; } }

            [GenerateWindowsBindingModel(typeof(Model<>))]
            public sealed partial class ModelBinder { }
            """;

        var result = TestHarness.Run(source);

        TestHarness.AssertDiagnostic(result, "WGD003", DiagnosticSeverity.Error);
        Assert.Empty(result.Source);
    }

    [Fact]
    public void Reports_nothing_for_a_well_formed_model()
    {
        var source = TestSources.Wrap("public class Model { public int Value { get; set; } }");

        var result = TestHarness.AssertCompiles(source);

        Assert.Empty(result.GeneratorDiagnostics);
    }
}
