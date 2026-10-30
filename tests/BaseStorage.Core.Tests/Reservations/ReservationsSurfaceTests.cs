namespace BaseStorage.Core.Tests.Reservations;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Reservations;
using BaseStorage.Core.Tests.Snapshots;

/// <summary>AC-1 and AC-3 (S7): the enums, the exported surface of the Reservations namespace, and the rule that Core cannot force a chest closed.</summary>
public sealed class ReservationsSurfaceTests
{
    private const string Namespace = "BaseStorage.Core.Reservations";

    private const BindingFlags AllDeclared = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
        | BindingFlags.Static | BindingFlags.DeclaredOnly;

    private const BindingFlags PublicDeclared = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static
        | BindingFlags.DeclaredOnly;

    private static readonly string[] ExpectedTypeNames =
    {
        "BaseStorage.Core.Reservations.ChestReservations`1",
        "BaseStorage.Core.Reservations.Reservation`1",
        "BaseStorage.Core.Reservations.ReservationEvent",
        "BaseStorage.Core.Reservations.ReservationFailure",
        "BaseStorage.Core.Reservations.ReservationState",
        "BaseStorage.Core.Reservations.ReservationTransition",
        "BaseStorage.Core.Reservations.ReservationTransitions",
    };

    // GW-U-150 (AC-1): the three enums have the contracted members, values and underlying type.
    [Fact]
    public void ReservationStateHasTheSixContractedMembers()
    {
        AssertEnum(
            typeof(ReservationState),
            ("Requesting", 0),
            ("Granted", 1),
            ("Writing", 2),
            ("Releasing", 3),
            ("Completed", 4),
            ("Failed", 5));
    }

    // GW-U-150 (AC-1)
    [Fact]
    public void ReservationEventHasTheNineContractedMembersInOrder()
    {
        AssertEnum(
            typeof(ReservationEvent),
            ("Grant", 0),
            ("Deny", 1),
            ("Cancel", 2),
            ("BeginWrite", 3),
            ("BeginRelease", 4),
            ("CompleteRelease", 5),
            ("RequesterDisconnected", 6),
            ("OwnerLost", 7),
            ("InUseByOther", 8));
    }

    // GW-U-150 (AC-1)
    [Fact]
    public void ReservationFailureHasTheSevenContractedMembers()
    {
        AssertEnum(
            typeof(ReservationFailure),
            ("None", 0),
            ("Timeout", 1),
            ("Denied", 2),
            ("Cancelled", 3),
            ("RequesterDisconnected", 4),
            ("OwnerLost", 5),
            ("InUseByOther", 6));
    }

