namespace BaseStorage.Core.Patching;

/// <summary>One independently applicable patch (in the Plugin: one Harmony patch class).</summary>
public interface IPatchUnit
{
    /// <summary>Identifying name used in log messages. Plugin: the patch class's Type.FullName.</summary>
    string Name { get; }

    /// <summary>Applies the patch. May throw; PatchApplier catches every exception.</summary>
    void Apply();
}
