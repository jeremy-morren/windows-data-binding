namespace WinDataBinding.SourceGenerator.Internal;

/// <summary>How a value is turned into display text.</summary>
internal enum Renderer
{
    /// <summary>Cannot be rendered; a grid binds it directly or the graph is traversed instead.</summary>
    None,

    /// <summary>
    /// Offers <c>ToString(string, IFormatProvider)</c> publicly, so it is asked for text directly.
    /// </summary>
    FormattableDirect,

    /// <summary>
    /// Implements <c>System.IFormattable</c>, but not under a name we can write: explicitly, or in generated
    /// code this compilation cannot see. Reached through a cast, which works either way.
    /// </summary>
    FormattableByCast,

    /// <summary>Already text. Only ever an element renderer: a string property is a leaf and renders itself.</summary>
    Text,

    /// <summary>
    /// An enum, which names itself with the no-argument <c>ToString()</c>.
    /// <c>Enum.ToString(string, IFormatProvider)</c> is obsolete — it ignores the provider — so taking the
    /// formattable route would put a CS0618 in the consumer's build.
    /// </summary>
    Enum,

    /// <summary><c>System.Text.Json.Nodes.JsonNode</c> or a type deriving from it.</summary>
    JsonNode,

    /// <summary><c>System.Text.Json.JsonElement</c>.</summary>
    JsonElement,
}

/// <summary>What the traversal needs to know about a type, computed once and cached.</summary>
/// <param name="IsSequence">Bound as-is rather than traversed. Never true for a JSON value.</param>
/// <param name="Renderer">How the type itself renders, when it is not a sequence.</param>
/// <param name="ElementRenderer">How the sequence's elements render.</param>
/// <param name="Count">How to read the number of items the type holds.</param>
/// <param name="ElementIsLifted">
/// Whether an element can be null, so rendering it has to go through <c>?.</c>. True for a reference type
/// and for <c>Nullable{T}</c>; <c>string.Join</c> turns the null that then comes back into an empty entry.
/// </param>
internal readonly record struct TypeTraits(
    bool IsSequence,
    Renderer Renderer,
    Renderer ElementRenderer,
    CountAccess Count = default,
    bool ElementIsLifted = false);

/// <summary>How the number of items a collection holds is reached. Both null when there is no count to read.</summary>
/// <param name="Member">A member to read directly, <c>Count</c> or <c>Length</c>.</param>
/// <param name="Cast">
/// The interface to read <c>Count</c> through, for a type that implements it explicitly and so does not
/// offer the member under any name of its own.
/// </param>
internal readonly record struct CountAccess(string? Member, string? Cast);
