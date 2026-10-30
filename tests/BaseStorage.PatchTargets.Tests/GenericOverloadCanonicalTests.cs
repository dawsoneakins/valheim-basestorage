namespace BaseStorage.PatchTargets.Tests;

using System;
using System.Collections.Generic;
using BaseStorage.PatchTargets.Tests.Checking;

/// <summary>
/// S-E006-002 rule PT2 (contract version-rpc-contracts.md 6.2, GW-C-1263): an overload companion renders parameter types
/// canonically, so a generic method's delegate parameter appears as the definition with its arguments in brackets and a method
/// generic parameter by name. Non-generic parameters keep their FullName, so every earlier row is unchanged. Runtime types only.
/// </summary>
public sealed class GenericOverloadCanonicalTests
{
    private const string DllPath = @"X:\fixture.dll";

    // GW-C-1263: the canonical companion of Register<T>(string, Action<object, T>) is accepted among five overloads.
    [Fact]
    public void AGenericDelegateCompanionIsAcceptedByItsCanonicalName()
    {
        IReadOnlyList<TargetEntry> entries = Entries("System.String,System.Action`2[System.Object,T]");

        Assert.Null(GameTargetChecker.Check(entries[2], entries, FindHolder, DllPath));
        Assert.Null(GameTargetChecker.Check(entries[1], entries, FindHolder, DllPath));
    }

    // GW-C-1263: the arity-two overload has its own canonical name, so the two generic overloads never collide.
    [Fact]
    public void TheArityTwoOverloadHasItsOwnCanonicalName()
    {
        IReadOnlyList<TargetEntry> entries = Entries("System.String,System.Action`3[System.Object,T,U]");

        Assert.Null(GameTargetChecker.Check(entries[2], entries, FindHolder, DllPath));
    }

    // GW-C-1263: the old FullName rendering (an empty second parameter) is not accepted, and the message lists the canonical overloads.
    [Fact]
    public void TheFullNameRenderingIsRejectedWithTheCanonicalOverloadsListed()
    {
        IReadOnlyList<TargetEntry> entries = Entries("System.String,");

        string? failure = GameTargetChecker.Check(entries[2], entries, FindHolder, DllPath);

        Assert.NotNull(failure);
        Assert.StartsWith("BST2012: Patch target overload '" + typeof(RpcHolder).FullName + ".Register(System.String,)' (PatchTargetNames.Holder.RegisterParametersHandler) not found in '" + DllPath + "'.", failure);
        Assert.Contains("Register(System.String,System.Action`2[System.Object,T])", failure, StringComparison.Ordinal);
        Assert.Contains("Register(System.String,System.Action`3[System.Object,T,U])", failure, StringComparison.Ordinal);
        Assert.Contains("Register(System.String,System.Action`1[System.Object])", failure, StringComparison.Ordinal);
    }

    // GW-C-1263: a non-generic companion still matches by FullName (PT1 unchanged), including an array parameter.
    [Fact]
    public void NonGenericCompanionsStillMatchByFullName()
    {
        IReadOnlyList<TargetEntry> entries =
        [
            new TargetEntry("Holder", "TypeName", TargetKind.TypeEntry, typeof(RpcHolder).FullName!, null),
            new TargetEntry("Holder", "Write", TargetKind.MethodOverloads, "Write", null),
            new TargetEntry("Holder", "WriteParametersBytes", TargetKind.OverloadCompanion, "System.Byte[]", "Write"),
        ];

        Assert.Null(GameTargetChecker.Check(entries[1], entries, FindHolder, DllPath));
    }

    private static IReadOnlyList<TargetEntry> Entries(string companion) =>
    [
        new TargetEntry("Holder", "TypeName", TargetKind.TypeEntry, typeof(RpcHolder).FullName!, null),
        new TargetEntry("Holder", "Register", TargetKind.MethodOverloads, "Register", null),
        new TargetEntry("Holder", "RegisterParametersHandler", TargetKind.OverloadCompanion, companion, "Register"),
    ];

    private static Type? FindHolder(string name) => name == typeof(RpcHolder).FullName ? typeof(RpcHolder) : null;

    /// <summary>A fixture shaped like ZRpc: one non-generic Register and two generic ones, plus two Write overloads.</summary>
    private sealed class RpcHolder
    {
        public void Register(string name, Action<object> handler)
        {
        }

        public void Register<T>(string name, Action<object, T> handler)
        {
        }

        public void Register<T, U>(string name, Action<object, T, U> handler)
        {
        }

        public void Write(byte[] bytes)
        {
        }

        public void Write(int value)
        {
        }
    }
}
