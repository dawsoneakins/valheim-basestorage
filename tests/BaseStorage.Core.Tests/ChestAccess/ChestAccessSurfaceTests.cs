namespace BaseStorage.Core.Tests.ChestAccess;

using System.Reflection;
using BaseStorage.Core.ChestAccess;
using BaseStorage.Core.Reservations;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Tests.Snapshots;
using BaseStorage.Core.Ui;

/// <summary>GW-U-760 (AC-1): the exported surface of BaseStorage.Core.ChestAccess is exactly the 4 contract types with the contract 2.1-2.4 members.</summary>
public sealed class ChestAccessSurfaceTests
{
    private const string Namespace = "BaseStorage.Core.ChestAccess";

    private const BindingFlags PublicDeclared = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    // GW-U-760
    [Fact]
    public void NamespaceExportsExactlyTheFourContractedTypes()
    {
        string[] exported = ContractTypes.CoreAssembly().GetExportedTypes()
            .Where(t => t.Namespace == Namespace)
            .Select(t => t.FullName ?? string.Empty)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            new[]
            {
                "BaseStorage.Core.ChestAccess.ChestAccessRules",
                "BaseStorage.Core.ChestAccess.ChestInUse",
                "BaseStorage.Core.ChestAccess.ChestObservation",
                "BaseStorage.Core.ChestAccess.ReservationRoute",
            },
            exported);
    }

    // GW-U-760: ChestAccessRules is static and has exactly the 12 contract methods with the contract parameter types.
    [Fact]
    public void ChestAccessRulesIsStaticWithExactlyTheContractMethods()
    {
        Type rules = typeof(ChestAccessRules);
        Assert.True(rules.IsAbstract && rules.IsSealed, "ChestAccessRules must be static.");

        string[] signatures = rules.GetMethods(PublicDeclared)
            .Select(m => m.Name + "(" + string.Join(",", m.GetParameters().Select(p => p.ParameterType.Name)) + "):" + m.ReturnType.Name)
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            new[]
            {
                "ClaimsOwnership(ReservationRoute):Boolean",
                "EventForResponse(Boolean):Nullable`1",
                "EventForRoute(ReservationRoute):Nullable`1",
                "HolderLabel(String,StringTable):String",
                "InUseBy(ContainerId,ChestObservation,StringTable):ChestInUse",
                "IsInUseByOther(ChestObservation):Boolean",
                "LateResponseWindow(Int32):TimeSpan",
                "Poll(ReservationState,Boolean,Boolean,Boolean,Boolean):Nullable`1",
                "Route(ChestObservation):ReservationRoute",
                "SendsRequest(ReservationRoute):Boolean",
                "SetsInUse(ReservationRoute):Boolean",
                "ShouldClearInUse(Boolean,Boolean,Boolean,Boolean):Boolean",
            },
            signatures);
        Assert.Empty(rules.GetProperties(PublicDeclared));
        Assert.Empty(rules.GetFields(PublicDeclared));
    }

    // GW-U-760: ChestObservation has the ten get-only properties, one constructor and value equality.
    [Fact]
    public void ChestObservationHasTenGetOnlyPropertiesAndValueEquality()
    {
        Type type = typeof(ChestObservation);
        Assert.True(type.IsSealed);
        Assert.Contains(typeof(IEquatable<ChestObservation>), type.GetInterfaces());

        PropertyInfo[] properties = type.GetProperties(PublicDeclared);
        Assert.Equal(10, properties.Length);
        Assert.All(properties, p => Assert.Null(p.SetMethod));
        Assert.Equal(
            new[] { "AccessAllowed", "InUseFlag", "IsServer", "LocalSessionId", "OpenLocally", "OwnerConnected", "OwnerListed", "OwnerName", "OwnerSessionId", "ViewValid" },
            properties.Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal).ToArray());

        ConstructorInfo ctor = Assert.Single(type.GetConstructors(BindingFlags.Public | BindingFlags.Instance));
        Assert.Equal(
            new[] { "viewValid", "accessAllowed", "openLocally", "inUseFlag", "localSessionId", "ownerSessionId", "ownerListed", "ownerConnected", "ownerName", "isServer" },
            ctor.GetParameters().Select(p => p.Name).ToArray());
        Assert.NotNull(type.GetMethod("op_Equality", PublicDeclared));
        Assert.NotNull(type.GetMethod("op_Inequality", PublicDeclared));
    }

    // GW-U-760: ChestInUse has two get-only properties, one constructor and value equality.
    [Fact]
    public void ChestInUseHasTwoGetOnlyPropertiesAndValueEquality()
    {
        Type type = typeof(ChestInUse);
        Assert.True(type.IsSealed);
        Assert.Contains(typeof(IEquatable<ChestInUse>), type.GetInterfaces());

        PropertyInfo[] properties = type.GetProperties(PublicDeclared);
        Assert.Equal(new[] { "Chest", "HolderName" }, properties.Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal).ToArray());
        Assert.All(properties, p => Assert.Null(p.SetMethod));
        Assert.Equal(typeof(ContainerId), type.GetProperty("Chest")!.PropertyType);
        Assert.Equal(typeof(string), type.GetProperty("HolderName")!.PropertyType);

        ConstructorInfo ctor = Assert.Single(type.GetConstructors(BindingFlags.Public | BindingFlags.Instance));
        Assert.Equal(new[] { "chest", "holderName" }, ctor.GetParameters().Select(p => p.Name).ToArray());
    }

    // GW-U-760: the nullable annotations of the contract.
    [Fact]
    public void NullableAnnotationsFollowTheContract()
    {
        var context = new NullabilityInfoContext();

        Assert.Equal(NullabilityState.Nullable, context.Create(typeof(ChestObservation).GetProperty("OwnerName")!).ReadState);
        Assert.Equal(NullabilityState.NotNull, context.Create(typeof(ChestInUse).GetProperty("HolderName")!).ReadState);
        Assert.Equal(NullabilityState.Nullable, context.Create(typeof(ChestAccessRules).GetMethod("InUseBy")!.ReturnParameter).ReadState);
        Assert.Equal(typeof(ReservationEvent?), typeof(ChestAccessRules).GetMethod("EventForRoute")!.ReturnType);
        Assert.Equal(typeof(ReservationEvent?), typeof(ChestAccessRules).GetMethod("EventForResponse")!.ReturnType);
        Assert.Equal(typeof(ReservationEvent?), typeof(ChestAccessRules).GetMethod("Poll")!.ReturnType);
        Assert.Equal(NullabilityState.Nullable, context.Create(typeof(ChestAccessRules).GetMethod("HolderLabel")!.GetParameters()[0]).WriteState);
    }

    // GW-U-760: the S-E005-001 surface is untouched: still 7 exported Reservations types.
    [Fact]
    public void TheReservationsSurfaceStillHasSevenTypes()
    {
        int count = ContractTypes.CoreAssembly().GetExportedTypes().Count(t => t.Namespace == "BaseStorage.Core.Reservations");

        Assert.Equal(7, count);
        Assert.NotNull(typeof(StringTable));
    }
}
