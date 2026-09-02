using System.Reflection;

namespace WinDataBinding.SourceGenerator.Tests;

public class VersionStampTests
{
    [Fact]
    public void Stamps_the_generators_package_version_suffix_and_all()
    {
        // The package version, not the assembly version: the latter is deliberately stable across a release
        // line, so it cannot tell one build of a prerelease from another.
        var informational = typeof(WindowsBindingModelGenerator).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;

        var expected = informational.Split('+')[0];

        var result = TestHarness.Run(TestSources.Wrap("public class Model { public int Value { get; set; } }"));

        result.RawSource.Should().Contain(
            $"""[global::System.CodeDom.Compiler.GeneratedCode("WinDataBinding.SourceGenerator", "{expected}")]""");

        // The '+<commit>' the build appends is metadata about the build, not part of the version.
        result.RawSource.Should().NotContain("+");
    }
}
