using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Basic.Reference.Assemblies;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using WinDataBinding;

namespace WinDataBinding.SourceGenerator.Tests;

/// <summary>Which framework the generated code is compiled against, to exercise both <c>#if</c> branches.</summary>
public enum Target
{
    /// <summary>net8.0 reference assemblies with NET6_0_OR_GREATER defined.</summary>
    Net8,

    /// <summary>netstandard2.0 reference assemblies, where DateOnly and TimeOnly do not exist.</summary>
    NetStandard20,
}

/// <param name="Source">The generated source, with the stamped tool version normalised.</param>
/// <param name="RawSource">The generated source exactly as written, for asserting on that version itself.</param>
public sealed record GeneratorResult(
    string Source,
    string RawSource,
    ImmutableArray<Diagnostic> GeneratorDiagnostics,
    ImmutableArray<Diagnostic> CompilationDiagnostics)
{
    /// <summary>
    /// Warnings coming from the generated file itself. These land in the consumer's build, so the generator
    /// has to avoid them; warnings from the test's own model source are not our business.
    /// </summary>
    public ImmutableArray<Diagnostic> GeneratedCodeWarnings =>
        [.. CompilationDiagnostics.Where(d =>
            d.Severity == DiagnosticSeverity.Warning &&
            d.Location.SourceTree?.FilePath.EndsWith(".g.cs", StringComparison.Ordinal) == true)];

    /// <summary>Compilation errors that are not our own reported diagnostics.</summary>
    public ImmutableArray<Diagnostic> CompilationErrors =>
        [.. CompilationDiagnostics.Where(d =>
            d.Severity == DiagnosticSeverity.Error && !d.Id.StartsWith("WGD", StringComparison.Ordinal))];

    /// <summary>The generated source with the fixed file header stripped, so expectations stay readable.</summary>
    public string Body
    {
        get
        {
            const string marker = "#nullable disable warnings";
            var index = Source.IndexOf(marker, StringComparison.Ordinal);
            return index < 0 ? Source.Trim() : Source[(index + marker.Length)..].Trim();
        }
    }
}

public static class TestHarness
{
    internal static readonly MetadataReference AttributeReference =
        MetadataReference.CreateFromFile(typeof(GenerateWindowsBindingModelAttribute).Assembly.Location);

    private static readonly MetadataReference NodaTimeNet8Reference =
        MetadataReference.CreateFromFile(typeof(NodaTime.Instant).Assembly.Location);

    /// <summary>NodaTime's netstandard2.0 asset, copied next to the tests by the project file.</summary>
    private static readonly MetadataReference NodaTimeNetStandardReference =
        MetadataReference.CreateFromFile(
            Path.Combine(AppContext.BaseDirectory, "refs", "netstandard2.0", "NodaTime.dll"));

    public static GeneratorResult Run(string source, Target target = Target.Net8)
    {
        // The generated file is parsed with these options too, so the preprocessor symbol decides
        // which side of the #if the compiler actually sees.
        var parseOptions = new CSharpParseOptions(
            LanguageVersion.Latest,
            preprocessorSymbols: target == Target.Net8 ? ["NET6_0_OR_GREATER"] : []);

        var (references, nodaTime) = target == Target.Net8
            ? (Net80.References.All, NodaTimeNet8Reference)
            : (NetStandard20.References.All, NodaTimeNetStandardReference);

        var compilation = CSharpCompilation.Create(
            "WinDataBindingTests",
            [CSharpSyntaxTree.ParseText(source, parseOptions)],
            [.. references, AttributeReference, nodaTime],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var driver = CSharpGeneratorDriver
            .Create([new WindowsBindingModelGenerator().AsSourceGenerator()], parseOptions: parseOptions)
            .RunGeneratorsAndUpdateCompilation(compilation, out var output, out _);

        var runResult = driver.GetRunResult();
        var generated = runResult.GeneratedTrees.Length == 0
            ? ""
            : string.Join("\n\n", runResult.GeneratedTrees.Select(t => t.ToString()));

        var raw = generated.Replace("\r\n", "\n");

        return new GeneratorResult(
            Normalize(generated),
            raw,
            runResult.Diagnostics,
            output.GetDiagnostics());
    }

    /// <summary>Asserts the generated body matches, and that nothing else failed to compile.</summary>
    public static GeneratorResult AssertGenerated(string expectedBody, string source, Target target = Target.Net8)
    {
        var result = Run(source, target);
        result.Should().Compile().And.HaveBody(expectedBody);
        return result;
    }

    /// <summary>Asserts the generated code compiles cleanly without pinning its exact text.</summary>
    public static GeneratorResult AssertCompiles(string source, Target target = Target.Net8)
    {
        var result = Run(source, target);
        result.Should().Compile();
        return result;
    }

    public static void AssertDiagnostic(this GeneratorResult result, string id, DiagnosticSeverity severity) =>
        result.Should().HaveDiagnostic(id, severity);

    /// <summary>
    /// The version stamped into <c>[GeneratedCode]</c> is the generator's package version, so a build given a
    /// different <c>VersionSuffix</c> would otherwise fail every pinned expectation. Only that one value is
    /// rewritten, and that it is stamped at all is asserted separately.
    /// </summary>
    private static readonly Regex StampedVersion = new(
        "(?<=GeneratedCode\\(\"WinDataBinding\\.SourceGenerator\", )\"[^\"]*\"",
        RegexOptions.Compiled);

    private static string Normalize(string text) =>
        StampedVersion.Replace(text.Replace("\r\n", "\n"), "\"1.0.0\"");
}

/// <summary>Boilerplate for the small models the tests generate from.</summary>
public static class TestSources
{
    /// <summary>Wraps model declarations in a namespace with a binder for a type named <c>Model</c>.</summary>
    public static string Wrap(string body) => $$"""
        using NodaTime;
        using WinDataBinding;

        namespace Demo;

        {{body}}

        [GenerateWindowsBindingModel(typeof(Model))]
        public sealed partial class ModelBinder { }
        """;
}
