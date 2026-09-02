using WinDataBinding;

namespace WinDataBinding.SourceGenerator.Tests;

public class AttributeTests
{
    /// <summary>
    /// Reading it back by reflection from another assembly is the point: the package's own definition is
    /// <c>[Conditional("JETBRAINS_ANNOTATIONS")]</c>, and a copy that kept that would be compiled away,
    /// leaving nothing in metadata for ReSharper to find.
    /// </summary>
    [Fact]
    public void Marks_the_attribute_as_meaning_implicit_use_of_the_binder_and_its_members()
    {
        var annotation = typeof(GenerateWindowsBindingModelAttribute)
            .GetCustomAttributesData()
            .Should().ContainSingle(d => d.AttributeType.FullName == "JetBrains.Annotations.MeansImplicitUseAttribute")
            .Subject;

        var argument = annotation.ConstructorArguments.Should().ContainSingle().Subject;

        argument.ArgumentType.FullName.Should().Be("JetBrains.Annotations.ImplicitUseTargetFlags");

        // WithMembers == Itself | Members == 3
        argument.Value.Should().Be(3);
    }

    [Fact]
    public void Keeps_the_annotation_types_out_of_the_public_surface()
    {
        // A public copy would be ambiguous with the real package in a project referencing both.
        var assembly = typeof(GenerateWindowsBindingModelAttribute).Assembly;

        assembly.GetExportedTypes().Should().NotContain(t => t.Namespace == "JetBrains.Annotations");
        assembly.GetTypes().Should().Contain(t => t.FullName == "JetBrains.Annotations.MeansImplicitUseAttribute");
    }
}
