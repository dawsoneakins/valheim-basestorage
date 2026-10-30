namespace BaseStorage.Core.Tests.Locking;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Locking;
using BaseStorage.Core.Settings;
using BaseStorage.Core.Ui;

/// <summary>AC-8 (contract 1, 2.1, 3.1, 3.2, 4.0, ADR-GWIN-027): the exported shape of BaseStorage.Core.Locking, checked by reflection.</summary>
public sealed class LockingSurfaceTests
{
    private const string LockingNamespace = "BaseStorage.Core.Locking";
    private const BindingFlags PublicDeclared = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    // GW-U-283 (AC-8): the exported types of the namespace are exactly the four contract names, ordinal-sorted.
    [Fact]
    public void ExportedTypesAreExactlyTheFourLockingNames()
    {
        string[] exported = typeof(OrganizeLock).Assembly.GetExportedTypes()
            .Where(type => type.Namespace == LockingNamespace)
            .Select(type => type.FullName!)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            new[]
            {
                "BaseStorage.Core.Locking.IOrganizeLockStore",
                "BaseStorage.Core.Locking.OrganizeLock",
                "BaseStorage.Core.Locking.OrganizeLockResult",
                "BaseStorage.Core.Locking.OrganizeLockStatus",
            },
            exported);
    }

    // GW-U-283 (AC-8, contract 2.1): IOrganizeLockStore has exactly Read, Write and Clear with the contracted parameter and return types.
    [Fact]
    public void StoreInterfaceMembersAreExactlyTheContractList()
    {
        Assert.Equal(
            Sorted(
                "M Clear(StationId) : Void",
                "M Read(StationId) : String",
                "M Write(StationId, String) : Void"),
            Members(typeof(IOrganizeLockStore)));
    }

    // GW-U-283 (AC-8, contract 4.0): OrganizeLock has exactly the public constructor and the four contracted members.
    [Fact]
    public void ServiceMembersAreExactlyTheContractList()
    {
        Assert.Equal(
            Sorted(
                "M Acquire(IReadOnlyList<StationSnapshot>, Int64, String) : OrganizeLockResult",
                "M FindLockStation(IReadOnlyList<StationSnapshot>) : StationId",
                "M Release(IReadOnlyList<StationSnapshot>, Int64) : Boolean",
                "M Renew(IReadOnlyList<StationSnapshot>, Int64, String) : OrganizeLockResult"),
            Members(typeof(OrganizeLock)));

        ConstructorInfo[] constructors = typeof(OrganizeLock).GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        ConstructorInfo ctor = Assert.Single(constructors);
        Assert.Equal(
            new[] { "IOrganizeLockStore", "IClock", "BaseStorageSettings" },
            ctor.GetParameters().Select(parameter => Name(parameter.ParameterType)).ToArray());
    }

    // GW-U-283 (AC-8, contract 3.2): OrganizeLockResult has exactly the contracted factories, properties, equality and FormatMessage.
    [Fact]
    public void ResultMembersAreExactlyTheContractList()
    {
        Assert.Equal(
            Sorted(
                "M Equals(Object) : Boolean",
                "M Equals(OrganizeLockResult) : Boolean",
                "M FormatMessage(StringTable) : String",
                "M GetHashCode() : Int32",
                "M Granted(OrganizeLockRecord) : OrganizeLockResult",
                "M op_Equality(OrganizeLockResult, OrganizeLockResult) : Boolean",
                "M op_Inequality(OrganizeLockResult, OrganizeLockResult) : Boolean",
                "M Refused(OrganizeLockRecord) : OrganizeLockResult",
                "P HolderName : String",
                "P IsGranted : Boolean",
                "P MessageKey : String",
                "P NotHeld : OrganizeLockResult",
                "P Record : OrganizeLockRecord",
                "P Status : OrganizeLockStatus"),
            Members(typeof(OrganizeLockResult)));
    }

    // GW-U-283 (AC-8, contract 3.1): the status enum declares no public methods or properties; its values are checked in GW-U-272.
    [Fact]
    public void StatusEnumDeclaresNoMembersBeyondItsValues()
    {
        Assert.Empty(Members(typeof(OrganizeLockStatus)));
    }

    // GW-U-283 (AC-8): OrganizeLockResult is sealed, has no public constructor, and implements IEquatable of itself.
    [Fact]
    public void ResultIsSealedWithNoPublicConstructorAndIsEquatable()
    {
        Assert.True(typeof(OrganizeLockResult).IsSealed, "OrganizeLockResult must be sealed.");
        Assert.Empty(typeof(OrganizeLockResult).GetConstructors(BindingFlags.Public | BindingFlags.Instance));
        Assert.True(typeof(IEquatable<OrganizeLockResult>).IsAssignableFrom(typeof(OrganizeLockResult)));
    }

    // GW-U-283 (AC-8): OrganizeLock is sealed and not abstract, and FindLockStation is static.
    [Fact]
    public void ServiceIsSealedAndNotAbstractAndFindLockStationIsStatic()
    {
        Assert.True(typeof(OrganizeLock).IsSealed, "OrganizeLock must be sealed.");
        Assert.False(typeof(OrganizeLock).IsAbstract, "OrganizeLock must not be abstract.");
        MethodInfo? find = typeof(OrganizeLock).GetMethod("FindLockStation", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(find);
        Assert.True(find!.IsStatic);
    }

    // GW-U-283 (AC-8, R11): the nullable returns are reported as Nullable by NullabilityInfoContext.
    [Fact]
    public void NullableReturnsAreReportedAsNullable()
    {
        var context = new NullabilityInfoContext();

        Assert.Equal(NullabilityState.Nullable, context.Create(typeof(IOrganizeLockStore).GetMethod("Read")!.ReturnParameter).ReadState);
        Assert.Equal(NullabilityState.Nullable, context.Create(typeof(OrganizeLockResult).GetProperty("Record")!).ReadState);
        Assert.Equal(NullabilityState.Nullable, context.Create(typeof(OrganizeLockResult).GetProperty("HolderName")!).ReadState);
        Assert.Equal(NullabilityState.Nullable, context.Create(typeof(OrganizeLockResult).GetProperty("MessageKey")!).ReadState);
        Assert.Equal(
            NullabilityState.Nullable,
            context.Create(typeof(OrganizeLockResult).GetMethod("FormatMessage")!.ReturnParameter).ReadState);
    }

    private static string[] Sorted(params string[] items) => items.OrderBy(item => item, StringComparer.Ordinal).ToArray();

    private static string[] Members(Type type)
    {
        var lines = new List<string>();
        foreach (MethodInfo method in type.GetMethods(PublicDeclared))
        {
            if (method.IsSpecialName && !method.Name.StartsWith("op_", StringComparison.Ordinal))
            {
                continue;
            }

            string parameters = string.Join(", ", method.GetParameters().Select(parameter => Name(parameter.ParameterType)));
            lines.Add("M " + method.Name + "(" + parameters + ") : " + Name(method.ReturnType));
        }

        foreach (PropertyInfo property in type.GetProperties(PublicDeclared))
        {
            lines.Add("P " + property.Name + " : " + Name(property.PropertyType));
        }

        return Sorted(lines.ToArray());
    }

    private static string Name(Type type)
    {
        if (type.IsGenericType)
        {
            string definition = type.Name.Substring(0, type.Name.IndexOf('`'));
            return definition + "<" + string.Join(", ", type.GetGenericArguments().Select(Name)) + ">";
        }

        return type.Name;
    }
}
