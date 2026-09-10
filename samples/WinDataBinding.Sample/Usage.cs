namespace WinDataBinding.Sample;

/// <summary>Touches the generated surface so a generator that emits nothing cannot pass unnoticed.</summary>
public static class Usage
{
    public static string Describe(Order order)
    {
        var binder = new OrderBinder(order);
        return $"{binder.Reference}: {binder.Customer_Name} ({binder.Customer_BillingAddress_City}) {binder.Total}";
    }
}
