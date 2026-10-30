namespace BaseStorage.Core.Tests.Persistence;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BaseStorage.Core.Tests.Snapshots;

/// <summary>
/// AC-2 and contract 0.1 (R1-R11) and section 1: the exported surface of BaseStorage.Core.Persistence is exactly the contract.
/// </summary>
public sealed class PersistenceSurfaceTests
{
    private const BindingFlags AllDeclared = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
        | BindingFlags.Static | BindingFlags.DeclaredOnly;

    // GW-U-078
    [Fact]
    public void ExportedTypesAreExactlyTheEightContractNames()
    {
        _ = PersistenceContract.ResolveAll();

        string[] exported = ContractTypes.CoreAssembly().GetExportedTypes()
            .Where(t => t.Namespace == PersistenceContract.Namespace)
            .Select(t => t.Name)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            PersistenceContract.SurfaceTypeNames.OrderBy(n => n, StringComparer.Ordinal).ToArray(),
            exported);
    }

    // GW-U-078
    [Fact]
    public void CodecSignaturesMatchTheContract()
    {
        var context = new NullabilityInfoContext();

        CheckCodec("CategoryCodec", "CategoryAssignment", context);
        CheckCodec("OrganizeLockCodec", "OrganizeLockRecord", context);
        CheckCodec("FavouritesCodec", "FavouriteSlots", context);
    }

    // GW-U-078
    [Fact]
    public void ClassesAreSealedAndInventorySlotIsReadonly()
    {
        foreach (Type type in PersistenceContract.ResolveAll())
        {
            Assert.True(type.IsSealed, $"{type.Name} must be sealed.");
        }

        Type slot = PersistenceContract.Resolve("InventorySlot");
        Assert.True(
            slot.IsValueType && slot.GetCustomAttributesData()
                .Any(attribute => attribute.AttributeType.FullName == "System.Runtime.CompilerServices.IsReadOnlyAttribute"),
            "InventorySlot must be a readonly struct.");
    }

    // GW-U-078
    [Fact]
    public void NoPropertyHasASetterAndEveryInstanceFieldIsReadonly()
    {
        var problems = new List<string>();
        foreach (Type type in PersistenceContract.ResolveAll())
        {
            foreach (PropertyInfo property in type.GetProperties(AllDeclared))
            {
                if (property.GetSetMethod(nonPublic: true) is not null)
                {
                    problems.Add($"{type.Name}.{property.Name} has a setter");
                }
            }

            foreach (FieldInfo field in type.GetFields(AllDeclared).Where(f => !f.IsStatic))
            {
                if (!field.IsInitOnly)
                {
                    problems.Add($"{type.Name}.{field.Name} is not readonly");
                }
            }

            if (type.GetMethods(AllDeclared).Any(m => m.Name == "<Clone>$"))
            {
                problems.Add($"{type.Name} declares <Clone>$");
            }
        }

        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
    }

    // GW-U-078
    [Fact]
    public void CategoryAssignmentHasNoPublicConstructor()
    {
        Type type = PersistenceContract.Resolve("CategoryAssignment");

        Assert.Empty(type.GetConstructors(BindingFlags.Public | BindingFlags.Instance));
    }

    // GW-U-078
    [Fact]
    public void ExistingSnapshotAndAbstractionsPinStillHolds()
    {
        _ = PersistenceContract.ResolveAll();

        _ = ContractTypes.SurfaceTypes();
    }

    private static void CheckCodec(string codecName, string valueName, NullabilityInfoContext context)
    {
        Type codec = PersistenceContract.Resolve(codecName);
        Type value = PersistenceContract.Resolve(valueName);

        MethodInfo encode = ContractTypes.Method(codec, "Encode", BindingFlags.Public | BindingFlags.Static, value);
        Assert.Equal(typeof(string), encode.ReturnType);
        Assert.Equal("value", encode.GetParameters()[0].Name);

        MethodInfo decode = ContractTypes.Method(codec, "Decode", BindingFlags.Public | BindingFlags.Static, typeof(string));
        Assert.Equal(value, decode.ReturnType);
        Assert.Equal(NullabilityState.Nullable, context.Create(decode.ReturnParameter).ReadState);
        Assert.Equal(NullabilityState.Nullable, context.Create(decode.GetParameters()[0]).ReadState);
    }
}
