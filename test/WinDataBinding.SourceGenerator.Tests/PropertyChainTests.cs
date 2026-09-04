using WinDataBinding.SourceGenerator.Internal;

namespace WinDataBinding.SourceGenerator.Tests;

public class PropertyChainTests
{
    private static PropertyChain Chain(params string[] segments) =>
        segments.Aggregate(PropertyChain.Empty, (chain, segment) => chain.Add(segment));

    [Fact]
    public void Joins_segments_with_a_single_underscore()
    {
        Assert.Equal("LastLogin_Timestamp_Value", PropertyChain.GetPath([Chain("LastLogin", "Timestamp", "Value")], 0));
    }

    [Fact]
    public void Gives_the_shorter_name_to_whichever_chain_comes_first()
    {
        PropertyChain[] chains = [Chain("Address", "Street"), Chain("Address_Street")];

        string[] expected = ["Address_Street", "Address_Street_"];
        Assert.Equal(expected, PropertyChain.GetPaths(chains));
    }

    [Fact]
    public void Widens_the_separator_when_a_multi_segment_chain_collides()
    {
        PropertyChain[] chains = [Chain("Address_Street"), Chain("Address", "Street")];

        string[] expected = ["Address_Street", "Address__Street"];
        Assert.Equal(expected, PropertyChain.GetPaths(chains));
    }

    [Fact]
    public void Keeps_widening_until_the_name_is_free()
    {
        PropertyChain[] chains = [Chain("A_B"), Chain("A__B"), Chain("A", "B")];

        string[] expected = ["A_B", "A__B", "A___B"];
        Assert.Equal(expected, PropertyChain.GetPaths(chains));
    }

    [Fact]
    public void Compares_structurally()
    {
        Assert.Equal(Chain("A", "B"), Chain("A", "B"));
        Assert.Equal(Chain("A", "B").GetHashCode(), Chain("A", "B").GetHashCode());
        Assert.NotEqual(Chain("A", "B"), Chain("A", "C"));
        Assert.NotEqual(Chain("A", "B"), Chain("A"));
    }

    [Fact]
    public void Add_does_not_mutate_the_original()
    {
        var chain = Chain("A");
        var extended = chain.Add("B");

        Assert.Equal(1, chain.Count);
        Assert.Equal(2, extended.Count);
    }
}
