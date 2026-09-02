using System.Collections.Immutable;
using System.Text;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Access = Microsoft.CodeAnalysis.Accessibility;

namespace WinDataBinding.SourceGenerator.Internal;

/// <summary>
/// Turns the attributed class and its source type into a <see cref="BinderModel"/>. 
/// All symbol work happens here so the pipeline only ever carries equatable data.
/// </summary>
internal static class Parser
{
    public static BinderModel? Parse(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        if (context.TargetSymbol is not INamedTypeSymbol binder ||
            context.TargetNode is not TypeDeclarationSyntax declaration ||
            context.Attributes.Length == 0)
            return null;

        // The model type comes first; a second argument names the generation options type.
        var attribute = context.Attributes[0];
        var arguments = attribute.ConstructorArguments;
        if (arguments.Length is not (1 or 2)) return null;

        if (arguments[0].Value is not INamedTypeSymbol sourceType) return null;

        var known = new KnownTypeSymbols(context.SemanticModel.Compilation);
        var options = GeneratorOptions.From(
            arguments.Length == 2 ? arguments[1].Value as INamedTypeSymbol : null, known);

        var location = LocationInfo.From(declaration.Identifier.GetLocation());
        var diagnostics = new List<DiagnosticInfo>();

        // The message names whatever was actually decorated: a class, a struct, a record.
        if (!declaration.Modifiers.Any(SyntaxKind.PartialKeyword))
            diagnostics.Add(DiagnosticInfo.Create(
                Diagnostics.NotPartial, location, declaration.Keyword.ValueText, binder.Name));

        // Every enclosing type has to be partial too, or the generated nesting will not compile.
        var containingTypes = new List<string>();
        for (var parent = declaration.Parent as TypeDeclarationSyntax;
             parent is not null;
             parent = parent.Parent as TypeDeclarationSyntax)
        {
            if (!parent.Modifiers.Any(SyntaxKind.PartialKeyword))
                diagnostics.Add(DiagnosticInfo.Create(Diagnostics.ContainingTypeNotPartial,
                    LocationInfo.From(parent.Identifier.GetLocation()),
                    parent.Keyword.ValueText, parent.Identifier.Text));
            containingTypes.Insert(0, parent.Keyword.ValueText + " " + parent.Identifier.Text);
        }

        if (binder.IsGenericType)
            diagnostics.Add(DiagnosticInfo.Create(Diagnostics.GenericType, location, binder.Name));
        if (sourceType.IsGenericType)
            diagnostics.Add(DiagnosticInfo.Create(Diagnostics.GenericType, location, sourceType.Name));

        var properties = binder.IsGenericType || sourceType.IsGenericType
            ? ImmutableArray<GeneratedProperty>.Empty
            : Collect(binder, sourceType, known, options, diagnostics, location, ct).Properties;

        var ns = binder.ContainingNamespace.IsGlobalNamespace
            ? null
            : binder.ContainingNamespace.ToDisplayString(Formats.Cref);

        return new BinderModel(
            ns,
            EquatableArray.Create(containingTypes),
            declaration.Keyword.ValueText,
            binder.Name,
            sourceType.ToDisplayString(Formats.Type),
            sourceType.IsReferenceType,
            KnownTypeSymbols.IsComparable(sourceType),
            Accessibility(sourceType.DeclaredAccessibility),
            HintName(ns, containingTypes, binder.Name),
            known.HasContractAnnotation,
            known.HasNotNullIfNotNull,
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
        /// <summary>The source object the binder wraps.</summary>
        public static Path Source { get; } = new(
            PropertyChain.Empty, "_source", "_source", ".", false,
            ImmutableArray<string>.Empty, ImmutableArray<string>.Empty);

        /// <summary>The binder itself, for flattening the properties it declares by hand.</summary>
        public static Path This { get; } = new(
            PropertyChain.Empty, "this", "this", ".", false,
            ImmutableArray<string>.Empty, ImmutableArray<string>.Empty);

        /// <summary>
        /// Extends the chain by a mapped type's expression, which is written out exactly as it was declared.
        /// The chain of names is left alone: a mapping stands in for the wrapper rather than hanging off it,
        /// so the property keeps the name the wrapper would have had.
        /// </summary>
        public Path Map(TypeMapping mapping)
        {
            var lifted = mapping.TargetType.IsReferenceType || IsNullableValue(mapping.TargetType);
            return new Path(
                Chain,
                Safe + Accessor + mapping.Expression,
                Unchecked + "." + mapping.Expression,
                lifted ? "?." : ".",
                Nullable || lifted,
                Remarks,
                Descriptions);
        }

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

    /// <summary>
    /// Chain segment for a converted value that would otherwise take the bare name of the property it came from.
    /// It matches what a strongly typed ID calls its value, so the two read alike.
    /// </summary>
    private const string ValueSuffix = "Value";

    /// <param name="SiblingIndex">
    /// Index of the candidate whose resolved name replaces <see cref="SiblingPlaceholder"/> in <paramref name="Expression"/>, 
    /// or -1 when the expression stands alone.
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

    /// <param name="outer">
    /// Types already being traversed further up, when this binder is being flattened on behalf of one that contains it.
    /// </param>
    private static FlattenedBinder Collect(
        INamedTypeSymbol binder, INamedTypeSymbol sourceType, KnownTypeSymbols known, GeneratorOptions options,
        List<DiagnosticInfo> diagnostics, LocationInfo? location, CancellationToken ct,
        ImmutableHashSet<INamedTypeSymbol>? outer = null)
    {
        var candidates = new List<Candidate>();
        var seed = outer ?? ImmutableHashSet.Create<INamedTypeSymbol>(SymbolEqualityComparer.Default);

        Walk(sourceType, Path.Source, seed.Add(sourceType), known, options, candidates, diagnostics, location, ct);
        var fromSource = candidates.Count;

        // Properties the binder declares itself are flattened the same way, rooted at 'this' instead.
        // Only what comes out of them is emitted: the property is already there, so re-exposing it under its own name
        // would not compile, and a simple one has nothing to flatten at all.
        Walk(binder, Path.This, seed.Add(binder), known, options, candidates, diagnostics, location, ct, bare: false);

        // Names are assigned only once every chain is known, first-come-first-served in declaration order.
        var names = PropertyChain.GetPaths(candidates.ConvertAll(c => c.Chain), Reserved(binder));

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
                Strip(expression),
                candidate.TypePre6,
                candidate.ExpressionPre6,
                candidate.ExpressionPre6 is null ? null : Strip(candidate.ExpressionPre6),
                EquatableArray.Create(candidate.Remarks),
                candidate.Description));
        }
        return new FlattenedBinder(properties.MoveToImmutable(), fromSource);
    }

    private static void Walk(
        INamedTypeSymbol type, Path path, ImmutableHashSet<INamedTypeSymbol> visited, KnownTypeSymbols known,
        GeneratorOptions options, List<Candidate> candidates, List<DiagnosticInfo> diagnostics,
        LocationInfo? location, CancellationToken ct, string? exclude = null, bool bare = true)
    {
        foreach (var (member, memberType) in GetMembers(type))
        {
            ct.ThrowIfCancellationRequested();

            if (exclude is not null && member.Name == exclude) continue;

            var next = path.Append(member, memberType, known);
            var underlying = Unwrap(memberType);

            // A mapped type is swapped out whole, before anything else looks at it: the wrapper is not walked,
            // and everything below classifies the target as though the member had been declared with it.
            // That order is what lets a mapping override a built-in type as well as describe an unknown one.
            var mapped = options.TryGetMapping(underlying, out var mapping);
            if (mapped)
            {
                next = next.Map(mapping);
                underlying = Unwrap(mapping.TargetType);
            }

            // Whether the value binds under this chain at all, and under what name. A property the binder
            // declares owns its own name, so nothing generated may take it — but a mapping yields a different
            // value, which takes a _Value segment rather than being dropped, exactly as a conversion does.
            var self = bare || mapped;
            var tail = bare ? null : ValueSuffix;

            var name = underlying.ToDisplayString(Formats.Match);

            var renderer = known.GetRenderer(underlying);
            if (renderer is Renderer.JsonNode or Renderer.JsonElement)
            {
                candidates.Add(Formatted(next, renderer));
                continue;
            }

            if (known.IsSequence(underlying))
            {
                if (self) candidates.Add(PassThrough(next, underlying, tail));

                // Anything that can say how long it is gets a count, which a grid can show on its own.
                if (known.GetCount(underlying) is var count && count != default)
                    candidates.Add(Convert(next, count.Member is { } read
                        ? Conversions.Tail("Count", "int", read)
                        : Conversions.Cast("Count", "int", count.Cast!, "Count")));

                // A sequence of renderable elements also gets a rendered form, for a grid column.
                var element = known.GetElementRenderer(underlying);
                if (element != Renderer.None)
                {
                    var displayIndex = candidates.Count;
                    candidates.Add(Display(next, element));
                    candidates.Add(Rendered(next, displayIndex));
                }
                continue;
            }

            if (underlying.TypeKind == TypeKind.Enum || KnownTypes.IsLeaf(name))
            {
                if (self) candidates.Add(PassThrough(next, underlying, tail));
                continue;
            }

            if (KnownTypes.TryGetConversions(name, out var conversions))
            {
                // A conversion with no suffix of its own would take the bare name, which is spoken for here.
                foreach (var conversion in conversions)
                    candidates.Add(Convert(next, conversion, tail));

                AddFormatted(next, renderer, candidates);
                continue;
            }

            var strongId = known.GetStrongId(underlying);
            if (strongId.Kind != StrongIdKind.None)
            {
                if (!TryBind(strongId, options, out var binding))
                {
                    diagnostics.Add(DiagnosticInfo.Create(Diagnostics.CustomStrongIdTemplate,
                        location, next.Chain.ToString(), underlying.Name));
                    continue;
                }

                // Whatever the ID declares in source binds too.
                // Its generated members, the value property among them, are invisible to us, 
                // so only what is statically there is reachable here.
                var id = underlying as INamedTypeSymbol;
                var walk = id is not null && !visited.Contains(id);
                var alone = bare && (!walk || !HasMembersBesides(id!, binding.PropertyName));

                // Standing alone the value takes the bare name; sharing with members it takes a suffix.
                var suffix = alone ? null : binding.PropertyName;
                candidates.Add(Convert(next, Conversions.Tail(
                    suffix, binding.ValueType, binding.PropertyName, binding.IsReference)));

                if (walk)
                    Walk(id!, next, visited.Add(id!), known, options, candidates, diagnostics, location, ct,
                        binding.PropertyName);

                AddStrongIdFormatted(next, binding, suffix, candidates);
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

            // The object itself binds too, before the members flattened out of it.
            if (self) candidates.Add(PassThrough(next, underlying, tail));
            Walk(complex, next, visited.Add(complex), known, options, candidates, diagnostics, location, ct);
            AddFormatted(next, renderer, candidates);
            AddNested(complex, next, visited, known, candidates, ct);
        }
    }

    /// <summary>
    /// What a nested binder flattens out of its own source object, read straight off its generated properties by name
    /// rather than rebuilt from the graph behind them.
    /// </summary>
    /// <remarks>
    /// That generated half is not in the compilation this transform sees — a generator never reads its own output —
    /// but this is the code that writes it, so its names and types are known exactly. Only the source half is spliced
    /// in: the members the nested binder declares by hand are real members, and walking it has bound them already.
    /// </remarks>
    private static void AddNested(
        INamedTypeSymbol binder, Path path, ImmutableHashSet<INamedTypeSymbol> visited, KnownTypeSymbols known,
        List<Candidate> candidates, CancellationToken ct)
    {
        if (!known.IsLocal(binder) || !known.TryGetBinder(binder, out var source, out var optionsType)) return;
        if (binder.IsGenericType || source.IsGenericType) return;

        if (!known.TryGetFlattened(binder, out var flattened))
        {
            // The nested binder reports its own diagnostics when it is generated; repeating them here would only
            // duplicate them against a second location.
            var ignored = new List<DiagnosticInfo>();
            flattened = Collect(binder, source, known, GeneratorOptions.From(optionsType, known),
                ignored, null, ct, visited.Add(binder));

            // A branch pruned as circular was pruned because of where we came from, so that result is ours alone.
            if (!ignored.Any(d => ReferenceEquals(d.Descriptor, Diagnostics.CircularReference)))
                known.SetFlattened(binder, flattened);
        }

        for (var i = 0; i < flattened.FromSource; i++)
        {
            var property = flattened.Properties[i];
            var expression = path.Safe + path.Accessor + property.Name;

            candidates.Add(new Candidate(
                path.Chain.Add(property.Name),
                Lift(property.Type, path.Nullable),
                expression,
                property.TypePre6 is null ? null : Lift(property.TypePre6, path.Nullable),
                property.TypePre6 is null ? null : expression,
                path.Remarks.AddRange(property.Remarks),
                Join(Description(path.Descriptions, null), property.Description)));
        }
    }

    /// <summary>Makes a type nullable when the chain reaching it can be.</summary>
    private static string Lift(string type, bool nullable) =>
        nullable && !type.EndsWith("?", StringComparison.Ordinal) ? type + "?" : type;

    /// <summary>Joins two descriptions the way one chain of summaries is joined.</summary>
    private static string? Join(string? left, string? right) =>
        left is null ? right : right is null ? left : left + ": " + right;

    private static bool HasMembersBesides(INamedTypeSymbol type, string exclude)
    {
        foreach (var (member, _) in GetMembers(type))
            if (member.Name != exclude)
                return true;

        return false;
    }

    /// <summary>
    /// The rendered twin of a strongly typed ID's value. 
    /// Unlike other leaf values, an ID gets one: the point of the wrapper is that the raw value rarely means anything on its own.
    /// </summary>
    private static void AddStrongIdFormatted(
        Path path, StrongIdBinding binding, string? suffix, List<Candidate> candidates)
    {
        if (binding.Renderer == Renderer.None) return;

        var target = binding.RendersSelf ? path.Safe : path.Safe + path.Accessor + binding.PropertyName;
        var accessor = binding.RendersSelf ? path.Accessor : binding.IsReference ? "?." : ".";
        var chain = suffix is null ? path.Chain : path.Chain.Add(suffix);

        candidates.Add(new Candidate(
            chain.Add("Formatted"),
            "string?",
            KnownTypes.RenderValue(binding.Renderer, target, accessor),
            null,
            null,
            path.Remarks,
            Description(path.Descriptions, "Formatted")));
    }

    /// <summary>How the strongly typed ID exposes its value: a built-in template, or one the options declare.</summary>
    private static bool TryBind(StrongId strongId, GeneratorOptions options, out StrongIdBinding binding)
    {
        if (strongId.Kind == StrongIdKind.Template && strongId.Template is { } template)
            return KnownTypes.TryGetStrongIdTemplate(template, out binding);

        // One configuration per ID: the first template the options describe, in the order the ID named them.
        foreach (var custom in strongId.CustomTemplates)
            if (options.TryGetStrongIdTemplate(custom, out binding))
                return true;

        binding = default;
        return false;
    }

    /// <summary>
    /// Appends the rendered form of a member that can format itself, after everything else that member produced. 
    /// Leaf types and enums are left alone: a grid already renders those.
    /// </summary>
    private static void AddFormatted(Path path, Renderer renderer, List<Candidate> candidates)
    {
        if (renderer == Renderer.None) return;

        candidates.Add(Formatted(path, renderer));
    }

    /// <summary>The value rendered as text, always nullable: the rendering itself may return null.</summary>
    private static Candidate Formatted(Path path, Renderer renderer) => new(
        path.Chain.Add("Formatted"),
        "string?",
        KnownTypes.RenderValue(renderer, path.Safe, path.Accessor),
        null,
        null,
        path.Remarks,
        Description(path.Descriptions, "Formatted"));

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

    /// <param name="suffix">Chain segment for a value that may not take the bare name, or null to leave it bare.</param>
    private static Candidate PassThrough(Path path, ITypeSymbol underlying, string? suffix = null)
    {
        var nullable = underlying.IsReferenceType || path.Nullable;
        return new Candidate(
            suffix is null ? path.Chain : path.Chain.Add(suffix),
            underlying.ToDisplayString(Formats.Type) + (nullable ? "?" : ""),
            path.Safe,
            null,
            null,
            path.Remarks,
            Description(path.Descriptions, suffix));
    }

    /// <summary>
    /// The elements joined into one string, or null when the sequence itself is null.
    /// </summary>
    private static Candidate Display(Path path, Renderer element) => new(
        path.Chain.Add("Display"),
        "string?",
        // Strings are their own display text, so they are joined as they stand rather than projected first.
        $"{path.Safe} is {{ }} items ? global::System.String.Join(\", \", " +
        (element == Renderer.Text
            ? "items)"
            : $"global::System.Linq.Enumerable.Select(items, item => {KnownTypes.RenderElement(element, "item")}))")
        + " : null",
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

    /// <param name="fallback">Chain segment for a conversion that has none of its own, or null to leave it bare.</param>
    private static Candidate Convert(Path path, Conversion conversion, string? fallback = null)
    {
        var context = new ExprContext(path.Safe, path.Unchecked, path.Accessor, path.Nullable);
        var nullable = conversion.IsReference || conversion.ForceNullable || path.Nullable;
        var annotation = nullable ? "?" : "";
        var segment = conversion.Suffix ?? fallback;

        return new Candidate(
            segment is null ? path.Chain : path.Chain.Add(segment),
            conversion.Type + annotation,
            conversion.Build(context),
            conversion.TypePre6 is null ? null : conversion.TypePre6 + annotation,
            conversion.BuildPre6?.Invoke(context),
            path.Remarks,
            Description(path.Descriptions, segment));
    }

    // -- helpers --------------------------------------------------------------------------------

    private static ITypeSymbol Unwrap(ITypeSymbol type) =>
        IsNullableValue(type) ? ((INamedTypeSymbol)type).TypeArguments[0] : type;

    private static bool IsNullableValue(ITypeSymbol type) =>
        type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;

    /// <summary>Drops the root from an expression, leaving the chain the summary comment shows.</summary>
    private static string Strip(string expression) =>
        expression.Replace("_source.", "").Replace("this.", "");

    /// <summary>
    /// Names the generated code cannot take: what the binder declares by hand, and what it always emits.
    /// A generated property sharing either would be a duplicate member rather than a shadowed one.
    /// </summary>
    private static IEnumerable<string> Reserved(INamedTypeSymbol binder)
    {
        yield return "_source";
        yield return "Create";
        yield return "Equals";
        yield return "GetHashCode";
        yield return "CompareTo";

        foreach (var member in binder.GetMembers())
            if (!member.IsImplicitlyDeclared)
                yield return member.Name;
    }

    /// <summary>Joins the doc summaries along the chain with ": ", appending the member suffix for multi-property types.</summary>
    private static string? Description(ImmutableArray<string> descriptions, string? suffix)
    {
        var parts = descriptions.Where(d => d.Length > 0).ToArray();
        if (parts.Length == 0) return null;
        var text = string.Join(": ", parts);
        return suffix is null ? text : text + " (" + suffix + ")";
    }

    /// <summary>
    /// Reads a member's summary as a documentation viewer would show it: inner text only, entities decoded, whitespace collapsed. 
    /// An <c>&lt;inheritdoc/&gt;</c> is followed to whatever it inherits from, repeatedly.
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
