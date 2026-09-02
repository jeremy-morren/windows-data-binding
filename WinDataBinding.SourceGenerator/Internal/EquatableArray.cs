using System.Collections;
using System.Collections.Immutable;

namespace WinDataBinding.SourceGenerator.Internal;

/// <summary>
/// An <see cref="ImmutableArray{T}"/> with structural equality, so pipeline models stay cacheable.
/// <see cref="ImmutableArray{T}"/> alone compares by underlying array reference, which defeats incremental caching.
/// </summary>
internal readonly struct EquatableArray<T>(ImmutableArray<T> items)
    : IEquatable<EquatableArray<T>>, IReadOnlyList<T>
    where T : IEquatable<T>
{
    public static readonly EquatableArray<T> Empty = new(ImmutableArray<T>.Empty);

    private readonly ImmutableArray<T> _items = items.IsDefault ? ImmutableArray<T>.Empty : items;

    public int Count => _items.Length;
    public T this[int index] => _items[index];
    public bool IsEmpty => _items.Length == 0;

    public bool Equals(EquatableArray<T> other)
    {
        if (_items.Length != other._items.Length) return false;
        for (var i = 0; i < _items.Length; i++)
            if (!_items[i].Equals(other._items[i]))
                return false;
        return true;
    }

    public override bool Equals(object? obj) => obj is EquatableArray<T> other && Equals(other);

    public override int GetHashCode()
    {
        var hash = 17;
        foreach (var item in _items)
            hash = unchecked(hash * 31 + item.GetHashCode());
        return hash;
    }

    public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)_items).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

internal static class EquatableArray
{
    public static EquatableArray<T> Create<T>(IEnumerable<T> items) where T : IEquatable<T> =>
        new(items.ToImmutableArray());
}
