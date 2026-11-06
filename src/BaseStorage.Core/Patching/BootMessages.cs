namespace BaseStorage.Core.Patching;

using System;

/// <summary>Exact text of the boot log lines. Pure functions, no I/O, InvariantCulture.</summary>
public static class BootMessages
{
    /// <summary>Builds the load line in the form "BaseStorage {version} loaded".</summary>
    /// <param name="version">The plugin version, used verbatim.</param>
    /// <returns>The load line without prefix, period or newline.</returns>
    public static string Loaded(string version)
    {
        CheckVersion(version);
        return "Dons-QOL " + version + " loaded";
    }

    /// <summary>Builds the Debug line for a patch that applied.</summary>
    /// <param name="patchName">The patch name, shaped to a single line.</param>
    /// <returns>The applied line.</returns>
    public static string PatchApplied(string patchName)
    {
        if (patchName is null)
        {
            throw new ArgumentNullException(nameof(patchName));
        }

        return "Patch '" + SingleLineText.Of(patchName) + "' applied";
    }

    /// <summary>Builds the Error line for a patch that failed to apply.</summary>
    /// <param name="patchName">The patch name, shaped to a single line.</param>
    /// <param name="error">The exception that was caught.</param>
    /// <returns>The failed-to-apply line.</returns>
    public static string PatchFailed(string patchName, Exception error)
    {
        if (patchName is null)
        {
            throw new ArgumentNullException(nameof(patchName));
        }

        if (error is null)
        {
            throw new ArgumentNullException(nameof(error));
        }

        return "Patch '" + SingleLineText.Of(patchName) + "' failed to apply: " + SingleLineText.DescribeWithInner(error);
    }

    /// <summary>Builds the Error line for a boot that failed outside the per-patch loop.</summary>
    /// <param name="version">The plugin version, used verbatim.</param>
    /// <param name="error">The exception that was caught.</param>
    /// <returns>The failed-to-load line.</returns>
    public static string LoadFailed(string version, Exception error)
    {
        CheckVersion(version);
        if (error is null)
        {
            throw new ArgumentNullException(nameof(error));
        }

        return "Dons-QOL " + version + " failed to load: " + SingleLineText.DescribeWithInner(error);
    }

    private static void CheckVersion(string version)
    {
        if (version is null)
        {
            throw new ArgumentNullException(nameof(version));
        }

        if (string.IsNullOrWhiteSpace(version))
        {
            throw new ArgumentException("The version must not be empty or whitespace.", nameof(version));
        }
    }
}
