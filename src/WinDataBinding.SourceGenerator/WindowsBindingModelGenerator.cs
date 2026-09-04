using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WinDataBinding.SourceGenerator.Internal;

namespace WinDataBinding.SourceGenerator;

/// <summary>
/// Generates a flat, bindable view over a deep object graph for every class marked with <c>[GenerateWindowsBindingModel]</c>.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class WindowsBindingModelGenerator : IIncrementalGenerator
{
    private const string AttributeName = "WinDataBinding.GenerateWindowsBindingModelAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var models = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                AttributeName,
                static (node, _) => node is TypeDeclarationSyntax,
                static (attributeContext, ct) => Parser.Parse(attributeContext, ct))
            .Where(static model => model is not null);

        context.RegisterSourceOutput(models, static (production, model) => Emitter.Emit(production, model!));
    }
}
