# Generated windows model binding

Binding deep objects (or non-primitive types such as `NodaTime`) types in WPF/WinForms can be tricky.
This project provides a source generator that generates a flat model for your deep objects.
This allows you to bind to your deep objects in WPF/WinForms without having to write boilerplate code
and without worrying about nullability in property chains.

## Usage

```csharp
[GenerateWindowsBindingModelAttribute(typeof(Person))]
public sealed partial class PersonModelBinder { }

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
```

The generated source will look like this. Every type is fully qualified so the generated code cannot be
broken by a name in your own project, the `[Description]` text is the doc comments of each property in the
chain joined by `: `, and `[GeneratedCode]` carries the generator's full package version, prerelease suffix
included, so a generated file always says which build wrote it:

```csharp
namespace Demo
{
    [global::System.CodeDom.Compiler.GeneratedCode("WinDataBinding.SourceGenerator", "1.0.0-beta01")]
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

        /// <summary><c>Address</c></summary>
        /// <remarks><see cref="Demo.Person.Address"/></remarks>
        [global::System.ComponentModel.Description("Person's address")]
        public global::Demo.Address? Address => _source.Address;

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

        /// <summary><c>CreatedAt.ToString(null, null)</c></summary>
        /// <remarks><see cref="Demo.Person.CreatedAt"/></remarks>
        [global::System.ComponentModel.Description("The timestamp that the person was created at (Formatted)")]
        public string? CreatedAt_Formatted => _source.CreatedAt.ToString(null, null);

        /// <summary><c>LastLogin</c></summary>
        /// <remarks><see cref="Demo.Person.LastLogin"/></remarks>
        [global::System.ComponentModel.Description("Last login in the user's local timezone")]
        public global::Demo.LoginInfo? LastLogin => _source.LastLogin;

        /// <summary><c>LastLogin?.Id</c></summary>
        /// <remarks><see cref="Demo.Person.LastLogin"/> <see cref="Demo.LoginInfo.Id"/></remarks>
        [global::System.ComponentModel.Description("Last login in the user's local timezone")]
        public int? LastLogin_Id => _source.LastLogin?.Id;

        /// <summary><c>LastLogin?.Timestamp.ToDateTimeOffset()</c></summary>
        /// <remarks><see cref="Demo.Person.LastLogin"/> <see cref="Demo.LoginInfo.Timestamp"/></remarks>
        [global::System.ComponentModel.Description("Last login in the user's local timezone: Timestamp the login occurred at")]
        public global::System.DateTimeOffset? LastLogin_Timestamp => _source.LastLogin?.Timestamp.ToDateTimeOffset();

        /// <summary><c>LastLogin?.Timestamp.ToDateTimeUtc()</c></summary>
        /// <remarks><see cref="Demo.Person.LastLogin"/> <see cref="Demo.LoginInfo.Timestamp"/></remarks>
        [global::System.ComponentModel.Description("Last login in the user's local timezone: Timestamp the login occurred at (Utc)")]
        public global::System.DateTime? LastLogin_Timestamp_Utc => _source.LastLogin?.Timestamp.ToDateTimeUtc();

        /// <summary><c>LastLogin?.Timestamp.ToDateTimeUnspecified()</c></summary>
        /// <remarks><see cref="Demo.Person.LastLogin"/> <see cref="Demo.LoginInfo.Timestamp"/></remarks>
        [global::System.ComponentModel.Description("Last login in the user's local timezone: Timestamp the login occurred at (Local)")]
        public global::System.DateTime? LastLogin_Timestamp_Local => _source.LastLogin?.Timestamp.ToDateTimeUnspecified();

        /// <summary><c>LastLogin?.Timestamp.Offset.ToTimeSpan()</c></summary>
        /// <remarks><see cref="Demo.Person.LastLogin"/> <see cref="Demo.LoginInfo.Timestamp"/></remarks>
        [global::System.ComponentModel.Description("Last login in the user's local timezone: Timestamp the login occurred at (Offset)")]
        public global::System.TimeSpan? LastLogin_Timestamp_Offset => _source.LastLogin?.Timestamp.Offset.ToTimeSpan();

        /// <summary><c>LastLogin?.Timestamp.Zone.Id</c></summary>
        /// <remarks><see cref="Demo.Person.LastLogin"/> <see cref="Demo.LoginInfo.Timestamp"/></remarks>
        [global::System.ComponentModel.Description("Last login in the user's local timezone: Timestamp the login occurred at (Timezone)")]
        public string? LastLogin_Timestamp_Timezone => _source.LastLogin?.Timestamp.Zone.Id;

        /// <summary><c>LastLogin?.Timestamp.ToString(null, null)</c></summary>
        /// <remarks><see cref="Demo.Person.LastLogin"/> <see cref="Demo.LoginInfo.Timestamp"/></remarks>
        [global::System.ComponentModel.Description("Last login in the user's local timezone: Timestamp the login occurred at (Formatted)")]
        public string? LastLogin_Timestamp_Formatted => _source.LastLogin?.Timestamp.ToString(null, null);
    }
}
```

