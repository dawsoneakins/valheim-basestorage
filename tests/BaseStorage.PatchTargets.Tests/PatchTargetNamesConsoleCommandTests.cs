namespace BaseStorage.PatchTargets.Tests;

using System;
using System.Reflection;
using BaseStorage.Shared;

/// <summary>
/// GW-U-2330 (AC-4; organize-console-command contract 6.1): every Terminal, ConsoleCommand, ConsoleEvent and ConsoleEventArgs constant holds
/// its verbatim value, and HarmonyPatched.TerminalInitTerminal is the InitTerminal postfix id. No ValheimDir is needed. Constants are read
/// by reflection so that a missing constant fails its own row.
/// </summary>
public sealed class PatchTargetNamesConsoleCommandTests
{
    // GW-U-2330 (6.1): every owner and constant of the console command, verbatim.
    [Theory]
    [InlineData("Terminal", "TypeName", "Terminal")]
    [InlineData("Terminal", "InitTerminal", "InitTerminal")]
    [InlineData("Terminal", "InitTerminalParametersNone", "")]
    [InlineData("Terminal", "AddString", "AddString")]
    [InlineData("Terminal", "AddStringParametersText", "System.String")]
    [InlineData("ConsoleCommand", "TypeName", "Terminal+ConsoleCommand")]
    [InlineData("ConsoleEvent", "TypeName", "Terminal+ConsoleEvent")]
    [InlineData("ConsoleEventArgs", "TypeName", "Terminal+ConsoleEventArgs")]
    [InlineData("ConsoleEventArgs", "FieldArgs", "Args")]
    [InlineData("ConsoleEventArgs", "FieldArgsType", "System.String[]")]
    [InlineData("ConsoleEventArgs", "FieldContext", "Context")]
    [InlineData("ConsoleEventArgs", "FieldContextType", "Terminal")]
    public void TheConsoleCommandConstantsHoldTheContractedValues(string owner, string name, string expected)
    {
        Assert.Equal(expected, Constant(owner, name));
    }

    // GW-U-2330 (6.1): the postfix id of the console command registration is listed in HarmonyPatched.
    [Fact]
    public void TheHarmonyPatchedIdOfTheRegistrationIsTheInitTerminalComposition()
    {
        Assert.Equal("Terminal.InitTerminal()", HarmonyPatched("TerminalInitTerminal"));
    }

    private static string? HarmonyPatched(string name)
    {
        Type? holder = typeof(PatchTargetNames).GetNestedType("HarmonyPatched", BindingFlags.Public | BindingFlags.NonPublic);
        Assert.True(holder is not null, "PatchTargetNames has no nested class HarmonyPatched.");

        FieldInfo? field = holder!.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        Assert.True(field is not null, $"PatchTargetNames.HarmonyPatched.{name} is missing.");

        return (string?)field!.GetValue(null);
    }

    private static string? Constant(string owner, string name)
    {
        Type? holder = typeof(PatchTargetNames).GetNestedType(owner, BindingFlags.Public | BindingFlags.NonPublic);
        Assert.True(holder is not null, $"PatchTargetNames has no owner class {owner}.");

        FieldInfo? field = holder!.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        Assert.True(field is not null, $"PatchTargetNames.{owner}.{name} is missing.");

        return (string?)field!.GetValue(null);
    }
}
