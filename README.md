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

The generated source will look like this:
```csharp
partial class PersonModelBinder
{
    private readonly Person _source;

    public PersonModelBinder(Person source)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
    }

    /// <summary><c>Name</c></summary>
    /// <remarks><see cref="Person.Name"/></remarks>
    [Description("Person name")]
    public string? Name => _source.Name;

    /// <summary><c>Address?.Street</c></summary>
    /// <remarks><see cref="Person.Address"/> <see cref="Address.Street"/></remarks>
    public string? Address_Street => _source.Address?.Street;
        
    /// <summary><c>Address?.City</c></summary>
    /// <remarks><see cref="Person.Address"/> <see cref="Address.City"/></remarks>
    public string? Address_City => _source.Address?.City;

    /// <summary><c>Address?.State</c></summary>
    /// <remarks><see cref="Person.Address"/> <see cref="Address.State"/></remarks>
    [Description("Address state")]
    public string? Address_State => _source.Address?.State;

    /// <summary><c>CreatedAt.ToDateTimeUtc()</c></summary>
    /// <remarks><see cref="Person.CreatedAt"/></remarks>
    [Description("The timestamp that the person was created at")]
    public DateTime CreatedAt => _source.CreatedAt.ToDateTimeUtc();

    /// <summary><c>LastLogin?.Id</c></summary>
    /// <remarks><see cref="Person.LastLogin"/> <see cref="LoginInfo.Id"/></remarks>
    [Description("Last login in the user's local timezone")]
    public int? LastLogin_Id => _source.LastLogin?.Id;

    /// <summary><c>LastLogin?.Timestamp.ToDateTimeOffset()</c></summary>
    /// <remarks><see cref="Person.LastLogin"/> <see cref="LoginInfo.Timestamp"/></remarks>
    [Description("Last login in the user's local timezone: Timestamp the login occurred at (Value)")]
    public DateTimeOffset? LastLogin_Timestamp_Value => _source.LastLogin?.Timestamp.ToDateTimeOffset();

    /// <summary><c>LastLogin?.Timestamp.Zone.Id</c></summary>
    /// <remarks><see cref="Person.LastLogin"/> <see cref="LoginInfo.Timestamp"/></remarks>
    [Description("Last login in the user's local timezone: Timestamp the login occurred at (Timezone)")]
    public string? LastLogin_Timestamp_Timezone => _source.LastLogin?.Timestamp.Zone.Id;
}
```

## Types

#### Value types

`string`, `bool`, `char`, `System.Half`, `float`, `double`, `decimal`, 
`byte`, `sbyte`, `short`, `int`, `long`, `ushort`, `uint`, `ulong`,
`Uri`, `Guid`, `Version`,
`DateTime`, `DateTimeOffset`, `TimeSpan`, `DateOnly`, `TimeOnly`

Any `enum` is passed through as-is.

#### Collections

Anything inheriting from `IEnumerable<>` (except `string`) will be returned as-is, with no further processing.

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

