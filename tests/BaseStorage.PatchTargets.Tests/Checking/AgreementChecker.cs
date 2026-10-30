namespace BaseStorage.PatchTargets.Tests.Checking;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// The agreement rule of contract 4.7.3. Returns an empty string when the check passes, otherwise the BST2020, BST2021
/// and BST2022 lines, sorted ordinally and joined with <c>\n</c>. Any BST2022 line fails the check.
/// </summary>
internal static class AgreementChecker
{
    public static string Compare(
        IReadOnlyList<PatchClass> patchClasses,
        IReadOnlyList<HarmonyEntry> harmonyPatched,
        string pluginDll)
    {
        HashSet<string> listed = harmonyPatched.Select(e => e.Value).ToHashSet(StringComparer.Ordinal);
        HashSet<string> patched = patchClasses.SelectMany(c => c.TargetIds).ToHashSet(StringComparer.Ordinal);
        List<string> lines = [];

        foreach (PatchClass patch in patchClasses)
        {
            foreach (string id in patch.TargetIds.Distinct(StringComparer.Ordinal).Where(id => !listed.Contains(id)))
            {
                lines.Add("BST2020: Plugin patch class '" + patch.ClassFullName + "' targets '" + id + "', which is not listed in PatchTargetNames.HarmonyPatched.");
            }

            lines.AddRange(patch.Diagnostics);
        }

        foreach (HarmonyEntry entry in harmonyPatched.Where(e => !patched.Contains(e.Value)))
        {
            lines.Add("BST2021: PatchTargetNames.HarmonyPatched." + entry.Const + " = '" + entry.Value + "' has no matching Harmony patch in '" + pluginDll + "'.");
        }

        return string.Join("\n", lines.Distinct(StringComparer.Ordinal).OrderBy(l => l, StringComparer.Ordinal));
    }
}
