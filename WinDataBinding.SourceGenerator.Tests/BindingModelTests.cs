namespace WinDataBinding.SourceGenerator.Tests;

public class BindingModelTests
{
    [Fact]
    public void Generates_the_readme_example()
    {
        const string source = """
            using NodaTime;
            using WinDataBinding;

            namespace Demo;

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

            [GenerateWindowsBindingModel(typeof(Person))]
            public sealed partial class PersonModelBinder { }
            """;

        const string expected = """
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

                    /// <summary><c>((global::System.IFormattable)CreatedAt)?.ToString(null, null)</c></summary>
                    /// <remarks><see cref="Demo.Person.CreatedAt"/></remarks>
                    [global::System.ComponentModel.Description("The timestamp that the person was created at (Formatted)")]
                    public string? CreatedAt_Formatted => ((global::System.IFormattable)_source.CreatedAt)?.ToString(null, null);

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
                    [global::System.ComponentModel.Description("Last login in the user's local timezone: Timestamp the login occurred at (Value)")]
                    public global::System.DateTimeOffset? LastLogin_Timestamp_Value => _source.LastLogin?.Timestamp.ToDateTimeOffset();

                    /// <summary><c>LastLogin?.Timestamp.Zone.Id</c></summary>
                    /// <remarks><see cref="Demo.Person.LastLogin"/> <see cref="Demo.LoginInfo.Timestamp"/></remarks>
                    [global::System.ComponentModel.Description("Last login in the user's local timezone: Timestamp the login occurred at (Timezone)")]
                    public string? LastLogin_Timestamp_Timezone => _source.LastLogin?.Timestamp.Zone.Id;

                    /// <summary><c>((global::System.IFormattable)LastLogin?.Timestamp)?.ToString(null, null)</c></summary>
                    /// <remarks><see cref="Demo.Person.LastLogin"/> <see cref="Demo.LoginInfo.Timestamp"/></remarks>
                    [global::System.ComponentModel.Description("Last login in the user's local timezone: Timestamp the login occurred at (Formatted)")]
                    public string? LastLogin_Timestamp_Formatted => ((global::System.IFormattable)_source.LastLogin?.Timestamp)?.ToString(null, null);
                }
            }
            """;

        TestHarness.AssertGenerated(expected, source);
    }

    [Fact]
    public void Passes_through_leaf_types_collections_enums_and_fields()
    {
        var source = TestSources.Wrap("""
            public enum Colour { Red, Green }

            public class Model
            {
                public System.Collections.Generic.List<int> Numbers { get; set; }
                public string[] Names { get; set; }
                public Colour Colour { get; set; }
                public Colour? MaybeColour { get; set; }
                public int Field;
                public int? Nullable { get; set; }
                public string Text { get; set; }
            }
            """);

        const string expected = """
            namespace Demo
            {
                [global::System.CodeDom.Compiler.GeneratedCode("WinDataBinding.SourceGenerator", "1.0.0.0")]
                partial class ModelBinder
                {
                    private readonly global::Demo.Model _source;

                    public ModelBinder(global::Demo.Model source)
                    {
            #if NET6_0_OR_GREATER
                        global::System.ArgumentNullException.ThrowIfNull(source);
                        _source = source;
            #else
                        _source = source ?? throw new global::System.ArgumentNullException(nameof(source));
            #endif
                    }

                    /// <summary><c>Numbers</c></summary>
                    /// <remarks><see cref="Demo.Model.Numbers"/></remarks>
                    public global::System.Collections.Generic.List<int>? Numbers => _source.Numbers;

                    /// <summary><c>Numbers is { } items ? global::System.String.Join(", ", global::System.Linq.Enumerable.Select(items, item =&gt; ((global::System.IFormattable)item).ToString(null, null))) : null</c></summary>
                    /// <remarks><see cref="Demo.Model.Numbers"/></remarks>
                    public string? Numbers_Display => _source.Numbers is { } items ? global::System.String.Join(", ", global::System.Linq.Enumerable.Select(items, item => ((global::System.IFormattable)item).ToString(null, null))) : null;

                    /// <summary><c>Numbers_Display is { } display ? $"[{display}]" : null</c></summary>
                    /// <remarks><see cref="Demo.Model.Numbers"/></remarks>
                    public string? Numbers_Array => Numbers_Display is { } display ? $"[{display}]" : null;

                    /// <summary><c>Names</c></summary>
                    /// <remarks><see cref="Demo.Model.Names"/></remarks>
                    public string[]? Names => _source.Names;

                    /// <summary><c>Colour</c></summary>
                    /// <remarks><see cref="Demo.Model.Colour"/></remarks>
                    public global::Demo.Colour Colour => _source.Colour;

                    /// <summary><c>MaybeColour</c></summary>
                    /// <remarks><see cref="Demo.Model.MaybeColour"/></remarks>
                    public global::Demo.Colour? MaybeColour => _source.MaybeColour;

                    /// <summary><c>Field</c></summary>
                    /// <remarks><see cref="Demo.Model.Field"/></remarks>
                    public int Field => _source.Field;

                    /// <summary><c>Nullable</c></summary>
                    /// <remarks><see cref="Demo.Model.Nullable"/></remarks>
                    public int? Nullable => _source.Nullable;

                    /// <summary><c>Text</c></summary>
                    /// <remarks><see cref="Demo.Model.Text"/></remarks>
                    public string? Text => _source.Text;
                }
            }
            """;

        TestHarness.AssertGenerated(expected, source);
    }