## Installation

```xml
<Project>
  <ItemGroup>
    <!-- Add the package -->
    <PackageReference Include="WinDataBinding" Version="1.0.0-*" />
    <!-- -->
  </ItemGroup>
</Project>
```

The package has no dependencies of its own. Only the attributes assembly reaches your output — the generator
ships as an analyzer and is never referenced — and that assembly is a **runtime** dependency, not merely a
compile-time one: reading an attribute back reflectively loads the assembly that declares it.

So resist the usual analyzer idioms. `PrivateAssets="all"` and `ExcludeAssets="runtime"` both stop the
attributes assembly reaching the application, and neither shows up at build time — the code compiles, and
then throws `FileNotFoundException` the first time anything calls `GetCustomAttributes`. `PrivateAssets="all"`
is the subtler of the two: the project declaring the binders works fine, and only an application referencing
*it* breaks, because the attributes never travel that last hop.

To keep the generator itself from running in projects downstream of yours, make just that part private:

```xml
<PackageReference Include="WinDataBinding" Version="1.0.0-*" PrivateAssets="analyzers" />
```

## Descriptions

`[Description]` is built from the `<summary>` of every property in the chain, joined by `: `. Markup inside a
summary is reduced to its displayed form — the same text a documentation viewer would show:

- only the XML inner text is kept, and attributes are ignored;
- entities are decoded, so `&gt;` becomes `>` and `&amp;` becomes `&`;
- whitespace is collapsed, so a summary spread over several lines becomes one;
- `<inheritdoc/>` is followed to whatever the member inherits from — an override's base member, the interface
  member it implements, or an explicit `cref` — repeatedly, until a real summary turns up.

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

## The binder

The attribute goes on a `partial class` or a `partial struct`, and the generated part carries a factory
alongside the constructor:

```csharp
[return: NotNullIfNotNull("source")]              // and [ContractAnnotation], where either is available
public static PersonModelBinder? Create(Person? source) =>
    source is not null ? new PersonModelBinder(source) : null;
```

A struct cannot express "no source, no binder" through a constructor at all, and for a class the factory
saves the caller a null check either way. The annotations are only emitted when the consuming compilation can
actually resolve them, so neither becomes a dependency. A struct binder is declared `readonly`, so reading a
member never takes a defensive copy.

The binder also mirrors how the source compares:

| Interface              | Always?                            | Delegates to |
| :--------------------- | :--------------------------------- | :- |
| `IEquatable<TBinder>`  | Yes                                | `EqualityComparer<TSource>.Default.Equals(left, right)` |
| `IComparable<TBinder>` | Only when the source orders itself | `Comparer<TSource>.Default.Compare` |

`Equals(object)` and `GetHashCode` are overridden alongside `IEquatable<TBinder>`. Without them a struct
binder would fall back to `ValueType.Equals`, which compares a struct holding a reference field by
reflection, and the two notions of equality could disagree.

## Properties you declare yourself

Anything the binder's own half of the `partial` declares is flattened too, exactly as a property of the
source would be, rooted at `this` instead of `_source`:

```csharp
[GenerateWindowsBindingModel(typeof(Person))]
public sealed partial class PersonModelBinder
{
    /// <summary>The vehicle being driven</summary>
    public Vehicle? Current { get; set; }

    public OrderId? Order { get; set; }

    public Duration? Elapsed { get; set; }

    public bool IsSelected { get; set; }
}
```

```csharp
/// <summary><c>Current?.Speed</c></summary>
/// <remarks><see cref="Demo.PersonModelBinder.Current"/> <see cref="Demo.Vehicle.Speed"/></remarks>
[global::System.ComponentModel.Description("The vehicle being driven")]
public int? Current_Speed => this.Current?.Speed;

public global::System.Guid? Order_Value => this.Order?.Value;

public string? Order_Value_Formatted => this.Order?.Value.ToString(null, null);

public global::System.TimeSpan? Elapsed_Value => this.Elapsed?.ToTimeSpan();
```

