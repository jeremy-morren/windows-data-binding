using Microsoft.CodeAnalysis;

namespace WinDataBinding.SourceGenerator.Tests;

public class DiagnosticTests
{
    [Fact]
    public void Skips_a_circular_reference_and_warns()
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

                    /// <summary><c>Node?.Value</c></summary>
                    /// <remarks><see cref="Demo.Model.Node"/> <see cref="Demo.Node.Value"/></remarks>
                    public int? Node_Value => _source.Node?.Value;
                }
            }
            """;

        var result = TestHarness.AssertGenerated(expected, source);
        TestHarness.AssertDiagnostic(result, "WGD001", DiagnosticSeverity.Warning);
    }

    [Fact]
    public void Detects_a_cycle_that_does_not_pass_through_the_root()
    {
        var source = TestSources.Wrap("""
            public class A { public B B { get; set; } }
            public class B { public A A { get; set; } public int Value { get; set; } }

            public class Model { public A Root { get; set; } }
            """);

        var result = TestHarness.AssertCompiles(source);

        TestHarness.AssertDiagnostic(result, "WGD001", DiagnosticSeverity.Warning);
        Assert.Contains("public int? Root_B_Value => _source.Root?.B?.Value;", result.Body);
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
    public void Rejects_a_generic_source_type()
    {
        const string source = """
            using WinDataBinding;

            namespace Demo;

            public class Model<T> { public int Value { get; set; } }

            [GenerateWindowsBindingModel(typeof(Model<int>))]
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
