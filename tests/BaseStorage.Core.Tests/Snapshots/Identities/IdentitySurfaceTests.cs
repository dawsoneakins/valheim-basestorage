namespace BaseStorage.Core.Tests.Snapshots.Identities;

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Snapshots.Identities;

/// <summary>GW-U-2487 (S-QR-001 AC-2, R4, contract section 1): the identity surface is one immutable exported type.</summary>
public sealed class IdentitySurfaceTests
{
    private const string Namespace = "BaseStorage.Core.Snapshots.Identities";
    private const BindingFlags Declared = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static
        | BindingFlags.DeclaredOnly;

    // GW-U-2487
    [Fact]
    public void TheIdentitySurfaceIsOneImmutableType()
    {
        Type[] exported = typeof(ItemStackIdentity).Assembly.GetExportedTypes()
            .Where(type => type.Namespace == Namespace)
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(new[] { typeof(ItemStackIdentity) }, exported);

        foreach (PropertyInfo property in typeof(ItemStackIdentity).GetProperties(Declared))
        {
            Assert.True(property.SetMethod is null, "Property has a setter: " + property.Name);
        }

        Assert.True(typeof(ItemStackSnapshot).GetProperty("Identity")!.SetMethod is null);

        foreach (FieldInfo field in typeof(ItemStackIdentity).GetFields(Declared))
        {
            if (!field.IsStatic)
            {
                Assert.True(field.IsInitOnly, "Instance field is not readonly: " + field.Name);
            }
        }

        Assert.IsType<ReadOnlyCollection<System.Collections.Generic.KeyValuePair<string, string>>>(ItemStackIdentity.Empty.CustomData);

        ContractTypes.SurfaceTypes();
    }
}
