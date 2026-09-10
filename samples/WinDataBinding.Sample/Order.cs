namespace WinDataBinding.Sample;

/// <summary>The kind of deep object graph a grid cannot bind to directly.</summary>
public sealed class Order
{
    public required string Reference { get; init; }
    public required Customer Customer { get; init; }
    public decimal Total { get; init; }
}

public sealed class Customer
{
    public required string Name { get; init; }
    public Address? BillingAddress { get; init; }
}

public sealed class Address
{
    public required string City { get; init; }
    public required string Country { get; init; }
}
