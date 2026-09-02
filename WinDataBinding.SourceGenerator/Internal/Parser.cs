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
            : binder.ContainingNamespace.ToDisplayString(Formats.Cref);

        return new BinderModel(
            ns,
            EquatableArray.Create(containingTypes),
            binder.Name,
            sourceType.ToDisplayString(Formats.Type),
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

        public Path Append(ISymbol member, ITypeSymbol memberType, KnownTypeSymbols known)
        {
            // A link needs a null-conditional accessor after it when it can itself be null; _source never can.
            var lifted = memberType.IsReferenceType || IsNullableValue(memberType);
            return new Path(
                Chain.Add(member.Name),
                Safe + Accessor + member.Name,
                Unchecked + "." + member.Name,
                lifted ? "?." : ".",
                Nullable || lifted,
                Remarks.Add(member.ContainingType.ToDisplayString(Formats.Cref) + "." + member.Name),
                Descriptions.Add(Summary(member, known)));
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

            var next = path.Append(member, memberType, known);
            var underlying = Unwrap(memberType);
            var name = underlying.ToDisplayString(Formats.Match);

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

            var strongId = known.GetStrongId(underlying);
            if (strongId.Kind != StrongIdKind.None)
            {
                if (strongId.Template is { } template && KnownTypes.TryGetStrongIdTemplate(template, out var unwrap))
                    candidates.Add(Convert(next, unwrap));
                else
                    diagnostics.Add(DiagnosticInfo.Create(Diagnostics.CustomStrongIdTemplate,
                        location, next.Chain.ToString(), underlying.Name));
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
            underlying.ToDisplayString(Formats.Type) + (nullable ? "?" : ""),
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

    /// <summary>
    /// Reads a member's summary as a documentation viewer would show it: inner text only, entities decoded,
    /// whitespace collapsed. An <c>&lt;inheritdoc/&gt;</c> is followed to whatever it inherits from, repeatedly.
    /// </summary>
    private static string Summary(ISymbol member, KnownTypeSymbols known)
    {
        var visited = new HashSet<ISymbol>(SymbolEqualityComparer.Default);

        for (ISymbol? current = member; current is not null && visited.Add(current);)
        {
            var xml = current.GetDocumentationCommentXml();
            if (string.IsNullOrWhiteSpace(xml)) return "";

            XElement? root;
            try
            {
                root = XDocument.Parse(xml).Root;
            }
            catch (System.Xml.XmlException)
            {
                return "";
            }

            if (root is null) return "";
            if (root.Element("summary") is { } summary) return Collapse(summary.Value);
            if (root.Element("inheritdoc") is not { } inherited) return "";

            current = Inherited(current, inherited.Attribute("cref")?.Value, known);
        }

        return "";
    }

    /// <summary>What an <c>&lt;inheritdoc/&gt;</c> on this member points at.</summary>
    private static ISymbol? Inherited(ISymbol member, string? cref, KnownTypeSymbols known)
    {
        if (cref is not null)
            return DocumentationCommentId.GetFirstSymbolForDeclarationId(cref, known.Compilation)
                ?? DocumentationCommentId.GetFirstSymbolForReferenceId(cref, known.Compilation);

        // An override inherits from what it overrides, a type from its base.
        switch (member)
        {
            case IPropertySymbol { OverriddenProperty: { } property }: return property;
            case IMethodSymbol { OverriddenMethod: { } method }: return method;
            case INamedTypeSymbol { BaseType: { } baseType }: return baseType;
        }

        if (member.ContainingType is not { } containing) return null;

        // Otherwise it inherits from the interface member it implements.
        foreach (var @interface in containing.AllInterfaces)
            foreach (var candidate in @interface.GetMembers(member.Name))
                if (SymbolEqualityComparer.Default.Equals(
                        containing.FindImplementationForInterfaceMember(candidate), member))
                    return candidate;

        // A 'new' member hides rather than implements, so nothing claims it; match on name instead.
        foreach (var @interface in containing.AllInterfaces)
            foreach (var candidate in @interface.GetMembers(member.Name))
                if (!SymbolEqualityComparer.Default.Equals(candidate, member))
                    return candidate;

        return null;
    }

    private static string Collapse(string text)
    {
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
