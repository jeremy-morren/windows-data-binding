using System.Collections.Immutable;
using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace WinDataBinding.SourceGenerator.Tests;

/// <summary>
/// The generated file disables the cref warnings, because a cref may legitimately name something the
/// consuming project cannot see. That blanket suppression could just as easily be hiding a cref we wrote
/// wrongly, so these compile the generated source a second time with the pragma stripped and documentation
/// diagnostics turned on, which is the only way to find out whether a cref actually binds.
/// </summary>
public class CrefTests
{
    [Fact]
    public void Writes_crefs_that_bind_for_a_generic_declaring_type()
    {
        // A cref sits in an XML attribute, so Base<Reading> has to be written Base{Demo.Reading}.
        var source = """
            using System.Collections.Generic;
            using WinDataBinding;

            namespace Demo;

            public class Reading { public int Depth { get; set; } }

            public class Base<T>
            {
                /// <summary>The current one</summary>
                public T Current { get; set; }

                public IReadOnlyList<T> History { get; set; }
            }

            public sealed class Inherited : Base<Reading>
            {
                public string Label { get; set; }
            }

            [GenerateWindowsBindingModel(typeof(Inherited))]
            public sealed partial class InheritedBinder { }
            """;

        Unresolved(source).Should().BeEmpty();
    }

    [Fact]
    public void Writes_crefs_that_bind_through_a_nested_generic()
    {
        var source = """
            using System.Collections.Generic;
            using WinDataBinding;

            namespace Demo;

            public class Pair<TKey, TValue>
            {
                public TKey Key { get; set; }
                public TValue Value { get; set; }
            }

            public class Model { public Pair<string, int> Entry { get; set; } }

            [GenerateWindowsBindingModel(typeof(Model))]
            public sealed partial class ModelBinder { }
            """;

        Unresolved(source).Should().BeEmpty();
    }

    [Fact]
    public void Writes_crefs_that_bind_for_an_ordinary_graph()
    {
        var source = """
            using WinDataBinding;

            namespace Demo;

            public class Street { public string Name { get; set; } }

            public class Address { public Street Street { get; set; } }

            public class Model { public Address Home { get; set; } }

            [GenerateWindowsBindingModel(typeof(Model))]
            public sealed partial class ModelBinder { }
            """;

        Unresolved(source).Should().BeEmpty();
    }

    /// <summary>
    /// Compiles the model together with the generated source, minus its suppression, and returns whatever the
    /// compiler says about the crefs in it.
    /// </summary>
    private static ImmutableArray<string> Unresolved(string source)
    {
        var generated = TestHarness.Run(source).RawSource;

        var unsuppressed = string.Join("\n", generated
            .Split('\n')
            .Where(line => !line.StartsWith("#pragma warning disable", StringComparison.Ordinal)));

        // Crefs are only bound when the compiler is asked to diagnose documentation comments.
        var parseOptions = new CSharpParseOptions(LanguageVersion.Latest, DocumentationMode.Diagnose)
            .WithPreprocessorSymbols("NET6_0_OR_GREATER");

        var compilation = CSharpCompilation.Create(
            "CrefTests",
            [
                CSharpSyntaxTree.ParseText(source, parseOptions),
                CSharpSyntaxTree.ParseText(unsuppressed, parseOptions, path: "Generated.g.cs"),
            ],
            [.. Net80.References.All, TestHarness.AttributeReference],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // CS1574 and its siblings are the whole point; anything else here is not this test's business.
        string[] cref = ["CS1574", "CS1580", "CS1581", "CS1584"];

        return
        [
            .. compilation.GetDiagnostics()
                .Where(d => cref.Contains(d.Id))
                .Select(d => d.ToString()),
        ];
    }
}
