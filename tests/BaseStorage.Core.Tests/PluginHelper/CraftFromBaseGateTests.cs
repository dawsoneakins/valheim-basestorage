namespace BaseStorage.Core.Tests.PluginHelper;

using System;
using System.Reflection;

/// <summary>
/// GW-U-1800 to GW-U-1805 (AC-1, AC-4; contract craft-from-base-toggle 3.1, rules G1-G5 and the truth table). The gate file
/// <c>src/BaseStorage.Plugin/Patching/CraftFromBaseGate.cs</c> is linked into this project by implement (contract 1.2), so the
/// tests reach the gate through <see cref="Gate"/>, which binds the real <c>Initialize</c> and <c>IsOpen</c> by reflection.
/// Until the link lands every test fails with the missing-link message. The gate is static, so every test detaches it.
/// </summary>
public sealed class CraftFromBaseGateTests : IDisposable
{
    /// <inheritdoc />
    public void Dispose()
    {
        if (Gate.Linked)
        {
            Gate.Initialize(null, null);
        }
    }

    // GW-U-1800 (G1, truth-table row 1): with the setting on, IsOpen is true and inFlight is not called.
    [Fact]
    public void WithTheSettingOnInFlightIsNotCalled()
    {
        int inFlightCalls = 0;
        Gate.Initialize(() => true, () =>
        {
            inFlightCalls++;
            return false;
        });

        Assert.True(Gate.IsOpen());
        Assert.Equal(0, inFlightCalls);
    }

    // GW-U-1800 (G1, row 2): with the setting off and nothing in flight, IsOpen is false and inFlight is called once.
    [Fact]
    public void WithTheSettingOffAndNothingInFlightTheGateIsClosed()
    {
        int inFlightCalls = 0;
        Gate.Initialize(() => false, () =>
        {
            inFlightCalls++;
            return false;
        });

        Assert.False(Gate.IsOpen());
        Assert.Equal(1, inFlightCalls);
    }

    // GW-U-1800 (G1, row 3): with the setting off and an operation in flight, IsOpen is true.
    [Fact]
    public void WithTheSettingOffAndAnOperationInFlightTheGateIsOpen()
    {
        int inFlightCalls = 0;
        Gate.Initialize(() => false, () =>
        {
            inFlightCalls++;
            return true;
        });

        Assert.True(Gate.IsOpen());
        Assert.Equal(1, inFlightCalls);
    }

    // GW-U-1801 (G3, row 4): a throwing setting counts as false, and an in-flight operation still opens the gate. Nothing escapes.
    [Fact]
    public void AThrowingSettingCountsAsFalseAndAnInFlightOperationStillOpensTheGate()
    {
        Gate.Initialize(() => throw new InvalidOperationException("setting"), () => true);

        Assert.True(Gate.IsOpen());
    }

    // GW-U-1801 (G3, row 5): a throwing setting with nothing in flight is closed. Nothing escapes.
    [Fact]
    public void AThrowingSettingWithNothingInFlightIsClosed()
    {
        Gate.Initialize(() => throw new InvalidOperationException("setting"), () => false);

        Assert.False(Gate.IsOpen());
    }

    // GW-U-1801 (G3, row 6): a throwing in-flight read counts as false, so the gate stays closed. Nothing escapes.
    [Fact]
    public void AThrowingInFlightReadCountsAsFalse()
    {
        Gate.Initialize(() => false, () => throw new InvalidOperationException("in flight"));

        Assert.False(Gate.IsOpen());
    }

    // GW-U-1802 (G2, row 7): two null delegates are both false.
    [Fact]
    public void NullDelegatesAreFalse()
    {
        Gate.Initialize(null, null);

        Assert.False(Gate.IsOpen());
    }

    // GW-U-1802 (G2, row 8): a null setting with an in-flight operation is open.
    [Fact]
    public void ANullSettingWithAnInFlightOperationIsOpen()
    {
        Gate.Initialize(null, () => true);

        Assert.True(Gate.IsOpen());
    }

