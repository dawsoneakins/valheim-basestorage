namespace BaseStorage.Core.Tests.StationPanel;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using BaseStorage.Core.StationPanel;

/// <summary>AC-4 (contract 1, 2, 3.1, 4.0): the exported surface of BaseStorage.Core.StationPanel is pinned by reflection.</summary>
public sealed class StationPanelSurfaceTests
{
    private const string StationPanelNamespace = "BaseStorage.Core.StationPanel";
    private const BindingFlags PublicInstanceDeclared = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
    private const BindingFlags PublicStaticDeclared = BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly;

    // GW-U-547 (AC-4, contract 1): exactly the seven contract names, sorted by ordinal.
    [Fact]
    public void ExportedTypesAreExactlyTheSevenContractNames()
    {
        string[] names = typeof(PanelRect).Assembly.GetExportedTypes()
            .Where(type => type.Namespace == StationPanelNamespace)
            .Select(type => type.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            new[]
            {
                "PanelRect",
                "StationPanelDisabledReason",
                "StationPanelHiddenReason",
                "StationPanelInput",
                "StationPanelLayout",
                "StationPanelPlacement",
                "StationPanelState",
            },
            names);
    }

    // GW-U-547 (AC-4, contract 2, 3.1, 4.0): the public members of each type match the contract lists by name and parameter types.
    [Fact]
    public void PanelRectMembersMatchTheContract()
    {
        Assert.Equal(new[] { "Height", "Right", "Top", "Width", "X", "Y" }, Names(typeof(PanelRect).GetProperties(PublicInstanceDeclared)));
        Assert.Equal(new[] { "Equals", "GetHashCode", "Inflate", "Overlaps" }, Names(typeof(PanelRect).GetMethods(PublicInstanceDeclared)));
        ConstructorInfo constructor = Assert.Single(typeof(PanelRect).GetConstructors());
        Assert.Equal(new[] { typeof(float), typeof(float), typeof(float), typeof(float) }, constructor.GetParameters().Select(p => p.ParameterType).ToArray());
        Assert.Equal(typeof(bool), typeof(PanelRect).GetMethod("Overlaps", new[] { typeof(PanelRect) })!.ReturnType);
        Assert.Equal(typeof(PanelRect), typeof(PanelRect).GetMethod("Inflate", new[] { typeof(float) })!.ReturnType);
    }

    [Fact]
    public void StationPanelInputMembersMatchTheContract()
    {
        Assert.Equal(
            new[]
            {
                "CraftButton", "EligibleChestCount", "FillPercent", "OrganizeHolderName", "OrganizeRunning", "RecipeList", "Requirements",
                "ScreenHeight", "ScreenWidth", "StationButtons", "StationCount", "UiScale",
            },
            Names(typeof(StationPanelInput).GetProperties(PublicInstanceDeclared)));
        Assert.Equal(new[] { "Equals", "GetHashCode", "op_Equality", "op_Inequality" }, Names(typeof(StationPanelInput).GetMethods(PublicInstanceDeclared | PublicStaticDeclared)));
        ConstructorInfo constructor = Assert.Single(typeof(StationPanelInput).GetConstructors());
        Assert.Equal(
            new[] { "screenWidth", "screenHeight", "uiScale", "recipeList", "requirements", "craftButton", "stationCount", "eligibleChestCount", "fillPercent", "organizeHolderName", "stationButtons" },
            constructor.GetParameters().Select(p => p.Name).ToArray());
    }

    [Fact]
    public void StationPanelStateMembersMatchTheContract()
    {
        Assert.Equal(
            new[]
            {
                "ButtonsEnabled", "CanvasHeight", "CanvasWidth", "DepositButton", "DisabledReason", "HiddenReason", "OrganizeButton",
                "Placement", "StatusLine", "StatusLineText", "TooltipKey", "TooltipText", "Visible",
            },
            Names(typeof(StationPanelState).GetProperties(PublicInstanceDeclared)));
        Assert.Equal(new[] { "Equals", "GetHashCode", "op_Equality", "op_Inequality" }, Names(typeof(StationPanelState).GetMethods(PublicInstanceDeclared | PublicStaticDeclared)));
        Assert.Equal(new[] { "Compute" }, Names(typeof(StationPanelLayout).GetMethods(PublicStaticDeclared)));
        Assert.Equal(typeof(StationPanelState), typeof(StationPanelLayout).GetMethod("Compute")!.ReturnType);
    }

    // GW-U-547 (AC-4, contract 4.0): StationPanelState has no public constructor, and StationPanelLayout is static.
    [Fact]
    public void StateHasNoPublicConstructorAndLayoutIsStatic()
    {
        Assert.Empty(typeof(StationPanelState).GetConstructors(BindingFlags.Public | BindingFlags.Instance));
        Assert.True(typeof(StationPanelLayout).IsAbstract && typeof(StationPanelLayout).IsSealed, "StationPanelLayout must be a static class.");
    }

