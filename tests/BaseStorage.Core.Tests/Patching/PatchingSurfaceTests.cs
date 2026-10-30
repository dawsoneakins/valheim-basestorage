namespace BaseStorage.Core.Tests.Patching;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Patching;
using BaseStorage.Core.Tests.Snapshots;

/// <summary>
/// AC-3: the public surface of BaseStorage.Core.Patching is exactly contracts/plugin-boot.md section 1.1 (reflection, ADR-GWIN-013).
/// The existing ContractTypes.SurfaceTypes() checks stay unedited and must stay green.
/// </summary>
public sealed class PatchingSurfaceTests
{
    private const string PatchingNamespace = "BaseStorage.Core.Patching";
    private const BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
    private const BindingFlags AllInstanceFields = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
    private const BindingFlags PublicStatic = BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly;

    // GW-U-079
    [Fact]
    public void ExportedTypesAreExactlyTheFiveContractTypes()
    {
        string[] expected = { "BootMessages", "IPatchUnit", "PatchApplier", "PatchApplyReport", "PatchApplyResult" };

        string[] actual = ContractTypes.CoreAssembly().GetExportedTypes()
            .Where(t => t.Namespace == PatchingNamespace)
            .Select(t => t.Name)
            .ToArray();

        Assert.Equal(Sorted(expected), Sorted(actual));
    }

    // GW-U-079
    [Fact]
    public void IPatchUnitDeclaresExactlyNameAndApply()
    {
        Type type = Patching("IPatchUnit");

        Assert.True(type.IsInterface);
        Assert.Equal(new[] { "Apply", "get_Name" }, Sorted(type.GetMethods(BindingFlags.Public | BindingFlags.Instance).Select(m => m.Name).ToArray()));
        PropertyInfo name = Assert.Single(type.GetProperties(BindingFlags.Public | BindingFlags.Instance));
        Assert.Equal("Name", name.Name);
        Assert.Null(name.SetMethod);
        Assert.Equal(typeof(string), name.PropertyType);
        MethodInfo apply = type.GetMethod("Apply", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null)!;
        Assert.NotNull(apply);
        Assert.Equal(typeof(void), apply.ReturnType);
    }

    // GW-U-079
    [Theory]
    [InlineData("PatchApplyResult")]
    [InlineData("PatchApplyReport")]
    public void ResultTypesAreSealedWithoutPublicConstructorsAndReadOnly(string simpleName)
    {
        Type type = Patching(simpleName);

        Assert.True(type.IsSealed);
        Assert.Empty(type.GetConstructors(BindingFlags.Public | BindingFlags.Instance));

        foreach (PropertyInfo property in type.GetProperties(PublicInstance))
        {
            Assert.True(property.CanRead, $"{simpleName}.{property.Name} must be readable.");
            Assert.Null(property.SetMethod);
        }

        foreach (FieldInfo field in type.GetFields(AllInstanceFields))
        {
            Assert.True(field.IsInitOnly, $"{simpleName}.{field.Name} must be readonly.");
        }
    }

    // GW-U-079
    [Fact]
    public void ErrorPropertyIsDeclaredNullableException()
    {
        PropertyInfo error = Patching("PatchApplyResult").GetProperty("Error", BindingFlags.Public | BindingFlags.Instance)!;

        Assert.NotNull(error);
        Assert.Equal(typeof(Exception), error.PropertyType);
        NullabilityInfo info = new NullabilityInfoContext().Create(error);
        Assert.Equal(NullabilityState.Nullable, info.ReadState);
    }

    // GW-U-079
    [Fact]
    public void ApplierAndMessagesAreStaticWithTheContractSignatures()
    {
        Type applier = Patching("PatchApplier");
        Type messages = Patching("BootMessages");
        Assert.True(applier.IsAbstract && applier.IsSealed);
        Assert.True(messages.IsAbstract && messages.IsSealed);

        MethodInfo applyAll = applier.GetMethod("ApplyAll", PublicStatic, null, new[] { typeof(IReadOnlyList<IPatchUnit>), typeof(ILogSink) }, null)!;
        Assert.NotNull(applyAll);
        Assert.Equal(Patching("PatchApplyReport"), applyAll.ReturnType);

        Assert.Equal(typeof(string), messages.GetMethod("Loaded", PublicStatic, null, new[] { typeof(string) }, null)!.ReturnType);
        Assert.Equal(typeof(string), messages.GetMethod("PatchApplied", PublicStatic, null, new[] { typeof(string) }, null)!.ReturnType);
        Assert.Equal(typeof(string), messages.GetMethod("PatchFailed", PublicStatic, null, new[] { typeof(string), typeof(Exception) }, null)!.ReturnType);
        Assert.Equal(typeof(string), messages.GetMethod("LoadFailed", PublicStatic, null, new[] { typeof(string), typeof(Exception) }, null)!.ReturnType);
    }

    // GW-U-079
    [Fact]
    public void ResultsIsAReadOnlyCollectionWhoseWritesThrow()
    {
        var sink = new RecordingLogSink();
        PatchApplyReport report = PatchApplier.ApplyAll(new IPatchUnit[] { new FakePatch("A.B") }, sink);
        PatchApplyResult first = report.Results[0];

        Assert.IsType<ReadOnlyCollection<PatchApplyResult>>(report.Results);
        var writable = (IList<PatchApplyResult>)report.Results;
        Assert.Throws<NotSupportedException>(() => writable.Add(first));
        Assert.Throws<NotSupportedException>(() => writable.Clear());
        Assert.Throws<NotSupportedException>(() => writable[0] = first);
    }

    private static Type Patching(string simpleName) =>
        ContractTypes.Require(PatchingNamespace + "." + simpleName);

    private static string[] Sorted(string[] values) =>
        values.OrderBy(v => v, StringComparer.Ordinal).ToArray();
}
