using System.Collections.Immutable;
using System.Text;

namespace WinDataBinding.SourceGenerator.Internal;

/// <summary>
/// The ordered member names leading from the source type to a generated property.
/// Immutable and structurally equatable.
/// </summary>
internal sealed class PropertyChain : IEquatable<PropertyChain>
{
    public static readonly PropertyChain Empty = new(ImmutableList<string>.Empty);

    private readonly ImmutableList<string> _segments;

    private PropertyChain(ImmutableList<string> segments) => _segments = segments;

    public int Count => _segments.Count;
    public string this[int index] => _segments[index];

    public PropertyChain Add(string segment) => new(_segments.Add(segment));

    /// <summary>Joins the segments with <paramref name="separator"/> repeated <paramref name="repeat"/> times.</summary>
    private string Join(int repeat)
    {
        var separator = new string('_', repeat);
        var capacity = repeat * (_segments.Count - 1);
        foreach (var segment in _segments) capacity += segment.Length;
        var builder = new StringBuilder(capacity);
        for (var i = 0; i < _segments.Count; i++)
        {
            if (i > 0) builder.Append(separator);
            builder.Append(_segments[i]);
        }
        return builder.ToString();
    }

    /// <summary>
    /// Generates the property name for the chain at <paramref name="index"/>, 
    /// on a first-come-first-served basis: chains earlier in <paramref name="chains"/> (i.e. declared earlier) keep the shorter name, 
    /// and a later chain that would collide widens its separator by one underscore until it is unique.
    /// </summary>
    public static string GetPath(IReadOnlyList<PropertyChain> chains, int index) =>
        GetPaths(chains)[index];

    /// <summary>Generates every property name in one pass. See <see cref="GetPath"/> for the rules.</summary>
    public static ImmutableArray<string> GetPaths(IReadOnlyList<PropertyChain> chains)
    {
        var taken = new HashSet<string>(StringComparer.Ordinal);
        var paths = ImmutableArray.CreateBuilder<string>(chains.Count);
        foreach (var chain in chains)
        {
            var repeat = 1;
            var path = chain.Join(repeat);
            while (!taken.Add(path))
            {
                repeat++;
                // A single-segment chain has no separator to widen, so it grows a trailing one instead.
                path = chain.Count == 1 ? path + "_" : chain.Join(repeat);
            }
            paths.Add(path);
        }
        return paths.MoveToImmutable();
    }

    public bool Equals(PropertyChain? other)
    {
        if (other is null || _segments.Count != other._segments.Count) return false;
        return !_segments
            .Where((t, i) => !string.Equals(t, other._segments[i], StringComparison.Ordinal))
            .Any();
    }

    public override bool Equals(object? obj) => Equals(obj as PropertyChain);

    public override int GetHashCode() => 
        _segments.Aggregate(17, (current, segment) => unchecked(current * 31 + segment.GetHashCode()));

    public override string ToString() => Join(1);
}
