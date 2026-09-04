namespace WinDataBinding.SourceGenerator.Internal;

/// <summary>The expression being built at the point a conversion is applied.</summary>
/// <param name="Safe">Chain expression using <c>?.</c> where a link may be null, e.g. <c>_source.LastLogin?.Timestamp</c>.</param>
/// <param name="Unchecked">Same chain with every <c>?.</c> replaced by <c>.</c>, for use inside a guarded branch.</param>
/// <param name="Accessor"><c>?.</c> or <c>.</c>, the accessor to place after the converted member.</param>
/// <param name="Nullable">Whether any link in the chain can be null, so the result must be lifted.</param>
internal readonly record struct ExprContext(string Safe, string Unchecked, string Accessor, bool Nullable);

/// <summary>Builds the right-hand side of a generated property from the chain that reaches it.</summary>
internal delegate string ExprBuilder(ExprContext context);