Two rules differ from the source object:

- **The property's own name is never re-emitted.** It is already declared, so a second member under the same
  name would not compile. `Current` stays exactly as you wrote it, and only what comes out of it is
  generated.
- **A simple property is ignored entirely.** `bool`, `bool?`, `string`, `int`, an enum, a `DateTime` — there
  is nothing to flatten, and the property already binds as it stands.

A value that would otherwise have taken the bare name gets a `_Value` segment instead, so nothing is lost: a
`Duration` becomes `Elapsed_Value` (a `TimeSpan`), and a
[mapped](#mapping-a-wrapper-onto-the-type-it-wraps) wrapper becomes `Declared_Value` whatever shape it maps
to. A conversion that names its own properties is unaffected — `TimeZoneInfo` still yields `_Id` and
`_DisplayName`.

The distinction is whether the value is *different* from the property. A conversion or a mapping produces
something the declared property does not already give you, so it is kept under `_Value`; a plain object graph
or sequence produces the property itself, so it is dropped and only what comes out of it is generated.

Names you declare by hand always win. A generated property that would collide with one widens its separator
just as it would against another generated property, so a source `Rank` alongside a declared `Rank` becomes
`Rank_`.

## Nested binders

A property whose type is itself marked `[GenerateWindowsBindingModel]` is flattened through *what that binder
generates*, not through the object graph behind it:

```csharp
public class Address
{
    public string Street { get; set; }
    public Instant Created { get; set; }
}

[GenerateWindowsBindingModel(typeof(Address))]
public sealed partial class AddressBinder { }

public class Person { public AddressBinder Home { get; set; } }

[GenerateWindowsBindingModel(typeof(Person))]
public sealed partial class PersonBinder { }
```

```csharp
public global::Demo.AddressBinder? Home           => _source.Home;
public string?                     Home_Street    => _source.Home?.Street;
public global::System.DateTime?    Home_Created   => _source.Home?.Created;
```

Note `Home_Created`: `AddressBinder.Created` is already a `DateTime`, so it is simply read. Re-deriving it
from the graph would have produced `_source.Home?.Created.ToDateTimeUtc()`, which does not compile — the
`Instant` is behind the nested binder, not in front of it.

Those flattened members are not in the compilation the generator reads: a generator never sees its own
output, its own earlier files included. They are known anyway, because the same logic that will write them
runs here to work out their names and types. The nesting goes as deep as you take it, and two binders that
point at each other terminate with `WGD001` rather than looping.

Whatever the nested binder declares by hand is an ordinary member of it and binds by the usual rules, so
`AddressBinder`'s own `bool IsPrimary` becomes `Home_IsPrimary`. A binder from a *referenced* assembly is
already compiled, generated half and all, so it is walked as any other object would be.

## Types

A property is either *simple* — bound directly, with the object never traversed — or an *object graph*, which
is flattened. Value types, enums, collections, [mapped](#mapping-a-wrapper-onto-the-type-it-wraps)
wrappers, `JsonNode` and `JsonElement` are all simple.

The `<remarks>` cref names the type each member is declared on. A cref lives in an XML attribute, where an
angle bracket is illegal, so a generic is written with braces around its type *parameters* —
`<see cref="Demo.Base{T}.Current"/>`. The parameters rather than the arguments: a cref's type arguments have
to be simple names, so the constructed `Base{Demo.Reading}` is rejected as malformed (CS1584) while `Base{T}`
always binds.

A source type may be generic as long as its type arguments are supplied — `typeof(Base<Reading>)` on the
attribute, or a `sealed class Inherited : Base<Reading>` whose base carries the members. Substitution has
already happened by the time the traversal sees them, so a `T` declared on the base arrives as `Reading` and
every rule that turns on a type lands on the substituted one: `IReadOnlyList<T>` counts and renders as
`IReadOnlyList<Reading>`, a `T` of `Instant` converts, a `T` of `int` binds as a leaf. Only `typeof(Base<>)`
is rejected, with `WGD003`: nothing has been substituted, so there is nothing to flatten. The binding model
type itself may never be generic.

### Simple types:

#### Common

| Source type                   | Output property |
| :---------------------------- | :- |
| `TimeZoneInfo`                | 2 properties: `string _Id` (`Id`) and `string _DisplayName` (`DisplayName`) |
| `Exception`                   | 4 properties: the exception itself, `string _Message`, `string _StackTrace`, and `string _Display` (`ToString()`) |
| `ArgumentException`           | adds `string _ParamName` |
| `ArgumentOutOfRangeException` | adds `object _ActualValue` |
| `HttpRequestException`        | adds `HttpStatusCode _StatusCode`, on NET5 and later |
| `IPAddress`                   | 4 properties: the address itself, `string _Formatted` (`ToString()`), `AddressFamily _AddressFamily`, `string _AddressFamily_Formatted` (`AddressFamily.ToString()`) |
| `IPNetwork`                   | NET8+. The network itself, `int _PrefixLength`, `string _Formatted`, and `_BaseAddress` flattened through the `IPAddress` row above — `_BaseAddress`, `_BaseAddress_Formatted`, `_BaseAddress_AddressFamily`, `_BaseAddress_AddressFamily_Formatted` |

`IPAddress`'s rendered form is spelled out as `ToString()` rather than left to the
[formattable rule](#formattable-types): it implements `IFormattable` explicitly, so that rule would reach it
through a cast, and before NET6 it does not implement the interface at all — where the cast would throw.
`IPNetwork` has no such problem, so its `_Formatted` is left to that rule, which casts.

A row applies to the type it names **and to everything deriving from it**, and a row for a derived type
*adds* to the rows of its bases rather than replacing them. So an `ArgumentNullException` property collects
all four `Exception` properties plus `_ParamName`, and an `HttpRequestException` gets the four plus
`_StatusCode`. A property declared as a concrete `DateTimeZone` is likewise described by the row written for
the abstract base.

An exception is worth a row because walking it reaches `TargetSite`, a whole reflection graph, and `Data`, a
dictionary. `_Display` is the useful whole of it — type, message, stack trace and every inner exception —
with `_Message` for a column narrow enough to read.

A row's property is dropped when the framework the consuming code targets does not have the member behind
it, which is how `HttpRequestException._StatusCode` appears on NET5 and later and simply is not there before.
Nothing is guessed from the target framework: the member is looked for in the compilation.

A row may land on a type that has a row of its own, as `IPNetwork._BaseAddress` lands on `IPAddress`. That
row then takes over, its properties hanging off the segment that reached it — so `_BaseAddress` is the
address itself, exactly as the bare name of the `IPAddress` row is.

#### `NodaTime`:

| Source Type      | Output property |
| :--------------- | :- |
| `DateTimeZone`   | `string`: `Id` |
| `Instant`        | `DateTime`: `ToDateTimeUtc()` |
| `OffsetDateTime` | 4 properties: `DateTimeOffset` (`ToDateTimeOffset()`), `DateTime _Utc` (`ToInstant().ToDateTimeUtc()`), `DateTime _Local` (`LocalDateTime.ToDateTimeUnspecified()`), `TimeSpan _Offset` (`Offset.ToTimeSpan()`) |
| `ZonedDateTime`  | 5 properties: `DateTimeOffset` (`ToDateTimeOffset()`), `DateTime _Utc` (`ToDateTimeUtc()`), `DateTime _Local` (`ToDateTimeUnspecified()`), `TimeSpan _Offset` (`Offset.ToTimeSpan()`), `string _Timezone` (`Zone.Id`) |
| `LocalDateTime`  | `DateTime`: `ToDateTimeUnspecified()` |
| `LocalDate`      | On NET6+: `DateOnly`: `ToDateOnly()`. Earlier: `DateTime`: `ToDateTimeUnspecified()` |
| `LocalTime`      | On NET6+: `TimeOnly`: `.ToTimeOnly()`. Earlier: `TimeSpan.FromTicks(x.TickOfDay)` |
| `Duration`       | `TimeSpan`: `.ToTimeSpan()` |
| `Offset`         | `TimeSpan`: `.ToTimeSpan()` |
| `YearMonth`      | On NET6+: `DateOnly`: `OnDayOfMonth(1).ToDateOnly()`. Earlier: `DateTime`: `OnDayOfMonth(1).ToDateTimeUnspecified()` |
| `Interval`       | 3 properties: `DateTime? _Start` (`HasStart ? Start.ToDateTimeUtc() : null`), `DateTime? _End` (`HasEnd ? End.ToDateTimeUtc() : null`), and `TimeSpan? _Duration` (`HasStart && HasEnd ? Duration.ToTimeSpan() : null`) |
| `Period`         | `string`: `.ToString()` |

#### Object graphs

Everything else is traversed, and **every** object along the way — the root one included — binds as a bare
property of its own, emitted just before the members flattened out of it:

```csharp
public class Model { public Address Address { get; set; } }
public class Address { public Current Current { get; set; } }
public class Current { public string City { get; set; } }
```

gives three properties, not one:

```csharp
public global::Demo.Address? Address => _source.Address;                        // the root object
public global::Demo.Current? Address_Current => _source.Address?.Current;       // and each one below it
public string? Address_Current_City => _source.Address?.Current?.City;          // down to the leaf
```

So a grid can bind a whole object to a templated column, or its flattened members to plain ones, without you
having to choose up front. A property skipped for a circular reference still gets its bare property: the
object binds even where the graph cannot be flattened.

#### Value types

`string`, `bool`, `char`, `System.Half`, `float`, `double`, `decimal`, 
`byte`, `sbyte`, `short`, `int`, `long`, `ushort`, `uint`, `ulong`, `Int128`, `UInt128`,
`Uri`, `Guid`, `Version`, `Type`, `CultureInfo`, `BigInteger`, `Rune`, `Index`,
`DateTime`, `DateTimeOffset`, `TimeSpan`, `DateOnly`, `TimeOnly`

Any `enum` is passed through as-is.

A member is skipped entirely when its type is a **ref struct**: it cannot be boxed, and a nullable chain
would ask for `ReadOnlySpan<char>?`, which the language forbids. `ReadOnlyMemory<T>.Span` is the one that
turns up in practice.

A name is bound once however many times it is declared. An `override` or a `new` member hides what it
replaces, and the most derived declaration is the one reachable by name, so the base declaration the walk
meets on its way up the chain is passed over.

#### Collections

Anything inheriting from `IEnumerable<>` (except `string`) will be returned as is.

Anything inheriting from `IEnumerable<T>` where `T` is renderable (see below) or is `string` will have 2
additional properties (both `string?`):
- `Property_Display` - `string.Join(", ", Property.Select(x => /* render x */))`
- `Property_Array`- `Property_Display is { } display ? $"[{display}]" : null`

Strings are already their own display text, so a sequence of them is joined as it stands rather than
projected first: `string.Join(", ", Property)`. Enum elements are named with the plain `ToString()`:
`Enum.ToString(string, IFormatProvider)` is obsolete — it ignores the provider — and gives identical text.

An element that can be null is rendered through `?.`, so `IEnumerable<T?>` and a sequence of a reference
type both work: `Nullable<T>` implements nothing itself, so the renderer is chosen from the `T` it wraps, and
the null that comes back joins as an empty entry rather than throwing.

Anything inheriting from `IReadOnlyCollection<T>` also gets `Property_Count`, which is `int` when the
collection cannot be null and `int?` when it can — a struct collection on a non-nullable chain gives a plain
`int`. `IReadOnlyDictionary<TKey, TValue>` is covered by the same rule: it derives from
`IReadOnlyCollection<T>` of its pairs. A bare `IEnumerable<T>` gets no count, having no length to report
without being walked.

The count is read the way the type itself spells it, and the generated code never resorts to LINQ:

| Collection                                     | Count expression |
| :--------------------------------------------- | :- |
| `List<T>`, `IReadOnlyDictionary<TKey, TValue>` | `Property?.Count` |
| `T[]`, `ImmutableArray<T>`                     | `Property?.Length` |
| implements `Count` explicitly                  | `((IReadOnlyCollection<T>)Property)?.Count` |

An array satisfies `IReadOnlyCollection<T>` through the runtime rather than through its own members, and
`ImmutableArray<T>` implements `Count` explicitly while offering `Length` — so both are read as `Length`,
which also avoids boxing the struct. A type that offers neither name is read back through the interface.

#### Formattable types

A non-enumerable property whose type is *renderable* — but is not one of the value types above, and not an
`enum`, both of which a grid already renders — gets one extra property alongside whatever else it produces,
emitted last:

- `Property_Formatted` (always `string?`)

A type is renderable if it is any of these:

| Type                                                        | Rendered with |
| :---------------------------------------------------------- | :- |
| implements `IFormattable`                                   | `Property.ToString(null, null)`, or `((IFormattable)Property)?.ToString(null, null)` when a cast is needed |
| `System.Text.Json.Nodes.JsonNode` (including derived types) | `Property?.ToJsonString()` |
| `System.Text.Json.JsonElement`                              | `Property.GetRawText()` |

`ToString(null, null)` is called directly whenever the type offers it publicly — `int`, `Guid`, every
`NodaTime` value. The cast is the fallback, for a type that implements `IFormattable` explicitly, and for one
carrying a second two-parameter `ToString` that would make the call ambiguous. Calling directly avoids boxing
a struct on every read.

This applies to a bare property and to one reached through the object graph alike:

```csharp
public struct Temperature : IFormattable { public double Degrees { get; } /* ... */ }

public class Inner { public Temperature Reading { get; } }

public class Model
{
    public Temperature Outside { get; set; }
    public Inner Inner { get; set; }
}
```

gives

```csharp
public double Outside_Degrees => _source.Outside.Degrees;
public string? Outside_Formatted => _source.Outside.ToString(null, null);

public double? Inner_Reading_Degrees => _source.Inner?.Reading.Degrees;
public string? Inner_Reading_Formatted => _source.Inner?.Reading.ToString(null, null);
```

## Generation options

The second argument to `[GenerateWindowsBindingModel]` names a type carrying configuration attributes. It
need not derive from anything and is never instantiated — only its attributes are read. Attributes on its
base types are read too, with the most derived declaration for a type winning, and attributes the generator
does not understand are ignored.

| Attribute          | Purpose |
| :----------------- | :- |
| `MapTypeAttribute` | Stands a wrapper type in for the type it wraps: the wrapper, the type it wraps, and the expression reaching the wrapped value. Repeatable. |

### Mapping a wrapper onto the type it wraps

`[MapType]` says "wherever you find this type, treat it as that one, reached like this":

```csharp
[MapType(typeof(OrderId), typeof(Guid), "Value")]
[MapType(typeof(Tags), typeof(List<int>), "Values")]
[MapType(typeof(Boxed), typeof(Address), "Unwrap()")]
public class BindingOptions;
```

```csharp
public Guid       Order        => _source.Order.Value;            // OrderId Order
public List<int>? Labels       => _source.Labels?.Values;         // Tags    Labels
public string?    Labels_Array => ...
public Address?   Home         => _source.Home?.Unwrap();         // Boxed   Home
public string?    Home_Street  => _source.Home?.Unwrap()?.Street;
```

The expression is written out **exactly as given** — nothing parses, resolves or checks it, so a property, a
field and a method call are all equally acceptable, and getting it wrong is a compile error in the generated
file rather than a diagnostic. It is placed after the chain's own accessor, so `?.` is still used wherever a
link can be null.

The substitution is total and transparent. The wrapper is not traversed, the property keeps the name the
wrapper would have had, and the *target* is then classified by the ordinary rules — every transformation its
type earns, it gets:

| Target               | Emitted |
| :------------------- | :- |
| `List<int>`          | the list, `_Count`, `_Display`, `_Array` |
| a formattable class  | the value, `_Formatted` |
| a formattable struct | the value, its flattened members, `_Formatted` |
| `JsonNode`           | `_Formatted` |
| `Instant`            | the converted `DateTime`, `_Formatted` |
| an enum, or a leaf   | the value alone — a grid renders those already |

The lookup runs before every other rule, so a mapping can override a type the generator already understands
as easily as it can describe one it does not.

On a property the binder [declares itself](#properties-you-declare-yourself), where the bare name is already
taken, the mapped value takes a `_Value` segment just as a conversion does.

Naming an **interface or a base type** maps everything that derives from it, which is what makes one line
enough for a whole family:

```csharp
[MapType(typeof(IStringId), typeof(string), "Value")]
public class BindingOptions;
```

An exact mapping for a type always beats an inherited one, wherever the two are declared.

That form is what makes a generated wrapper such as a
[strongly typed ID](https://github.com/andrewlock/StronglyTypedId) workable. Those IDs are declared by an
attribute the compiler never writes into the assembly — `StronglyTypedIds`' own is marked `[Conditional]` —
so a generator in a project that merely *references* them has nothing to read: no attribute, and a `Value`
property written by a generator it cannot see. The marker interface a template adds does survive into
metadata, so mapping that one interface reaches every ID at once, in every assembly.

Two things `[MapType]` deliberately does not do. It renders the *mapped value*, never the wrapper, so a
wrapper that is itself `IFormattable` gets no twin of its own — what comes out is whatever the target earns.
And it matches a named type exactly, so a closed generic must be named as one.

