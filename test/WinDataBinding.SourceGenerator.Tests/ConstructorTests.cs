using Microsoft.CodeAnalysis;

namespace WinDataBinding.SourceGenerator.Tests;

/// <summary>
/// The constructor can be written by hand, to validate the source or to take more than it. Doing so replaces
/// the generated one outright, which leaves filling <c>_source</c> to whoever wrote it.
/// </summary>
public class ConstructorTests
{
    private static string Wrap(string binder) => $$"""
        using WinDataBinding;

        namespace Demo;

        public class Model { public int Value { get; set; } }

        [GenerateWindowsBindingModel(typeof(Model))]
        {{binder}}
        """;

    [Fact]
    public void Steps_aside_for_a_constructor_written_by_hand()
    {
        var source = Wrap("""
            public sealed partial class ModelBinder
            {
                public ModelBinder(Model source)
                {
                    System.ArgumentNullException.ThrowIfNull(source);
                    _source = source;
                }
            }
            """);

        var result = TestHarness.AssertCompiles(source);

        result.Should().HaveNoDiagnostics();

        // The field is still generated: it is what every property reads through.
        result.Source.Should().Contain("private readonly global::Demo.Model _source;");

        // The constructor is not, or the two would be duplicates of each other.
        result.Source.Should().NotContain("ModelBinder(global::Demo.Model source)");

        // It takes the source, so the factory can still call it.
        result.Source.Should().Contain("public static ModelBinder? Create(global::Demo.Model? source) =>");
        result.Source.Should().Contain("public int Value => _source.Value;");
    }

    [Fact]
    public void Warns_when_a_hand_written_constructor_never_fills_the_field()
    {
        var source = Wrap("""
            public sealed partial class ModelBinder
            {
                public ModelBinder(Model source)
                {
                    System.ArgumentNullException.ThrowIfNull(source);
                }
            }
            """);

        var result = TestHarness.Run(source);

        result.Should().HaveDiagnostic("WGD006", DiagnosticSeverity.Warning);
    }

    [Fact]
    public void Accepts_the_field_written_through_this()
    {
        var source = Wrap("""
            public sealed partial class ModelBinder
            {
                public ModelBinder(Model source) => this._source = source;
            }
            """);

        var result = TestHarness.AssertCompiles(source);

        result.Should().HaveNoDiagnostics();
    }

    [Fact]
    public void Follows_a_constructor_that_hands_the_work_to_another()
    {
        var source = Wrap("""
            public sealed partial class ModelBinder
            {
                public ModelBinder(Model source) => _source = source;

                public ModelBinder(int value) : this(new Model { Value = value }) { }
            }
            """);

        var result = TestHarness.AssertCompiles(source);

        // Neither warns: one assigns the field, the other reaches it through the first.
        result.Should().HaveNoDiagnostics();
    }

    [Fact]
    public void Warns_for_each_constructor_that_does_not_reach_the_field()
    {
        var source = Wrap("""
            public sealed partial class ModelBinder
            {
                public ModelBinder(Model source) { }

                public ModelBinder(int value) { }
            }
            """);

        var result = TestHarness.Run(source);

        result.GeneratorDiagnostics.Count(d => d.Id == "WGD006").Should().Be(2);
    }

    [Fact]
    public void Leaves_out_the_factory_when_no_constructor_takes_the_source()
    {
        // Create says 'new ModelBinder(source)', which needs a constructor shaped that way.
        var source = Wrap("""
            public sealed partial class ModelBinder
            {
                public ModelBinder(Model source, string label) => _source = source;
            }
            """);

        var result = TestHarness.AssertCompiles(source);

        result.Should().HaveNoDiagnostics();
        result.Source.Should().NotContain("Create(");
        result.Source.Should().Contain("public int Value => _source.Value;");
    }

    [Fact]
    public void Steps_aside_for_a_constructor_on_a_struct_binder()
    {
        var source = Wrap("""
            public readonly partial struct ModelBinder
            {
                public ModelBinder(Model source) => _source = source;
            }
            """);

        var result = TestHarness.AssertCompiles(source);

        // A struct has an implicit parameterless constructor, which is not one of theirs.
        result.Should().HaveNoDiagnostics();
        result.Source.Should().NotContain("ModelBinder(global::Demo.Model source)");
    }

    [Fact]
    public void Warns_for_a_primary_constructor_which_cannot_reach_the_field()
    {
        // A primary constructor has no body to assign from, and the field it would need to initialise is
        // declared in the generated half, out of its reach.
        var source = Wrap("""
            public sealed partial class ModelBinder(Model source);
            """);

        var result = TestHarness.Run(source);

        result.Should().HaveDiagnostic("WGD006", DiagnosticSeverity.Warning);
    }
}
