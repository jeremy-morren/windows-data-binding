# Generated windows model binding

Binding deep objects (or non-primitive types such as `NodaTime`) types in WPF/WinForms can be tricky.
This project provides a source generator that generates a flat model for your deep objects.
This allows you to bind to your deep objects in WPF/WinForms without having to write a lot of boilerplate code.

## Usage

```csharp
public class Address
{
    public string Street { get; set; }
    public string City { get; set; }
        
    /// <summary>Address state</summary>
    public string State { get; set; }
}
    
public class LoginInfo
{
    public int Id { get; set; }

    /// <summary>
    /// Timestamp the login occurred at
    /// </summary>
    public ZonedDateTime Timestamp { get; set; }
}

public class Person
{
    /// <summary>Person name</summary>
    public string Name { get; set; }

    /// <summary>Person's address</summary>
    public Address Address { get; set; }

    /// <summary>The timestamp that the person was created at</summary>
    public Instant CreatedAt { get; set; }

    /// <summary>Last login in the user's local timezone</summary>
    public LoginInfo? LastLogin { get; set; }
}

[GenerateWindowsBindingModelAttribute(typeof(Person))]
public sealed partial class PersonModelBinder { }
```

The generated source will look like this. Every type is fully qualified so the generated code cannot be
broken by a name in your own project, and the `[Description]` text is the doc comments of each property in
the chain joined by `: `:

```csharp
namespace Demo
{
    [global::System.CodeDom.Compiler.GeneratedCode("WinDataBinding.SourceGenerator", "1.0.0.0")]
    partial class PersonModelBinder
    {
        private readonly global::Demo.Person _source;

        public PersonModelBinder(global::Demo.Person source)
        {
#if NET6_0_OR_GREATER
            global::System.ArgumentNullException.ThrowIfNull(source);
            _source = source;
#else
            _source = source ?? throw new global::System.ArgumentNullException(nameof(source));
#endif
        }

        /// <summary><c>Name</c></summary>
        /// <remarks><see cref="Demo.Person.Name"/></remarks>
        [global::System.ComponentModel.Description("Person name")]
        public string? Name => _source.Name;

        /// <summary><c>Address?.Street</c></summary>
        /// <remarks><see cref="Demo.Person.Address"/> <see cref="Demo.Address.Street"/></remarks>
        [global::System.ComponentModel.Description("Person's address")]
        public string? Address_Street => _source.Address?.Street;

        /// <summary><c>Address?.City</c></summary>
        /// <remarks><see cref="Demo.Person.Address"/> <see cref="Demo.Address.City"/></remarks>
        [global::System.ComponentModel.Description("Person's address")]
        public string? Address_City => _source.Address?.City;

        /// <summary><c>Address?.State</c></summary>
        /// <remarks><see cref="Demo.Person.Address"/> <see cref="Demo.Address.State"/></remarks>
        [global::System.ComponentModel.Description("Person's address: Address state")]
        public string? Address_State => _source.Address?.State;

        /// <summary><c>CreatedAt.ToDateTimeUtc()</c></summary>
        /// <remarks><see cref="Demo.Person.CreatedAt"/></remarks>
        [global::System.ComponentModel.Description("The timestamp that the person was created at")]
        public global::System.DateTime CreatedAt => _source.CreatedAt.ToDateTimeUtc();

        /// <summary><c>LastLogin?.Id</c></summary>
        /// <remarks><see cref="Demo.Person.LastLogin"/> <see cref="Demo.LoginInfo.Id"/></remarks>
        [global::System.ComponentModel.Description("Last login in the user's local timezone")]
        public int? LastLogin_Id => _source.LastLogin?.Id;

        /// <summary><c>LastLogin?.Timestamp.ToDateTimeOffset()</c></summary>
        /// <remarks><see cref="Demo.Person.LastLogin"/> <see cref="Demo.LoginInfo.Timestamp"/></remarks>
        [global::System.ComponentModel.Description("Last login in the user's local timezone: Timestamp the login occurred at (Value)")]
        public global::System.DateTimeOffset? LastLogin_Timestamp_Value => _source.LastLogin?.Timestamp.ToDateTimeOffset();

        /// <summary><c>LastLogin?.Timestamp.Zone.Id</c></summary>
        /// <remarks><see cref="Demo.Person.LastLogin"/> <see cref="Demo.LoginInfo.Timestamp"/></remarks>
        [global::System.ComponentModel.Description("Last login in the user's local timezone: Timestamp the login occurred at (Timezone)")]
        public string? LastLogin_Timestamp_Timezone => _source.LastLogin?.Timestamp.Zone.Id;
    }
}
```

