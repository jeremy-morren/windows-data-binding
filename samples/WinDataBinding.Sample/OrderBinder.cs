namespace WinDataBinding.Sample;

/// <summary>
/// The flattened view of <see cref="Order"/> that a grid binds to. Every property below is generated;
/// the class is empty on purpose, so an analyzer that fails to run shows up as a compile error here.
/// </summary>
[GenerateWindowsBindingModel(typeof(Order))]
public sealed partial class OrderBinder;
