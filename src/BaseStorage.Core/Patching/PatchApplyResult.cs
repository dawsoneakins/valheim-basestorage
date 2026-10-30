namespace BaseStorage.Core.Patching;

using System;

/// <summary>Outcome of applying one patch.</summary>
public sealed class PatchApplyResult
{
    /// <summary>Creates a result. Instances are created only by PatchApplier.</summary>
    /// <param name="name">The display name used in the log.</param>
    /// <param name="index">Zero-based position in the input list.</param>
    /// <param name="succeeded">True when Apply() returned normally.</param>
    /// <param name="error">The caught exception, or null when succeeded.</param>
    internal PatchApplyResult(string name, int index, bool succeeded, Exception? error)
    {
        Name = name;
        Index = index;
        Succeeded = succeeded;
        Error = error;
    }

    /// <summary>The display name used in the log (see contract 1.3).</summary>
    public string Name { get; }

    /// <summary>Zero-based position in the input list.</summary>
    public int Index { get; }

    /// <summary>True when Apply() returned normally.</summary>
    public bool Succeeded { get; }

    /// <summary>The caught exception; null exactly when Succeeded is true.</summary>
    public Exception? Error { get; }
}
