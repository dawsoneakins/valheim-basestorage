namespace BaseStorage.Plugin.Patching;

using System;
using System.Threading;

/// <summary>
/// The one CraftFromBase check of every E3 patch (contract craft-from-base-toggle 3). Open when the setting is on, or while a
/// craft or build that already started is in flight. Uses only BCL types, so the same file is compiled into the tests.
/// </summary>
internal static class CraftFromBaseGate
{
    private static Func<bool>? settingOnRead;
    private static Func<bool>? inFlightRead;

    /// <summary>Wires the two reads. Null for either detaches it (tests); a detached read counts as false.</summary>
    /// <param name="settingOn">Reads the effective CraftFromBase value; called on every <see cref="IsOpen"/>.</param>
    /// <param name="inFlight">Tells whether a craft or build session is past Idle.</param>
    internal static void Initialize(Func<bool>? settingOn, Func<bool>? inFlight)
    {
        Volatile.Write(ref settingOnRead, settingOn);
        Volatile.Write(ref inFlightRead, inFlight);
    }

    /// <summary>Tells whether an E3 patch may run its body. Never throws, never allocates, never logs.</summary>
    /// <returns>True when the setting is on or an operation is in flight; otherwise false.</returns>
    internal static bool IsOpen()
    {
        return Read(Volatile.Read(ref settingOnRead)) || Read(Volatile.Read(ref inFlightRead));
    }

    private static bool Read(Func<bool>? probe)
    {
        if (probe == null)
        {
            return false;
        }

        try
        {
            return probe();
        }
        catch (Exception)
        {
            return false;
        }
    }
}