    // GW-U-547 (AC-4, contract 2, 3.1, 4.0): PanelRect is a readonly struct, the input and the state are sealed and implement IEquatable<T>.
    [Fact]
    public void TypesHaveTheContractedModifiersAndEquatableInterfaces()
    {
        Assert.True(typeof(PanelRect).IsValueType, "PanelRect must be a struct.");
        Assert.True(typeof(PanelRect).CustomAttributes.Any(attribute => attribute.AttributeType.FullName == "System.Runtime.CompilerServices.IsReadOnlyAttribute"), "PanelRect must be readonly.");
        Assert.True(typeof(StationPanelInput).IsSealed);
        Assert.True(typeof(StationPanelState).IsSealed);
        Assert.Contains(typeof(IEquatable<PanelRect>), typeof(PanelRect).GetInterfaces());
        Assert.Contains(typeof(IEquatable<StationPanelInput>), typeof(StationPanelInput).GetInterfaces());
        Assert.Contains(typeof(IEquatable<StationPanelState>), typeof(StationPanelState).GetInterfaces());
    }

    // GW-U-547 (AC-4, R4): every public property is get-only.
    [Fact]
    public void EveryPublicPropertyIsGetOnly()
    {
        Type[] types = { typeof(PanelRect), typeof(StationPanelInput), typeof(StationPanelState) };
        foreach (Type type in types)
        {
            foreach (PropertyInfo property in type.GetProperties(PublicInstanceDeclared))
            {
                Assert.False(property.CanWrite, type.Name + "." + property.Name + " must be get-only.");
            }
        }
    }

    // GW-U-547 (AC-4, contract 4.0): the enums are int-based with the contracted names and values.
    [Fact]
    public void EnumNamesAndValuesMatchTheContract()
    {
        AssertEnum<StationPanelHiddenReason>(
            ("None", 0), ("StationButtonsOff", 1), ("NotInBase", 2), ("NoRoom", 3));
        AssertEnum<StationPanelDisabledReason>(
            ("None", 0), ("OrganizeInProgress", 1), ("NoEligibleChests", 2));
        AssertEnum<StationPanelPlacement>(
            ("None", 0), ("RightOfCraftButton", 1), ("BelowCraftButton", 2), ("LeftOfCraftButton", 3), ("AboveControls", 4), ("RightOfControls", 5), ("BelowCraftButtonRightAligned", 6));
    }

    // GW-U-547 (AC-4, contract 4.0): the eight layout constants have their contracted values.
    [Fact]
    public void LayoutConstantsHaveTheContractedValues()
    {
        var constants = typeof(StationPanelLayout)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(field => field.IsLiteral)
            .ToDictionary(field => field.Name, field => (float)field.GetRawConstantValue()!, StringComparer.Ordinal);

        Assert.Equal(8, constants.Count);
        Assert.Equal(1920f, constants["ReferenceWidth"]);
        Assert.Equal(1080f, constants["ReferenceHeight"]);
        Assert.Equal(8f, constants["ButtonGap"]);
        Assert.Equal(8f, constants["VanillaGap"]);
        Assert.Equal(16f, constants["StatusLineGap"]);
        Assert.Equal(24f, constants["StatusLineHeight"]);
        Assert.Equal(120f, constants["MinButtonWidth"]);
        Assert.Equal(32f, constants["MinButtonHeight"]);
    }

    // GW-U-547 (AC-4, R9 and contract 3.1, 4.0): the nullability of the string members, as NullabilityInfoContext reports it.
    [Fact]
    public void NullableStringMembersAreMarkedNullable()
    {
        var context = new NullabilityInfoContext();
        Assert.Equal(NullabilityState.Nullable, context.Create(typeof(StationPanelInput).GetProperty("OrganizeHolderName")!).ReadState);
        Assert.Equal(NullabilityState.Nullable, context.Create(typeof(StationPanelState).GetProperty("TooltipKey")!).ReadState);
        Assert.Equal(NullabilityState.Nullable, context.Create(typeof(StationPanelState).GetProperty("TooltipText")!).ReadState);
        Assert.Equal(NullabilityState.Nullable, context.Create(typeof(StationPanelState).GetProperty("StatusLineText")!).ReadState);

        ParameterInfo holder = typeof(StationPanelInput).GetConstructors()[0].GetParameters().Single(p => p.Name == "organizeHolderName");
        Assert.Equal(NullabilityState.Nullable, context.Create(holder).ReadState);
    }

    private static void AssertEnum<TEnum>(params (string Name, int Value)[] expected)
        where TEnum : struct, Enum
    {
        Assert.Equal(typeof(int), Enum.GetUnderlyingType(typeof(TEnum)));
        string[] names = Enum.GetNames(typeof(TEnum));
        Assert.Equal(expected.Select(pair => pair.Name).OrderBy(name => name, StringComparer.Ordinal), names.OrderBy(name => name, StringComparer.Ordinal));
        foreach (var (name, value) in expected)
        {
            Assert.Equal(value, Convert.ToInt32(Enum.Parse<TEnum>(name)));
        }
    }

    private static string[] Names(IEnumerable<MemberInfo> members)
    {
        return members
            .Select(member => member.Name)
            .Where(name => !name.StartsWith("get_", StringComparison.Ordinal) && !name.StartsWith("set_", StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
    }
}
