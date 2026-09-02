using System.Collections.Immutable;
using System.Text;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Access = Microsoft.CodeAnalysis.Accessibility;

namespace WinDataBinding.SourceGenerator.Internal;

/// <summary>
/// Turns the attributed class and its source type into a <see cref="BinderModel"/>. All symbol work happens
/// here so the pipeline only ever carries equatable data.
/// </summary>
internal static class Parser
{
    /// <summary>Fully qualified, with <c>global::</c> and language keywords, for emitted type names.</summary>
    private static readonly SymbolDisplayFormat TypeFormat = SymbolDisplayFormat.FullyQualifiedFormat;

    /// <summary>Namespace-qualified without <c>global::</c>, for XML doc <c>cref</c>s.</summary>
    private static readonly SymbolDisplayFormat CrefFormat = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);

    /// <summary>Namespace-qualified, generics dropped, for matching against the known-type tables.</summary>
    private static readonly SymbolDisplayFormat MatchFormat = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.None);

    public static BinderModel? Parse(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        if (context.TargetSymbol is not INamedTypeSymbol binder ||
            context.TargetNode is not ClassDeclarationSyntax declaration ||
            context.Attributes.Length == 0)
            return null;

        var attribute = context.Attributes[0];
        if (attribute.ConstructorArguments.Length != 1 ||
            attribute.ConstructorArguments[0].Value is not INamedTypeSymbol sourceType)
            return null;

        var location = LocationInfo.From(declaration.Identifier.GetLocation());
        var diagnostics = new List<DiagnosticInfo>();

        if (!declaration.Modifiers.Any(SyntaxKind.PartialKeyword))
            diagnostics.Add(DiagnosticInfo.Create(Diagnostics.NotPartial, location, binder.Name));

        // Every enclosing type has to be partial too, or the generated nesting will not compile.
        var containingTypes = new List<string>();
        for (var parent = declaration.Parent as TypeDeclarationSyntax;
             parent is not null;
             parent = parent.Parent as TypeDeclarationSyntax)
        {
            if (!parent.Modifiers.Any(SyntaxKind.PartialKeyword))
                diagnostics.Add(DiagnosticInfo.Create(Diagnostics.ContainingTypeNotPartial,
                    LocationInfo.From(parent.Identifier.GetLocation()), parent.Identifier.Text));
            containingTypes.Insert(0, parent.Identifier.Text);
        }

        if (binder.IsGenericType)
            diagnostics.Add(DiagnosticInfo.Create(Diagnostics.GenericType, location, binder.Name));
        if (sourceType.IsGenericType)
            diagnostics.Add(DiagnosticInfo.Create(Diagnostics.GenericType, location, sourceType.Name));

        var properties = binder.IsGenericType || sourceType.IsGenericType
            ? ImmutableArray<GeneratedProperty>.Empty
            : Collect(sourceType, new KnownTypeSymbols(context.SemanticModel.Compilation), diagnostics, location, ct);

        var ns = binder.ContainingNamespace.IsGlobalNamespace
            ? null
            : binder.ContainingNamespace.ToDisplayString(CrefFormat);

        return new BinderModel(
            ns,
            EquatableArray.Create(containingTypes),
            binder.Name,
            sourceType.ToDisplayString(TypeFormat),
            Accessibility(sourceType.DeclaredAccessibility),
            HintName(ns, containingTypes, binder.Name),
            new EquatableArray<GeneratedProperty>(properties),
            EquatableArray.Create(diagnostics));
    }

    // -- traversal ------------------------------------------------------------------------------

    /// <summary>The state carried down one branch of the object graph.</summary>
    private readonly record struct Path(
        PropertyChain Chain,
        string Safe,
        string Unchecked,
        string Accessor,
        bool Nullable,
        ImmutableArray<string> Remarks,
        ImmutableArray<string> Descriptions)
    {
        public static Path Root { get; } = new(
            PropertyChain.Empty, "_source", "_source", ".", false,
            ImmutableArray<string>.Empty, ImmutableArray<string>.Empty);

        public Path Append(ISymbol member, ITypeSymbol memberType)
        {
            // A link needs a null-conditional accessor after it when it can itself be null; _source never can.
            var lifted = memberType.IsReferenceType || IsNullableValue(memberType);
            return new Path(
                Chain.Add(member.Name),
                Safe + Accessor + member.Name,
                Unchecked + "." + member.Name,
                lifted ? "?." : ".",
                Nullable || lifted,
                Remarks.Add(member.ContainingType.ToDisplayString(CrefFormat) + "." + member.Name),
                Descriptions.Add(Summary(member)));
        }
    }

    /// <summary>
    /// Placeholder for a sibling property's name, which is only known once collisions are resolved.
    /// </summary>
    private const string SiblingPlaceholder = "$sibling$";

    /// <param name="SiblingIndex">
    /// Index of the candidate whose resolved name replaces <see cref="SiblingPlaceholder"/> in
    /// <paramref name="Expression"/>, or -1 when the expression stands alone.
    /// </param>
    private sealed record Candidate(
        PropertyChain Chain,
        string Type,
        string Expression,
        string? TypePre6,
        string? ExpressionPre6,
        ImmutableArray<string> Remarks,
        string? Description,
        int SiblingIndex = -1);

    private static ImmutableArray<GeneratedProperty> Collect(
        INamedTypeSymbol sourceType, KnownTypeSymbols known, List<DiagnosticInfo> diagnostics,
        LocationInfo? location, CancellationToken ct)
    {
        var candidates = new List<Candidate>();
        var visited = ImmutableHashSet.Create<INamedTypeSymbol>(SymbolEqualityComparer.Default, sourceType);
        Walk(sourceType, Path.Root, visited, known, candidates, diagnostics, location, ct);

        // Names are assigned only once every chain is known, first-come-first-served in declaration order.
        var names = PropertyChain.GetPaths(candidates.ConvertAll(c => c.Chain));

        var properties = ImmutableArray.CreateBuilder<GeneratedProperty>(candidates.Count);
        for (var i = 0; i < candidates.Count; i++)
        {
            var candidate = candidates[i];
            var expression = candidate.SiblingIndex < 0
                ? candidate.Expression
                : candidate.Expression.Replace(SiblingPlaceholder, names[candidate.SiblingIndex]);

            properties.Add(new GeneratedProperty(
                names[i],
                candidate.Type,
                expression,
                StripSource(expression),
                candidate.TypePre6,
                candidate.ExpressionPre6,
                candidate.ExpressionPre6 is null ? null : StripSource(candidate.ExpressionPre6),
                EquatableArray.Create(candidate.Remarks),
                candidate.Description));
        }
        return properties.MoveToImmutable();
    }

    private static void Walk(
        INamedTypeSymbol type, Path path, ImmutableHashSet<INamedTypeSymbol> visited, KnownTypeSymbols known,
        List<Candidate> candidates, List<DiagnosticInfo> diagnostics, LocationInfo? location, CancellationToken ct)
    {
        foreach (var (member, memberType) in GetMembers(type))
        {
            ct.ThrowIfCancellationRequested();

            var next = path.Append(member, memberType);
            var underlying = Unwrap(memberType);
            var name = underlying.ToDisplayString(MatchFormat);

            if (known.IsSequence(underlying))
            {
                candidates.Add(PassThrough(next, underlying));

                // A sequence of formattable elements also gets a rendered form, for display in a grid column.
                if (known.IsFormattableSequence(underlying))
                {
                    var displayIndex = candidates.Count;
                    candidates.Add(Display(next));
                    candidates.Add(Rendered(next, displayIndex));
                }
                continue;
            }

            if (underlying.TypeKind == TypeKind.Enum || KnownTypes.IsLeaf(name))
            {
                candidates.Add(PassThrough(next, underlying));
                continue;
            }

            if (KnownTypes.TryGetConversions(name, out var conversions))
            {
                foreach (var conversion in conversions)
                    candidates.Add(Convert(next, conversion));
                continue;
            }

            if (underlying is not INamedTypeSymbol complex ||
                complex.TypeKind is not (TypeKind.Class or TypeKind.Struct or TypeKind.Interface))
                continue;

            if (visited.Contains(complex))
            {
                diagnostics.Add(DiagnosticInfo.Create(
                    Diagnostics.CircularReference, location, next.Chain.ToString(), complex.Name));
                continue;
            }

            Walk(complex, next, visited.Add(complex), known, candidates, diagnostics, location, ct);
        }
    }

    /// <summary>
    /// Public instance properties with a public getter, and public instance fields, walking up the base chain.
    /// Members declared on the type itself come first.
    /// </summary>
    private static IEnumerable<(ISymbol Member, ITypeSymbol Type)> GetMembers(INamedTypeSymbol type)
    {
        for (var current = type;
             current is not null && current.SpecialType != SpecialType.System_Object;
             current = current.BaseType)
        {
            foreach (var member in current.GetMembers())
            {
                if (member.IsStatic || member.IsImplicitlyDeclared || member.DeclaredAccessibility != Access.Public)
                    continue;

                switch (member)
                {
                    case IPropertySymbol { GetMethod: { } getter, Parameters.IsEmpty: true } property
                        when getter.DeclaredAccessibility == Access.Public:
                        yield return (property, property.Type);
                        break;
                    case IFieldSymbol { AssociatedSymbol: null } field:
                        yield return (field, field.Type);
                        break;
                }
            }
        }
    }

    private static Candidate PassThrough(Path path, ITypeSymbol underlying)
    {
        var nullable = underlying.IsReferenceType || path.Nullable;
        return new Candidate(
            path.Chain,
            underlying.ToDisplayString(TypeFormat) + (nullable ? "?" : ""),
            path.Safe,
            null,
            null,
            path.Remarks,
            Description(path.Descriptions, null));
    }

    /// <summary>
    /// The elements joined into one string, or null when the sequence itself is null.
    /// </summary>
    private static Candidate Display(Path path) => new(
        path.Chain.Add("Display"),
        "string?",
        $"{path.Safe} is {{ }} items ? global::System.String.Join(\", \", " +
        "global::System.Linq.Enumerable.Select(items, item => ((global::System.IFormattable)item)" +
        ".ToString(null, null))) : null",
        null,
        null,
        path.Remarks,
        Description(path.Descriptions, "Display"));

    /// <summary>The display string in brackets, reading the sibling rather than rebuilding it.</summary>
    private static Candidate Rendered(Path path, int displayIndex) => new(
        path.Chain.Add("Array"),
        "string?",
        SiblingPlaceholder + " is { } display ? $\"[{display}]\" : null",
        null,
        null,
        path.Remarks,
        Description(path.Descriptions, "Array"),
        displayIndex);

    private static Candidate Convert(Path path, Conversion conversion)
    {
        var context = new ExprContext(path.Safe, path.Unchecked, path.Accessor, path.Nullable);
        var nullable = conversion.IsReference || conversion.ForceNullable || path.Nullable;
        var suffix = nullable ? "?" : "";

        return new Candidate(
            conversion.Suffix is null ? path.Chain : path.Chain.Add(conversion.Suffix),
            conversion.Type + suffix,
            conversion.Build(context),
            conversion.TypePre6 is null ? null : conversion.TypePre6 + suffix,
            conversion.BuildPre6?.Invoke(context),
            path.Remarks,
            Description(path.Descriptions, conversion.Suffix));
    }

    // -- helpers --------------------------------------------------------------------------------

    private static ITypeSymbol Unwrap(ITypeSymbol type) =>
        IsNullableValue(type) ? ((INamedTypeSymbol)type).TypeArguments[0] : type;

    private static bool IsNullableValue(ITypeSymbol type) =>
        type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;

    private static string StripSource(string expression) => expression.Replace("_source.", "");

    /// <summary>Joins the doc summaries along the chain with ": ", appending the member suffix for multi-property types.</summary>
    private static string? Description(ImmutableArray<string> descriptions, string? suffix)
    {
        var parts = descriptions.Where(d => d.Length > 0).ToArray();
        if (parts.Length == 0) return null;
        var text = string.Join(": ", parts);
        return suffix is null ? text : text + " (" + suffix + ")";
    }

    /// <summary>Reads the summary element of a member's XML doc comment, collapsed to a single line.</summary>
    private static string Summary(ISymbol member)
    {
        var xml = member.GetDocumentationCommentXml();
        if (string.IsNullOrWhiteSpace(xml)) return "";

        string? text;
        try
        {
            text = XDocument.Parse(xml).Root?.Element("summary")?.Value;
        }
        catch (System.Xml.XmlException)
        {
            return "";
        }
        if (text is null) return "";

        var builder = new StringBuilder(text.Length);
        foreach (var word in text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
        {
            if (builder.Length > 0) builder.Append(' ');
            builder.Append(word);
        }
        return builder.ToString();
    }

    private static string Accessibility(Access accessibility) => accessibility switch
    {
        Access.Public => "public",
        Access.Internal => "internal",
        Access.Protected => "protected",
        Access.ProtectedOrInternal => "protected internal",
        Access.ProtectedAndInternal => "private protected",
        Access.Private => "private",
        _ => "internal",
    };

    private static string HintName(string? ns, List<string> containingTypes, string className)
    {
        var builder = new StringBuilder((ns?.Length ?? 0) + className.Length + containingTypes.Count * 16 + 8);
        if (ns is not null) builder.Append(ns).Append('.');
        foreach (var containing in containingTypes) builder.Append(containing).Append('.');
        builder.Append(className).Append(".g.cs");
        return builder.ToString();
    }
}
