namespace BaseStorage.Core.Snapshots;

using System;

internal static class SnapshotGuard
{
    // Throws unless the value is finite; returns it with -0f normalised to 0f.
    internal static float Finite(float value, string name)
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
        {
            throw new ArgumentOutOfRangeException(name, "Must be a finite number.");
        }

        return value == 0f ? 0f : value;
    }
}
