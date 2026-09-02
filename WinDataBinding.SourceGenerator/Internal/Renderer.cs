namespace WinDataBinding.SourceGenerator.Internal;

/// <summary>How a value is turned into display text.</summary>
internal enum Renderer
{
    /// <summary>Cannot be rendered; a grid binds it directly or the graph is traversed instead.</summary>
    None,

    /// <summary>Implements <c>System.IFormattable</c>.</summary>
    Formattable,

    /// <summary><c>System.Text.Json.Nodes.JsonNode</c> or a type deriving from it.</summary>
    JsonNode,

    /// <summary><c>System.Text.Json.JsonElement</c>.</summary>
    JsonElement,
}

/// <summary>What the traversal needs to know about a type, computed once and cached.</summary>
/// <param name="IsSequence">Bound as-is rather than traversed. Never true for a JSON value.</param>
/// <param name="Renderer">How the type itself renders, when it is not a sequence.</param>
/// <param name="ElementRenderer">How the sequence's elements render.</param>
internal readonly record struct TypeTraits(bool IsSequence, Renderer Renderer, Renderer ElementRenderer);
