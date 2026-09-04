namespace WinDataBinding.SourceGenerator.Tests;

/// <summary>
/// A property whose type is itself a binder is flattened through what that binder generates, not through the
/// graph behind it. Those members do not exist in the compilation the generator reads — a generator never sees
/// its own output — so their names and types are worked out by running the same logic over the nested binder.
/// </summary>
public class NestedBinderTests
{
    private static string Wrap(string body) => $$"""
        using System.Collections.Generic;
        using NodaTime;
        using WinDataBinding;

        namespace Demo;

        {{body}}
        """;

    [Fact]
    public void Flattens_a_source_property_whose_type_is_a_binder()
    {
        var source = Wrap("""
            public class Address
            {
                public string Street { get; set; }
                public int Number { get; set; }
            }

            [GenerateWindowsBindingModel(typeof(Address))]
            public sealed partial class AddressBinder { }

            public class Person { public AddressBinder Home { get; set; } }

            [GenerateWindowsBindingModel(typeof(Person))]
            public sealed partial class PersonBinder { }
            """);

        var result = TestHarness.AssertCompiles(source);

        // Read off the nested binder's own flat properties, never rebuilt as _source.Home?.Street.
        result.Source.Should().Contain("public string? Home_Street => _source.Home?.Street;");

        // A value type lifts, because the binder holding it can be null.
        result.Source.Should().Contain("public int? Home_Number => _source.Home?.Number;");
    }

    [Fact]
    public void Reads_a_converted_value_off_the_nested_binder_instead_of_converting_again()
    {
        var source = Wrap("""
            public class LoginInfo { public Instant Timestamp { get; set; } }

            [GenerateWindowsBindingModel(typeof(LoginInfo))]
            public sealed partial class LoginBinder { }

            public class Person { public LoginBinder Login { get; set; } }

            [GenerateWindowsBindingModel(typeof(Person))]
            public sealed partial class PersonBinder { }
            """);

        var result = TestHarness.AssertCompiles(source);

        // LoginBinder.Timestamp is already a DateTime; calling ToDateTimeUtc() on it would not compile.
        result.Source.Should().Contain(
            "public global::System.DateTime? Login_Timestamp => _source.Login?.Timestamp;");
        result.Source.Should().Contain(
            "public string? Login_Timestamp_Formatted => _source.Login?.Timestamp_Formatted;");
        result.Source.Should().NotContain("Login?.Timestamp.ToDateTimeUtc()");
    }

    [Fact]
    public void Carries_a_per_framework_type_across_from_the_nested_binder()
    {
        var source = Wrap("""
            public class Event { public LocalDate Day { get; set; } }

            [GenerateWindowsBindingModel(typeof(Event))]
            public sealed partial class EventBinder { }

            public class Diary { public EventBinder Next { get; set; } }

            [GenerateWindowsBindingModel(typeof(Diary))]
            public sealed partial class DiaryBinder { }
            """);

        // The property is only read here, but its type still differs by framework, so both are emitted.
        TestHarness.AssertCompiles(source);
        var pre6 = TestHarness.AssertCompiles(source, Target.NetStandard20);

        pre6.Source.Should().Contain("public global::System.DateOnly? Next_Day => _source.Next?.Day;");
        pre6.Source.Should().Contain("public global::System.DateTime? Next_Day => _source.Next?.Day;");
    }

    [Fact]
    public void Reaches_through_a_struct_binder_without_lifting()
    {
        var source = Wrap("""
            public class Address { public int Number { get; set; } }

            [GenerateWindowsBindingModel(typeof(Address))]
            public readonly partial struct AddressBinder { }

            public class Person { public AddressBinder Home { get; set; } }

            [GenerateWindowsBindingModel(typeof(Person))]
            public sealed partial class PersonBinder { }
            """);

        var result = TestHarness.AssertCompiles(source);

        // The nested binder is a struct and cannot be null, so nothing lifts on the way in.
        result.Source.Should().Contain("public int Home_Number => _source.Home.Number;");
    }