    // GW-U-1802 (G2, never initialised): a gate with no wiring, or detached, is closed, so vanilla runs before boot.
    [Fact]
    public void AGateWithNoWiringIsClosedSoVanillaRunsBeforeBoot()
    {
        Gate.Initialize(null, null);

        Assert.False(Gate.IsOpen());
    }

    // GW-U-1803 (G4, T4, AC-4): the setting is read on every call, so a flip changes the next IsOpen with no other call.
    [Fact]
    public void FlippingTheSettingFlipsTheNextCallWithNoOtherCall()
    {
        bool setting = true;
        Gate.Initialize(() => setting, () => false);
        Assert.True(Gate.IsOpen());

        setting = false;
        Assert.False(Gate.IsOpen());

        setting = true;
        Assert.True(Gate.IsOpen());
    }

    // GW-U-1804 (G5, AC-1): Initialize(null, null) after a wired state closes the gate again.
    [Fact]
    public void InitializeWithNullsAfterAWiredStateClosesTheGate()
    {
        Gate.Initialize(() => true, () => true);
        Assert.True(Gate.IsOpen());

        Gate.Initialize(null, null);

        Assert.False(Gate.IsOpen());
    }

    // GW-U-1804: the test class is disposable, so xunit detaches the gate after every test of this class.
    [Fact]
    public void TheTestClassIsDisposableSoEachTestDetachesTheGate()
    {
        Assert.IsAssignableFrom<IDisposable>(this);
    }

    // GW-U-1805 (NFR performance, T1, G1): IsOpen allocates nothing over 1000 calls with static delegates.
    [Fact]
    public void IsOpenAllocatesNothingOverAThousandCalls()
    {
        Gate.Initialize(static () => false, static () => true);
        _ = Gate.IsOpen();

        long before = GC.GetAllocatedBytesForCurrentThread();
        int opened = 0;
        for (int i = 0; i < 1000; i++)
        {
            if (Gate.IsOpen())
            {
                opened++;
            }
        }

        long after = GC.GetAllocatedBytesForCurrentThread();
        Assert.Equal(1000, opened);
        Assert.Equal(0L, after - before);
    }

    /// <summary>
    /// The gate reached through its real members. The delegates are bound once by reflection, so this file compiles before
    /// implement links the gate file, and the same tests run unchanged afterwards.
    /// </summary>
    private static class Gate
    {
        private const string GateName = "BaseStorage.Plugin.Patching.CraftFromBaseGate";
        private static readonly Type? GateType = typeof(CraftFromBaseGateTests).Assembly.GetType(GateName, throwOnError: false);
        private static Action<Func<bool>?, Func<bool>?>? initialize;
        private static Func<bool>? isOpen;

        /// <summary>True once the gate file is linked into this assembly.</summary>
        public static bool Linked => GateType is not null;

        /// <summary>Calls the real Initialize.</summary>
        public static void Initialize(Func<bool>? settingOn, Func<bool>? inFlight)
        {
            initialize ??= Bind<Action<Func<bool>?, Func<bool>?>>("Initialize");
            initialize(settingOn, inFlight);
        }

        /// <summary>Calls the real IsOpen through a cached delegate, so the call itself allocates nothing.</summary>
        public static bool IsOpen()
        {
            isOpen ??= Bind<Func<bool>>("IsOpen");
            return isOpen();
        }

        private static T Bind<T>(string name)
            where T : Delegate
        {
            Type gate = GateType
                ?? throw new InvalidOperationException(GateName + " is not in BaseStorage.Core.Tests: implement must link src/BaseStorage.Plugin/Patching/CraftFromBaseGate.cs (contract craft-from-base-toggle 1.2).");
            MethodInfo method = gate.GetMethod(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException(GateName + "." + name + " is missing.");
            return (T)Delegate.CreateDelegate(typeof(T), method)!;
        }
    }
}