    // GW-U-157 (AC-1): the namespace exports exactly the 7 contracted full names.
    [Fact]
    public void NamespaceExportsExactlyTheSevenContractedTypes()
    {
        string[] exported = ContractTypes.CoreAssembly().GetExportedTypes()
            .Where(t => t.Namespace == Namespace)
            .Select(t => t.FullName ?? string.Empty)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            ExpectedTypeNames.OrderBy(name => name, StringComparer.Ordinal).ToArray(),
            exported);
    }

    // GW-U-157 (AC-1): the public-surface type list of the existing suites is unchanged, at 13.
    [Fact]
    public void ExistingSurfaceStillHasThirteenTypes()
    {
        Assert.Equal(13, ContractTypes.SurfaceTypes().Count);
    }

    // GW-U-157 (AC-1, R4): every public property of every type is get-only.
    [Fact]
    public void EveryPublicPropertyOfEveryTypeHasNoSetter()
    {
        var offenders = new List<string>();
        foreach (Type type in ReservationTypes())
        {
            foreach (PropertyInfo property in type.GetProperties(PublicDeclared))
            {
                if (property.SetMethod is not null)
                {
                    offenders.Add(type.Name + "." + property.Name);
                }
            }
        }

        Assert.Empty(offenders);
    }

    // GW-U-157 (AC-1, R5): the generic classes are sealed, implement no IEquatable and declare no equality members.
    [Fact]
    public void GenericClassesAreSealedWithReferenceEquality()
    {
        foreach (Type generic in new[] { typeof(Reservation<>), typeof(ChestReservations<>) })
        {
            Assert.True(generic.IsSealed, generic.Name + " must be sealed.");
            Assert.DoesNotContain(
                generic.GetInterfaces(),
                i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEquatable<>));

            string[] equality = generic.GetMethods(AllDeclared)
                .Select(m => m.Name)
                .Where(name => name is "Equals" or "GetHashCode" or "op_Equality" or "op_Inequality")
                .ToArray();
            Assert.Empty(equality);
        }
    }

    // GW-U-157 (AC-1, R5): the transition row is a value type with value equality.
    [Fact]
    public void ReservationTransitionIsAValueTypeImplementingIEquatable()
    {
        Assert.True(typeof(ReservationTransition).IsValueType);
        Assert.Contains(typeof(IEquatable<ReservationTransition>), typeof(ReservationTransition).GetInterfaces());
    }

    // GW-U-157 (AC-1): the table holder is a static class.
    [Fact]
    public void ReservationTransitionsIsStatic()
    {
        Assert.True(typeof(ReservationTransitions).IsAbstract && typeof(ReservationTransitions).IsSealed);
    }

    // GW-U-157 (AC-1): the operation parameter has no class or struct constraint.
    [Fact]
    public void OperationParameterHasNoClassOrStructConstraint()
    {
        const GenericParameterAttributes Mask = GenericParameterAttributes.ReferenceTypeConstraint
            | GenericParameterAttributes.NotNullableValueTypeConstraint;

        foreach (Type generic in new[] { typeof(Reservation<>), typeof(ChestReservations<>) })
        {
            Type parameter = generic.GetGenericArguments()[0];
            Assert.Equal(GenericParameterAttributes.None, parameter.GenericParameterAttributes & Mask);
        }
    }

    // GW-U-157 (AC-1): Active and CheckTimeouts return ReadOnlyCollection instances.
    [Fact]
    public void ActiveAndCheckTimeoutsReturnReadOnlyCollections()
    {
        var book = new ChestReservations<OrganizeRun>(ReservationFixtures.NewClock(), ReservationFixtures.TimeoutMs);

        Assert.Equal(typeof(ReadOnlyCollection<>), book.Active.GetType().GetGenericTypeDefinition());
        Assert.Equal(typeof(ReadOnlyCollection<>), book.CheckTimeouts().GetType().GetGenericTypeDefinition());
    }

    // GW-U-175 (AC-3, S7): no field, property, parameter or return type of the 7 types is or contains IContainerLayer,
    // or any Abstractions type other than IClock.
    [Fact]
    public void NoMemberOfAnyReservationTypeReferencesAContainerLayer()
    {
        var offenders = new List<string>();
        foreach (Type type in ReservationTypes())
        {
            foreach ((string origin, Type used) in SignatureTypesOf(type))
            {
                foreach (Type leaf in Leaves(used))
                {
                    if (IsForbidden(leaf))
                    {
                        offenders.Add(origin + " uses " + leaf.FullName);
                    }
                }
            }
        }

        Assert.Empty(offenders);
    }

    // GW-U-175 (AC-3, S7): no public member name says Close, Evict, Force or TakeOver.
    [Fact]
    public void NoPublicMemberNameSaysCloseEvictForceOrTakeOver()
    {
        string[] banned = { "Close", "Evict", "Force", "TakeOver" };
        var offenders = new List<string>();
        foreach (Type type in ReservationTypes())
        {
            foreach (MemberInfo member in type.GetMembers(PublicDeclared))
            {
                foreach (string word in banned)
                {
                    if (member.Name.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        offenders.Add(type.Name + "." + member.Name);
                    }
                }
            }
        }

        Assert.Empty(offenders);
    }

    // GW-U-175 (AC-1): the ReservationEvent member names are exactly the 9 contract names.
    [Fact]
    public void ReservationEventNamesAreExactlyTheNineContractNames()
    {
        string[] names = Enum.GetValues(typeof(ReservationEvent)).Cast<object>()
            .Select(v => Enum.GetName(typeof(ReservationEvent), v) ?? string.Empty)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            new[]
            {
                "BeginRelease", "BeginWrite", "Cancel", "CompleteRelease", "Deny",
                "Grant", "InUseByOther", "OwnerLost", "RequesterDisconnected",
            },
            names);
    }

    private static void AssertEnum(Type type, params (string Name, int Value)[] expected)
    {
        Assert.Equal(typeof(int), Enum.GetUnderlyingType(type));

        (string Name, int Value)[] actual = Enum.GetValues(type).Cast<object>()
            .Select(v => (Name: Enum.GetName(type, v) ?? string.Empty, Value: Convert.ToInt32(v)))
            .OrderBy(entry => entry.Value)
            .ToArray();

        Assert.Equal(expected, actual);
    }

    private static IReadOnlyList<Type> ReservationTypes()
    {
        Type[] types = ContractTypes.CoreAssembly().GetExportedTypes()
            .Where(t => t.Namespace == Namespace)
            .OrderBy(t => t.FullName, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(ExpectedTypeNames.Length, types.Length);
        return types;
    }

    private static IEnumerable<(string Origin, Type Used)> SignatureTypesOf(Type owner)
    {
        string name = owner.FullName ?? owner.Name;
        const BindingFlags Every = AllDeclared;

        foreach (FieldInfo field in owner.GetFields(Every))
        {
            yield return (name + "." + field.Name, field.FieldType);
        }

        foreach (PropertyInfo property in owner.GetProperties(Every))
        {
            yield return (name + "." + property.Name, property.PropertyType);
        }

        foreach (ConstructorInfo ctor in owner.GetConstructors(Every))
        {
            foreach (ParameterInfo parameter in ctor.GetParameters())
            {
                yield return (name + ".ctor(" + parameter.Name + ")", parameter.ParameterType);
            }
        }

        foreach (MethodInfo method in owner.GetMethods(Every))
        {
            yield return (name + "." + method.Name + " return", method.ReturnType);
            foreach (ParameterInfo parameter in method.GetParameters())
            {
                yield return (name + "." + method.Name + "(" + parameter.Name + ")", parameter.ParameterType);
            }
        }
    }

    private static IEnumerable<Type> Leaves(Type type)
    {
        if (type.HasElementType)
        {
            foreach (Type inner in Leaves(type.GetElementType()!))
            {
                yield return inner;
            }

            yield break;
        }

        if (type.IsGenericType)
        {
            yield return type.GetGenericTypeDefinition();
            foreach (Type argument in type.GetGenericArguments())
            {
                foreach (Type inner in Leaves(argument))
                {
                    yield return inner;
                }
            }

            yield break;
        }

        yield return type;
    }

    private static bool IsForbidden(Type type)
    {
        if (type.Name == "IContainerLayer")
        {
            return true;
        }

        return type.Namespace is not null
            && type.Namespace.StartsWith("BaseStorage.Core.Abstractions", StringComparison.Ordinal)
            && type != typeof(IClock);
    }
}