    [Fact]
    public void Binds_what_the_nested_binder_declares_by_hand_exactly_once()
    {
        var source = Wrap("""
            public class Engine { public int Rpm { get; set; } }

            public class Address { public string Street { get; set; } }

            [GenerateWindowsBindingModel(typeof(Address))]
            public sealed partial class AddressBinder
            {
                public bool IsPrimary { get; set; }
                public Engine? Backup { get; set; }
            }

            public class Person { public AddressBinder Home { get; set; } }

            [GenerateWindowsBindingModel(typeof(Person))]
            public sealed partial class PersonBinder { }
            """);

        var result = TestHarness.AssertCompiles(source);

        // A declared member is a real member of the nested binder, so it binds by being walked.
        result.Source.Should().Contain("public bool? Home_IsPrimary => _source.Home?.IsPrimary;");
        result.Source.Should().Contain("public global::Demo.Engine? Home_Backup => _source.Home?.Backup;");

        // Its flattening comes from the same walk, not from the splice: no widened duplicate.
        result.Source.Should().Contain("public int? Home_Backup_Rpm => _source.Home?.Backup?.Rpm;");
        result.Source.Should().NotContain("Home_Backup__Rpm");
    }

    [Fact]
    public void Flattens_a_binder_the_binder_declares_itself()
    {
        var source = Wrap("""
            public class Address { public string Street { get; set; } }

            [GenerateWindowsBindingModel(typeof(Address))]
            public sealed partial class AddressBinder { }

            public class Person { public int Id { get; set; } }

            [GenerateWindowsBindingModel(typeof(Person))]
            public sealed partial class PersonBinder
            {
                public AddressBinder? Shipping { get; set; }
            }
            """);

        var result = TestHarness.AssertCompiles(source);

        result.Source.Should().Contain("public string? Shipping_Street => this.Shipping?.Street;");

        // The declared property is already there; only what comes out of it is generated.
        result.Source.Should().NotContain("Shipping => ");
    }

    [Fact]
    public void Joins_the_descriptions_of_both_binders()
    {
        var source = Wrap("""
            public class Address
            {
                /// <summary>Street name</summary>
                public string Street { get; set; }
            }

            [GenerateWindowsBindingModel(typeof(Address))]
            public sealed partial class AddressBinder { }

            public class Person
            {
                /// <summary>Where they live</summary>
                public AddressBinder Home { get; set; }
            }

            [GenerateWindowsBindingModel(typeof(Person))]
            public sealed partial class PersonBinder { }
            """);

        var result = TestHarness.AssertCompiles(source);

        result.Source.Should().Contain(
            """[global::System.ComponentModel.Description("Where they live: Street name")]""");
        /* XML doc comments removed.
        result.Source.Should().Contain("""<see cref="Demo.Person.Home"/> <see cref="Demo.Address.Street"/>""");
        */
    }

    [Fact]
    public void Flattens_binders_nested_several_deep()
    {
        var source = Wrap("""
            public class Country { public string Name { get; set; } }

            [GenerateWindowsBindingModel(typeof(Country))]
            public sealed partial class CountryBinder { }

            public class Address { public CountryBinder Country { get; set; } }

            [GenerateWindowsBindingModel(typeof(Address))]
            public sealed partial class AddressBinder { }

            public class Person { public AddressBinder Home { get; set; } }

            [GenerateWindowsBindingModel(typeof(Person))]
            public sealed partial class PersonBinder { }
            """);

        var result = TestHarness.AssertCompiles(source);

        // AddressBinder flattens to Country_Name, so PersonBinder reads that one property.
        result.Source.Should().Contain("public string? Home_Country_Name => _source.Home?.Country_Name;");
    }

    [Fact]
    public void Stops_when_two_binders_point_at_each_other()
    {
        var source = Wrap("""
            public class Address
            {
                public string Street { get; set; }
                public PersonBinder Owner { get; set; }
            }

            [GenerateWindowsBindingModel(typeof(Address))]
            public sealed partial class AddressBinder { }

            public class Person
            {
                public string Name { get; set; }
                public AddressBinder Home { get; set; }
            }

            [GenerateWindowsBindingModel(typeof(Person))]
            public sealed partial class PersonBinder { }
            """);

        var result = TestHarness.AssertCompiles(source);

        result.Source.Should().Contain("public string? Home_Street => _source.Home?.Street;");
        result.Source.Should().Contain("public string? Home_Owner_Name => _source.Home?.Owner_Name;");
    }

    [Fact]
    public void Leaves_a_referenced_binder_to_bind_as_an_ordinary_object()
    {
        // WinDataBinding's own attribute type is compiled, not generated here, and carries no attribute of ours.
        var source = Wrap("""
            public class Person { public System.Text.StringBuilder Notes { get; set; } }

            [GenerateWindowsBindingModel(typeof(Person))]
            public sealed partial class PersonBinder { }
            """);

        var result = TestHarness.AssertCompiles(source);

        result.Source.Should().Contain("public int? Notes_Length => _source.Notes?.Length;");
    }
}
