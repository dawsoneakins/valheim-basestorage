namespace BaseStorage.PatchTargets.Tests.Checking;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

/// <summary>
/// The nine shape rules of contract 4.5.3. Each violation is one BST2015 line, sorted ordinally.
/// </summary>
internal static class ShapeRules
{
    private const string Composition = "value is not a TypeName.Method(Parameters) composition of declared constants";

    public static IReadOnlyList<string> Check(Type listType)
    {
        List<string> lines = [];
        IReadOnlyList<TargetEntry> entries = TargetList.Classify(listType);

        foreach (Type owner in TargetList.OwnerClasses(listType))
        {
            List<TargetEntry> own = entries.Where(e => e.Owner == owner.Name).ToList();

            if (!own.Any(e => e.Kind == TargetKind.TypeEntry))
            {
                lines.Add(Line(owner.Name, "*", "owner class has no TypeName"));
            }

            foreach (TargetEntry entry in own)
            {
                if (entry.Value.Length == 0 && entry.Kind != TargetKind.OverloadCompanion)
                {
                    lines.Add(Line(owner.Name, entry.Const, "value is empty"));
                }

                if (entry.Value.Any(char.IsWhiteSpace))
                {
                    lines.Add(Line(owner.Name, entry.Const, "value contains whitespace"));
                }

                if (entry.Kind == TargetKind.FieldEntry
                    && !own.Any(e => e.Kind == TargetKind.FieldTypeCompanion && e.BelongsTo == entry.Const))
                {
                    lines.Add(Line(owner.Name, entry.Const, "field entry has no Type companion"));
                }
            }

            foreach (IGrouping<string?, TargetEntry> method in own
                .Where(e => e.Kind == TargetKind.OverloadCompanion)
                .GroupBy(e => e.BelongsTo))
            {
                HashSet<string> seen = new(StringComparer.Ordinal);
                foreach (TargetEntry companion in method.OrderBy(e => e.Const, StringComparer.Ordinal))
                {
                    if (!seen.Add(companion.Value))
                    {
                        lines.Add(Line(owner.Name, companion.Const, "overload duplicates another companion of the same method"));
                    }
                }
            }
        }

        HashSet<string> typeNames = new(StringComparer.Ordinal);
        foreach (TargetEntry typeEntry in entries.Where(e => e.Kind == TargetKind.TypeEntry).OrderBy(e => e.Owner, StringComparer.Ordinal))
        {
            if (!typeNames.Add(typeEntry.Value))
            {
                lines.Add(Line(typeEntry.Owner, typeEntry.Const, "TypeName duplicates another owner class"));
            }
        }

        if (listType.GetNestedType("HarmonyPatched", BindingFlags.Public | BindingFlags.NonPublic) is null)
        {
            lines.Add(Line("HarmonyPatched", "*", "class is missing"));
        }
        else
        {
            CheckHarmonyPatched(listType, entries, lines);
        }

        return lines.OrderBy(l => l, StringComparer.Ordinal).ToList();
    }

    private static void CheckHarmonyPatched(Type listType, IReadOnlyList<TargetEntry> entries, List<string> lines)
    {
        HashSet<string> valid = new(StringComparer.Ordinal);
        foreach (TargetEntry typeEntry in entries.Where(e => e.Kind == TargetKind.TypeEntry))
        {
            foreach (TargetEntry method in entries.Where(e => e.Owner == typeEntry.Owner && e.Kind == TargetKind.MethodOverloads))
            {
                foreach (TargetEntry companion in entries.Where(
                    e => e.Owner == typeEntry.Owner && e.Kind == TargetKind.OverloadCompanion && e.BelongsTo == method.Const))
                {
                    valid.Add(typeEntry.Value + "." + method.Value + "(" + companion.Value + ")");
                }
            }
        }

        HashSet<string> seen = new(StringComparer.Ordinal);
        foreach (HarmonyEntry entry in TargetList.HarmonyPatchedEntries(listType))
        {
            if (!valid.Contains(entry.Value))
            {
                lines.Add(Line("HarmonyPatched", entry.Const, Composition));
            }

            if (!seen.Add(entry.Value))
            {
                lines.Add(Line("HarmonyPatched", entry.Const, "value duplicates another HarmonyPatched entry"));
            }
        }
    }

    private static string Line(string owner, string constant, string rule) =>
        "BST2015: PatchTargetNames." + owner + "." + constant + ": " + rule + ".";
}
