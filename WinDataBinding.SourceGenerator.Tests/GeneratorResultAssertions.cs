using System.Text;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;
using Microsoft.CodeAnalysis;

namespace WinDataBinding.SourceGenerator.Tests;

public static class GeneratorResultExtensions
{
    public static GeneratorResultAssertions Should(this GeneratorResult result) => new(result);
}

/// <summary>
/// Assertions over a generator run. Generated source is large and full of braces, which the built-in
/// string assertions cannot render in a failure message, so mismatches are written to disk instead.
/// </summary>
public class GeneratorResultAssertions(GeneratorResult subject)
    : ReferenceTypeAssertions<GeneratorResult, GeneratorResultAssertions>(subject)
{
    protected override string Identifier => "generator result";

    /// <summary>Asserts the output compiles, ignoring diagnostics this generator reported itself.</summary>
    public AndConstraint<GeneratorResultAssertions> Compile(string because = "", params object[] becauseArgs)
    {
        // CS8785 means the generator threw. Roslyn reports it as a warning and simply produces no source,
        // so without this a crashed generator looks indistinguishable from one with nothing to say.
        var crashes = Subject.GeneratorDiagnostics.Where(d => d.Id == "CS8785").ToArray();

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(crashes.Length == 0)
            .FailWith("Expected the generator not to throw{reason}, but it did: {0}.",
                () => string.Join("\n", crashes.Select(d => d.GetMessage())));

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(Subject.CompilationErrors.IsEmpty)
            .FailWith("Expected the generated code to compile{reason}, but found {0}.",
                () => string.Join("\n", Subject.CompilationErrors.Select(d => d.ToString())));

        return new AndConstraint<GeneratorResultAssertions>(this);
    }

    /// <summary>Asserts the generated source, with its fixed file header stripped, matches exactly.</summary>
    public AndConstraint<GeneratorResultAssertions> HaveBody(string expected, string because = "", params object[] becauseArgs)
    {
        var normalized = expected.Replace("\r\n", "\n").Trim();

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(Subject.Body == normalized)
            .FailWith("Expected the generated body to match{reason}, but it differed. {0}",
                () => WriteMismatch(normalized, Subject.Body));

        return new AndConstraint<GeneratorResultAssertions>(this);
    }

    public AndConstraint<GeneratorResultAssertions> HaveDiagnostic(
        string id, DiagnosticSeverity severity, string because = "", params object[] becauseArgs)
    {
        var matches = Subject.GeneratorDiagnostics.Where(d => d.Id == id).ToArray();

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(matches.Length == 1)
            .FailWith("Expected exactly one {0} diagnostic{reason}, but found {1}.",
                () => id, () => Describe(Subject.GeneratorDiagnostics));

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(matches.Length != 1 || matches[0].Severity == severity)
            .FailWith("Expected {0} to be {1}{reason}, but it was {2}.",
                () => id, () => severity, () => matches[0].Severity);

        return new AndConstraint<GeneratorResultAssertions>(this);
    }

    public AndConstraint<GeneratorResultAssertions> HaveNoDiagnostics(string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(Subject.GeneratorDiagnostics.IsEmpty)
            .FailWith("Expected no generator diagnostics{reason}, but found {0}.",
                () => Describe(Subject.GeneratorDiagnostics));

        return new AndConstraint<GeneratorResultAssertions>(this);
    }

    private static string Describe(IEnumerable<Diagnostic> diagnostics)
    {
        var text = string.Join(", ", diagnostics.Select(d => d.Id));
        return text.Length == 0 ? "none" : text;
    }

    /// <summary>
    /// Writes both sides to disk for diffing. Test classes run in parallel, so the file names carry the
    /// current test's name to keep concurrent failures from overwriting each other.
    /// </summary>
    private static string WriteMismatch(string expected, string actual)
    {
        var name = TestContext.Current.Test?.TestDisplayName ?? Guid.NewGuid().ToString("N");
        var builder = new StringBuilder(name.Length);
        foreach (var character in name)
            builder.Append(Path.GetInvalidFileNameChars().Contains(character) ? '_' : character);

        var directory = Path.Combine(AppContext.BaseDirectory, "mismatches");
        Directory.CreateDirectory(directory);

        var stem = Path.Combine(directory, builder.ToString());
        File.WriteAllText(stem + ".expected.txt", expected);
        File.WriteAllText(stem + ".actual.txt", actual);
        return $"See {stem}.expected.txt and {stem}.actual.txt";
    }
}