    [Fact]
    public void Writes_the_file_header()
    {
        var source = TestSources.Wrap("public class Model { public int Value { get; set; } }");

        var result = TestHarness.AssertCompiles(source);

        result.Source.Should().StartWith("""
            // <auto-generated>
            //     This file was generated by WinDataBinding.SourceGenerator.
            //     Changes to this file will be lost if the code is regenerated.
            // </auto-generated>
            // ReSharper disable all
            // CS1574 XML comment has a cref attribute that could not be resolved: https://learn.microsoft.com/dotnet/csharp/misc/cs1574
            // CS1584 XML comment has a syntactically incorrect cref attribute: https://learn.microsoft.com/dotnet/csharp/misc/cs1584
            // CS1581 Invalid return type in XML comment cref attribute: https://learn.microsoft.com/dotnet/csharp/misc/cs1581
            // CS1580 Invalid type for parameter in XML comment cref attribute: https://learn.microsoft.com/dotnet/csharp/misc/cs1580
            // CS1587 XML comment is not placed on a valid language element: https://learn.microsoft.com/dotnet/csharp/misc/cs1587
            #pragma warning disable CS1574, CS1584, CS1581, CS1580, CS1587
            #nullable enable annotations
            #nullable disable warnings
            """.ReplaceLineEndings("\n"));
    }

    [Fact]
    public void Gathers_members_from_every_part_of_a_partial_source_model()
    {
        // Roslyn merges the parts, but the order matters: it is what the collision rule works from.
        var source = TestSources.Wrap("""
            public partial class Model
            {
                /// <summary>From the first part</summary>
                public string First { get; set; }
            }

            public partial class Model
            {
                public int Second { get; set; }

                public Detail Detail { get; set; }
            }

            public partial class Detail { public string Left { get; set; } }

            public partial class Detail { public string Right { get; set; } }
            """);

        const string expected = """
            namespace Demo
            {
                [global::System.CodeDom.Compiler.GeneratedCode("WinDataBinding.SourceGenerator", "1.0.0.0")]
                partial class ModelBinder
                {
                    private readonly global::Demo.Model _source;

                    public ModelBinder(global::Demo.Model source)
                    {
            #if NET6_0_OR_GREATER
                        global::System.ArgumentNullException.ThrowIfNull(source);
                        _source = source;
            #else
                        _source = source ?? throw new global::System.ArgumentNullException(nameof(source));
            #endif
                    }

                    /// <summary><c>First</c></summary>
                    /// <remarks><see cref="Demo.Model.First"/></remarks>
                    [global::System.ComponentModel.Description("From the first part")]
                    public string? First => _source.First;

                    /// <summary><c>Second</c></summary>
                    /// <remarks><see cref="Demo.Model.Second"/></remarks>
                    public int Second => _source.Second;

                    /// <summary><c>Detail</c></summary>
                    /// <remarks><see cref="Demo.Model.Detail"/></remarks>
                    public global::Demo.Detail? Detail => _source.Detail;

                    /// <summary><c>Detail?.Left</c></summary>
                    /// <remarks><see cref="Demo.Model.Detail"/> <see cref="Demo.Detail.Left"/></remarks>
                    public string? Detail_Left => _source.Detail?.Left;

                    /// <summary><c>Detail?.Right</c></summary>
                    /// <remarks><see cref="Demo.Model.Detail"/> <see cref="Demo.Detail.Right"/></remarks>
                    public string? Detail_Right => _source.Detail?.Right;
                }
            }
            """;

        TestHarness.AssertGenerated(expected, source);
    }

    [Fact]
    public void Includes_base_members_and_matches_source_constructor_visibility()
    {
        var source = TestSources.Wrap("""
            public class Base { public int BaseValue { get; set; } }

            internal class Model : Base { public int Own { get; set; } }
            """);

        const string expected = """
            namespace Demo
            {
                [global::System.CodeDom.Compiler.GeneratedCode("WinDataBinding.SourceGenerator", "1.0.0.0")]
                partial class ModelBinder
                {
                    private readonly global::Demo.Model _source;

                    internal ModelBinder(global::Demo.Model source)
                    {
            #if NET6_0_OR_GREATER
                        global::System.ArgumentNullException.ThrowIfNull(source);
                        _source = source;
            #else
                        _source = source ?? throw new global::System.ArgumentNullException(nameof(source));
            #endif
                    }

                    /// <summary><c>Own</c></summary>
                    /// <remarks><see cref="Demo.Model.Own"/></remarks>
                    public int Own => _source.Own;

                    /// <summary><c>BaseValue</c></summary>
                    /// <remarks><see cref="Demo.Base.BaseValue"/></remarks>
                    public int BaseValue => _source.BaseValue;
                }
            }
            """;

        TestHarness.AssertGenerated(expected, source);
    }