## Target framework

The generated file is compiled as part of your project, so it adapts to your target framework with `#if`:

- `ArgumentNullException.ThrowIfNull` is used on NET6+, falling back to `?? throw new ArgumentNullException(...)`.
- `DateOnly` and `TimeOnly` conversions fall back to `DateTime` and `TimeSpan` (see the NodaTime table below).

Types that merely pass through (`DateOnly`, `Half`, …) need no guard: they can only appear in your model if
your target framework already has them.

## Descriptions

`[Description]` is built from the `<summary>` of every property in the chain, joined by `: `. Markup inside a
summary is reduced to its displayed form — the same text a documentation viewer would show:

- only the XML inner text is kept, and attributes are ignored;
- entities are decoded, so `&gt;` becomes `>` and `&amp;` becomes `&`;
- whitespace is collapsed, so a summary spread over several lines becomes one.

```csharp
public class Order
{
    /// <summary>Rendered with <c>ToString()</c> for display</summary>
    public string Rendered { get; set; }

    /// <summary>See <see cref="Order.Rendered"/> for the text form</summary>
    public int Raw { get; set; }

    /// <summary>Set when <c>a &gt; b</c></summary>
    public bool Compared { get; set; }
}
```

gives

```csharp
[global::System.ComponentModel.Description("Rendered with ToString() for display")]
public string? Rendered => _source.Rendered;

// <see cref="..."/> is empty, so it contributes no text of its own
[global::System.ComponentModel.Description("See for the text form")]
public int Raw => _source.Raw;

[global::System.ComponentModel.Description("Set when a > b")]
public bool Compared => _source.Compared;
```

## Types

#### Value types

`string`, `bool`, `char`, `System.Half`, `float`, `double`, `decimal`, 
`byte`, `sbyte`, `short`, `int`, `long`, `ushort`, `uint`, `ulong`,
`Uri`, `Guid`, `Version`,
`DateTime`, `DateTimeOffset`, `TimeSpan`, `DateOnly`, `TimeOnly`

Any `enum` is passed through as-is.

#### Collections

Anything inheriting from `IEnumerable<>` (except `string`) will be returned as it.

Anything inheriting from `IEnumerable<T>` where `T` is `IFormattable` will have 2 additional properties (both `string?`):
- `Property_Display` - `string.Join(", ", Property.Select(x => ((IFormattable)x).ToString(null, null)))`
- `Property_Array`- `Property_Display is { } display ? $"[{display}]" : null`

#### Common types:

| Source type | Output property |
|:-|:-|
| `TimeZoneInfo` | 2 properties: `string _Id` (`Id`) and `string _DisplayName` (`DisplayName`) |

#### `NodaTime` types:

| Source Type | Output property |
|:-|:-|
| `DateTimeZone` | `string`: `Id` |
| `Instant` | `DateTime`: `ToDateTimeUtc()` |
| `OffsetDateTime` | `DateTimeOffset`: `.ToDateTimeOffset()` |
| `ZonedDateTime` | 2 properties: `DateTimeOffset _Value` (`ToDateTimeOffset()`) and `string _Timezone` (`Zone.Id`) |
| `LocalDateTime` | `DateTime`: `ToDateTimeUnspecified()` |
| `LocalDate` | On NET6+: `DateOnly`: `ToDateOnly()`. Earlier: `DateTime`: `ToDateTimeUnspecified()` |
| `LocalTime` | On NET6+: `TimeOnly`: `.ToTimeOnly()`. Earlier: `TimeSpan.FromTicks(x.TickOfDay)` |
| `Duration` | `TimeSpan`: `.ToTimeSpan()` |
| `Offset` | `TimeSpan`: `.ToTimeSpan()` |
| `YearMonth` | On NET6+: `DateOnly`: `OnDayOfMonth(1).ToDateOnly()`. Earlier: `DateTime`: `OnDayOfMonth(1).ToDateTimeUnspecified()` |
| `Interval` | 3 properties: `DateTime? _Start` (`HasStart ? Start.ToDateTimeUtc() : null`), `DateTime? _End` (`HasEnd ? End.ToDateTimeUtc() : null`), and `TimeSpan? _Duration` (`HasStart && HasEnd ? Duration.ToTimeSpan() : null`) |
| `Period` | `string`: `.ToString()` |

