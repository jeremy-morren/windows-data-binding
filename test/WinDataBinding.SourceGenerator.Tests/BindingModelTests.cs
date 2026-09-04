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
                [global::System.CodeDom.Compiler.GeneratedCode("WinDataBinding.SourceGenerator", "1.0.0")]
                partial class PersonModelBinder : global::System.IEquatable<PersonModelBinder>
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

                    [return: global::System.Diagnostics.CodeAnalysis.NotNullIfNotNull("source")]
                    public static PersonModelBinder? Create(global::Demo.Person? source) =>
                        source is not null ? new PersonModelBinder(source) : null;

                    public bool Equals(PersonModelBinder? other) =>
                        other is not null && global::System.Collections.Generic.EqualityComparer<global::Demo.Person>.Default.Equals(_source, other._source);

                    public override bool Equals(object? obj) => obj is PersonModelBinder other && Equals(other);

                    public override int GetHashCode() => _source is null ? 0 : global::System.Collections.Generic.EqualityComparer<global::Demo.Person>.Default.GetHashCode(_source);

                    [global::System.ComponentModel.Description("Person name")]
                    public string? Name => _source.Name;

                    [global::System.ComponentModel.Description("Person's address")]
                    public global::Demo.Address? Address => _source.Address;

                    [global::System.ComponentModel.Description("Person's address")]
                    public string? Address_Street => _source.Address?.Street;

                    [global::System.ComponentModel.Description("Person's address")]
                    public string? Address_City => _source.Address?.City;

                    [global::System.ComponentModel.Description("Person's address: Address state")]
                    public string? Address_State => _source.Address?.State;

                    [global::System.ComponentModel.Description("The timestamp that the person was created at")]
                    public global::System.DateTime CreatedAt => _source.CreatedAt.ToDateTimeUtc();

                    [global::System.ComponentModel.Description("The timestamp that the person was created at (Formatted)")]
                    public string? CreatedAt_Formatted => _source.CreatedAt.ToString(null, null);

                    [global::System.ComponentModel.Description("Last login in the user's local timezone")]
                    public global::Demo.LoginInfo? LastLogin => _source.LastLogin;

                    [global::System.ComponentModel.Description("Last login in the user's local timezone")]
                    public int? LastLogin_Id => _source.LastLogin?.Id;

                    [global::System.ComponentModel.Description("Last login in the user's local timezone: Timestamp the login occurred at")]
                    public global::System.DateTimeOffset? LastLogin_Timestamp => _source.LastLogin?.Timestamp.ToDateTimeOffset();

                    [global::System.ComponentModel.Description("Last login in the user's local timezone: Timestamp the login occurred at (Utc)")]
                    public global::System.DateTime? LastLogin_Timestamp_Utc => _source.LastLogin?.Timestamp.ToDateTimeUtc();

                    [global::System.ComponentModel.Description("Last login in the user's local timezone: Timestamp the login occurred at (Local)")]
                    public global::System.DateTime? LastLogin_Timestamp_Local => _source.LastLogin?.Timestamp.ToDateTimeUnspecified();

                    [global::System.ComponentModel.Description("Last login in the user's local timezone: Timestamp the login occurred at (Offset)")]
                    public global::System.TimeSpan? LastLogin_Timestamp_Offset => _source.LastLogin?.Timestamp.Offset.ToTimeSpan();

                    [global::System.ComponentModel.Description("Last login in the user's local timezone: Timestamp the login occurred at (Timezone)")]
                    public string? LastLogin_Timestamp_Timezone => _source.LastLogin?.Timestamp.Zone.Id;

                    [global::System.ComponentModel.Description("Last login in the user's local timezone: Timestamp the login occurred at (Formatted)")]
                    public string? LastLogin_Timestamp_Formatted => _source.LastLogin?.Timestamp.ToString(null, null);
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
                [global::System.CodeDom.Compiler.GeneratedCode("WinDataBinding.SourceGenerator", "1.0.0")]
                partial class ModelBinder : global::System.IEquatable<ModelBinder>
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

                    [return: global::System.Diagnostics.CodeAnalysis.NotNullIfNotNull("source")]
                    public static ModelBinder? Create(global::Demo.Model? source) =>
                        source is not null ? new ModelBinder(source) : null;

                    public bool Equals(ModelBinder? other) =>
                        other is not null && global::System.Collections.Generic.EqualityComparer<global::Demo.Model>.Default.Equals(_source, other._source);

                    public override bool Equals(object? obj) => obj is ModelBinder other && Equals(other);

                    public override int GetHashCode() => _source is null ? 0 : global::System.Collections.Generic.EqualityComparer<global::Demo.Model>.Default.GetHashCode(_source);

                    public global::System.Collections.Generic.List<int>? Numbers => _source.Numbers;

                    public int? Numbers_Count => _source.Numbers?.Count;

                    public string? Numbers_Display => _source.Numbers is { } items ? global::System.String.Join(", ", global::System.Linq.Enumerable.Select(items, item => item.ToString(null, null))) : null;

                    public string? Numbers_Array => Numbers_Display is { } display ? $"[{display}]" : null;

                    public string[]? Names => _source.Names;

                    public int? Names_Count => _source.Names?.Length;

                    public string? Names_Display => _source.Names is { } items ? global::System.String.Join(", ", items) : null;

                    public string? Names_Array => Names_Display is { } display ? $"[{display}]" : null;

                    public global::Demo.Colour Colour => _source.Colour;

                    public global::Demo.Colour? MaybeColour => _source.MaybeColour;

                    public int Field => _source.Field;

                    public int? Nullable => _source.Nullable;

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
                [global::System.CodeDom.Compiler.GeneratedCode("WinDataBinding.SourceGenerator", "1.0.0")]
                partial class ModelBinder : global::System.IEquatable<ModelBinder>
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

                    [return: global::System.Diagnostics.CodeAnalysis.NotNullIfNotNull("source")]
                    public static ModelBinder? Create(global::Demo.Model? source) =>
                        source is not null ? new ModelBinder(source) : null;

                    public bool Equals(ModelBinder? other) =>
                        other is not null && global::System.Collections.Generic.EqualityComparer<global::Demo.Model>.Default.Equals(_source, other._source);

                    public override bool Equals(object? obj) => obj is ModelBinder other && Equals(other);

                    public override int GetHashCode() => _source is null ? 0 : global::System.Collections.Generic.EqualityComparer<global::Demo.Model>.Default.GetHashCode(_source);

                    [global::System.ComponentModel.Description("From the first part")]
                    public string? First => _source.First;

                    public int Second => _source.Second;

                    public global::Demo.Detail? Detail => _source.Detail;

                    public string? Detail_Left => _source.Detail?.Left;

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
                [global::System.CodeDom.Compiler.GeneratedCode("WinDataBinding.SourceGenerator", "1.0.0")]
                partial class ModelBinder : global::System.IEquatable<ModelBinder>
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

                    [return: global::System.Diagnostics.CodeAnalysis.NotNullIfNotNull("source")]
                    internal static ModelBinder? Create(global::Demo.Model? source) =>
                        source is not null ? new ModelBinder(source) : null;

                    public bool Equals(ModelBinder? other) =>
                        other is not null && global::System.Collections.Generic.EqualityComparer<global::Demo.Model>.Default.Equals(_source, other._source);

                    public override bool Equals(object? obj) => obj is ModelBinder other && Equals(other);

                    public override int GetHashCode() => _source is null ? 0 : global::System.Collections.Generic.EqualityComparer<global::Demo.Model>.Default.GetHashCode(_source);

                    public int Own => _source.Own;

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
                    [global::System.CodeDom.Compiler.GeneratedCode("WinDataBinding.SourceGenerator", "1.0.0")]
                    partial class Binder : global::System.IEquatable<Binder>
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

                        [return: global::System.Diagnostics.CodeAnalysis.NotNullIfNotNull("source")]
                        public static Binder? Create(global::Demo.Model? source) =>
                            source is not null ? new Binder(source) : null;

                        public bool Equals(Binder? other) =>
                            other is not null && global::System.Collections.Generic.EqualityComparer<global::Demo.Model>.Default.Equals(_source, other._source);

                        public override bool Equals(object? obj) => obj is Binder other && Equals(other);

                        public override int GetHashCode() => _source is null ? 0 : global::System.Collections.Generic.EqualityComparer<global::Demo.Model>.Default.GetHashCode(_source);

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
                [global::System.CodeDom.Compiler.GeneratedCode("WinDataBinding.SourceGenerator", "1.0.0")]
                partial class ModelBinder : global::System.IEquatable<ModelBinder>
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

                    [return: global::System.Diagnostics.CodeAnalysis.NotNullIfNotNull("source")]
                    public static ModelBinder? Create(global::Demo.Model? source) =>
                        source is not null ? new ModelBinder(source) : null;

                    public bool Equals(ModelBinder? other) =>
                        other is not null && global::System.Collections.Generic.EqualityComparer<global::Demo.Model>.Default.Equals(_source, other._source);

                    public override bool Equals(object? obj) => obj is ModelBinder other && Equals(other);

                    public override int GetHashCode() => _source is null ? 0 : global::System.Collections.Generic.EqualityComparer<global::Demo.Model>.Default.GetHashCode(_source);

                    public int GetSet => _source.GetSet;

                    public int GetOnly => _source.GetOnly;

                    public int Init => _source.Init;

                    public int Expression => _source.Expression;

                    public int PrivateSet => _source.PrivateSet;

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

    [Fact]
    public void Binds_a_descriptive_type_without_traversing_it()
    {
        // Each of these is an object graph by shape and a single value by intent. Walking Type or
        // CultureInfo would flatten hundreds of members, and Type's graph refers back to itself.
        var source = TestSources.Wrap("""
            public class Model
            {
                public System.Type Kind { get; set; }
                public System.Globalization.CultureInfo Culture { get; set; }
            }
            """);

        var result = TestHarness.AssertCompiles(source);

        result.Should().HaveNoDiagnostics();

        result.Source.Should().Contain("public global::System.Type? Kind => _source.Kind;");
        result.Source.Should().Contain(
            "public global::System.Globalization.CultureInfo? Culture => _source.Culture;");

        // Nothing is flattened out of them, and a leaf gets no rendered twin.
        result.Source.Should().NotContain("Kind_");
        result.Source.Should().NotContain("Culture_");
    }

    [Fact]
    public void Binds_a_descriptive_type_before_net6_as_well()
    {
        var source = TestSources.Wrap("""
            public class Model
            {
                public System.Type Kind { get; set; }
                public System.Globalization.CultureInfo Culture { get; set; }
            }
            """);

        var result = TestHarness.AssertCompiles(source, Target.NetStandard20);

        result.Source.Should().Contain("public global::System.Type? Kind => _source.Kind;");
        result.Source.Should().Contain(
            "public global::System.Globalization.CultureInfo? Culture => _source.Culture;");
    }

    [Fact]
    public void Reaches_a_member_declared_more_than_once_in_the_hierarchy_only_once()
    {
        // An override is declared on the derived type and again on the base, and the walk goes up the chain.
        var source = TestSources.Wrap("""
            public abstract class Base
            {
                public virtual int Shared => 0;
                public virtual string Hidden => null;
            }

            public class Derived : Base
            {
                public override int Shared => 1;
                public new int Hidden => 2;
            }

            public class Model { public Derived Value { get; set; } }
            """);

        var result = TestHarness.AssertCompiles(source);

        result.Source.Should().Contain("public int? Value_Shared => _source.Value?.Shared;");
        result.Source.Should().NotContain("Value__Shared");

        // 'new' hides rather than overrides, and the derived declaration is the one reachable by name.
        result.Source.Should().Contain("public int? Value_Hidden => _source.Value?.Hidden;");
        result.Source.Should().NotContain("Value__Hidden");
    }

    [Fact]
    public void Leaves_out_a_member_whose_type_cannot_be_a_property()
    {
        // ReadOnlyMemory<T>.Span is a ref struct: it cannot be boxed, and a lifted chain would ask for
        // ReadOnlySpan<char>?, which does not compile at all.
        var source = TestSources.Wrap("""
            public class Inner { public System.ReadOnlyMemory<char> Text { get; set; } }

            public class Model { public Inner Inner { get; set; } }
            """);

        var result = TestHarness.AssertCompiles(source);

        result.Source.Should().Contain("public int? Inner_Text_Length => _source.Inner?.Text.Length;");
        result.Source.Should().NotContain("Span");
    }

    [Fact]
    public void Binds_a_number_like_type_without_flattening_its_predicates()
    {
        var source = TestSources.Wrap("""
            public class Model
            {
                public System.Numerics.BigInteger Big { get; set; }
                public System.Text.Rune Rune { get; set; }
                public System.Range Range { get; set; }
            }
            """);

        var result = TestHarness.AssertCompiles(source);

        result.Should().HaveNoDiagnostics();

        result.Source.Should().Contain("public global::System.Numerics.BigInteger Big => _source.Big;");
        result.Source.Should().Contain("public global::System.Text.Rune Rune => _source.Rune;");

        // IsEven, IsOne, IsPowerOfTwo, IsZero, Sign, Utf8SequenceLength and the rest are noise around a
        // value that already renders itself.
        result.Source.Should().NotContain("Big_");
        result.Source.Should().NotContain("Rune_");

        // Range keeps its two ends, each an Index that binds as it stands.
        result.Source.Should().Contain("public global::System.Index Range_Start => _source.Range.Start;");
        result.Source.Should().Contain("public global::System.Index Range_End => _source.Range.End;");
        result.Source.Should().NotContain("Range_Start_");
    }
}