    [Fact]
    public void Emits_a_nested_binder_inside_its_parent()
    {
        const string source = """
            using WinDataBinding;

            namespace Demo;

            public class Model { public int Value { get; set; } }

            public partial class Outer
            {
                [GenerateWindowsBindingModel(typeof(Model))]
                public sealed partial class Binder { }
            }
            """;

        const string expected = """
            namespace Demo
            {
                partial class Outer
                {
                    [global::System.CodeDom.Compiler.GeneratedCode("WinDataBinding.SourceGenerator", "1.0.0.0")]
                    partial class Binder
                    {
                        private readonly global::Demo.Model _source;

                        public Binder(global::Demo.Model source)
                        {
            #if NET6_0_OR_GREATER
                            global::System.ArgumentNullException.ThrowIfNull(source);
                            _source = source;
            #else
                            _source = source ?? throw new global::System.ArgumentNullException(nameof(source));
            #endif
                        }

                        /// <summary><c>Value</c></summary>
                        /// <remarks><see cref="Demo.Model.Value"/></remarks>
                        public int Value => _source.Value;
                    }
                }
            }
            """;

        TestHarness.AssertGenerated(expected, source);
    }

    [Fact]
    public void Includes_only_readable_public_instance_members()
    {
        var source = TestSources.Wrap("""
            public class Model
            {
                public int GetSet { get; set; }
                public int GetOnly { get; }
                public int Init { get; init; }
                public int Expression => 1;
                public int PrivateSet { get; private set; }
                public int Field;
                public int WriteOnly { set { } }
                public static int Static { get; set; }
                private int Private { get; set; }
                internal int Internal { get; set; }
                public int this[int i] => i;
            }
            """);

        const string expected = """
            namespace Demo
            {
                [global::System.CodeDom.Compiler.GeneratedCode("WinDataBinding.SourceGenerator", "1.0.0.0")]
                partial class ModelBinder
                {
                    private readonly global::Demo.Model _source;

                    public ModelBinder(global::Demo.Model source)
                    {
            #if NET6_0_OR_GREATER
                        global::System.ArgumentNullException.ThrowIfNull(source);
                        _source = source;
            #else
                        _source = source ?? throw new global::System.ArgumentNullException(nameof(source));
            #endif
                    }

                    /// <summary><c>GetSet</c></summary>
                    /// <remarks><see cref="Demo.Model.GetSet"/></remarks>
                    public int GetSet => _source.GetSet;

                    /// <summary><c>GetOnly</c></summary>
                    /// <remarks><see cref="Demo.Model.GetOnly"/></remarks>
                    public int GetOnly => _source.GetOnly;

                    /// <summary><c>Init</c></summary>
                    /// <remarks><see cref="Demo.Model.Init"/></remarks>
                    public int Init => _source.Init;

                    /// <summary><c>Expression</c></summary>
                    /// <remarks><see cref="Demo.Model.Expression"/></remarks>
                    public int Expression => _source.Expression;

                    /// <summary><c>PrivateSet</c></summary>
                    /// <remarks><see cref="Demo.Model.PrivateSet"/></remarks>
                    public int PrivateSet => _source.PrivateSet;

                    /// <summary><c>Field</c></summary>
                    /// <remarks><see cref="Demo.Model.Field"/></remarks>
                    public int Field => _source.Field;
                }
            }
            """;

        // Write-only, static, private, internal and the indexer are all left out.
        TestHarness.AssertGenerated(expected, source);
    }

    [Fact]
    public void Resolves_name_collisions_first_come_first_served()
    {
        // Address is declared first, so the flattened chain keeps the plain name and the
        // root property that would collide gets widened.
        var source = TestSources.Wrap("""
            public class Address { public string Street { get; set; } }

            public class Model
            {
                public Address Address { get; set; }
                public string Address_Street { get; set; }
            }
            """);

        var result = TestHarness.AssertCompiles(source);

        Assert.Contains("public string? Address_Street => _source.Address?.Street;", result.Body);
        Assert.Contains("public string? Address_Street_ => _source.Address_Street;", result.Body);
    }

    [Fact]
    public void Widens_the_separator_when_the_flattened_chain_loses_the_race()
    {
        // Address_Street is declared first this time, so the chain widens to a double underscore.
        var source = TestSources.Wrap("""
            public class Address { public string Street { get; set; } }

            public class Model
            {
                public string Address_Street { get; set; }
                public Address Address { get; set; }
            }
            """);

        var result = TestHarness.AssertCompiles(source);

        Assert.Contains("public string? Address_Street => _source.Address_Street;", result.Body);
        Assert.Contains("public string? Address__Street => _source.Address?.Street;", result.Body);
    }
}
