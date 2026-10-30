namespace BaseStorage.PatchTargets.Tests.Checking;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

/// <summary>The kind of one classified constant (contract 4.5.1 rules 1 to 5).</summary>
internal enum TargetKind
{
    /// <summary>Rule 1: <c>TypeName</c>.</summary>
    TypeEntry,

    /// <summary>Rule 5, name-only: exactly one declared method has the name.</summary>
    MethodNameOnly,

    /// <summary>Rule 5, overload mode: the method has at least one overload companion.</summary>
    MethodOverloads,

    /// <summary>Rule 4: one overload companion of a method entry.</summary>
    OverloadCompanion,

    /// <summary>Rule 3: a field name.</summary>
    FieldEntry,

    /// <summary>Rule 2: the <c>Type</c> companion of a field entry.</summary>
    FieldTypeCompanion,
}

/// <summary>One classified constant of <c>PatchTargetNames</c>. <c>BelongsTo</c> names the method or field constant.</summary>
internal sealed record TargetEntry(string Owner, string Const, TargetKind Kind, string Value, string? BelongsTo);

/// <summary>One constant of <c>PatchTargetNames.HarmonyPatched</c>.</summary>
internal sealed record HarmonyEntry(string Const, string Value);

/// <summary>
/// Classifies a list type by reflection (contract 4.5.1).
/// </summary>
internal static class TargetList
{
    private const string HarmonyPatchedName = "HarmonyPatched";
    private const string ParametersMarker = "Parameters";
    private const BindingFlags NestedFlags = BindingFlags.Public | BindingFlags.NonPublic;

    /// <summary>Every entry of every owner class except HarmonyPatched, ordered by owner (ordinal) then by declaration order.</summary>
    public static IReadOnlyList<TargetEntry> Classify(Type listType)
    {
        List<TargetEntry> result = [];
        foreach (Type owner in OwnerClasses(listType))
        {
            result.AddRange(ClassifyOwner(owner));
        }

        return result;
    }

    /// <summary>The constants of the HarmonyPatched class, in ordinal name order.</summary>
    public static IReadOnlyList<HarmonyEntry> HarmonyPatchedEntries(Type listType)
    {
        Type? harmony = listType.GetNestedType(HarmonyPatchedName, NestedFlags);
        if (harmony is null)
        {
            return [];
        }

        return Constants(harmony)
            .Select(c => new HarmonyEntry(c.Name, c.Value))
            .ToList();
    }

    /// <summary>The nested owner classes (everything but HarmonyPatched), in ordinal name order.</summary>
    internal static IReadOnlyList<Type> OwnerClasses(Type listType)
    {
        return listType.GetNestedTypes(NestedFlags)
            .Where(t => t.Name != HarmonyPatchedName)
            .OrderBy(t => t.Name, StringComparer.Ordinal)
            .ToList();
    }

    private static List<(string Name, string Value)> Constants(Type type) =>
        LiteralStrings(type)
            .OrderBy(f => f.Name, StringComparer.Ordinal)
            .Select(f => (f.Name, (string)f.GetRawConstantValue()!))
            .ToList();

    private static List<(string Name, string Value)> ConstantsInDeclarationOrder(Type type) =>
        LiteralStrings(type)
            .OrderBy(f => f.MetadataToken)
            .Select(f => (f.Name, (string)f.GetRawConstantValue()!))
            .ToList();

    private static IEnumerable<FieldInfo> LiteralStrings(Type type) =>
        type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string));

    private static IEnumerable<TargetEntry> ClassifyOwner(Type owner)
    {
        List<(string Name, string Value)> constants = ConstantsInDeclarationOrder(owner);
        HashSet<string> names = constants.Select(c => c.Name).ToHashSet(StringComparer.Ordinal);
        List<(string Name, string Value, TargetKind Kind, string? BelongsTo)> classified = [];

        foreach ((string name, string value) in constants)
        {
            if (name == "TypeName")
            {
                classified.Add((name, value, TargetKind.TypeEntry, null));
            }
            else if (name.StartsWith("Field", StringComparison.Ordinal)
                && name.EndsWith("Type", StringComparison.Ordinal)
                && names.Contains(name[..^4]))
            {
                classified.Add((name, value, TargetKind.FieldTypeCompanion, name[..^4]));
            }
            else if (name.StartsWith("Field", StringComparison.Ordinal))
            {
                classified.Add((name, value, TargetKind.FieldEntry, null));
            }
            else
            {
                string? method = names
                    .Where(m => m != "TypeName"
                        && !m.StartsWith("Field", StringComparison.Ordinal)
                        && name.Length > m.Length + ParametersMarker.Length
                        && name.StartsWith(m + ParametersMarker, StringComparison.Ordinal))
                    .OrderByDescending(m => m.Length)
                    .ThenBy(m => m, StringComparer.Ordinal)
                    .FirstOrDefault();

                classified.Add(method is null
                    ? (name, value, TargetKind.MethodNameOnly, null)
                    : (name, value, TargetKind.OverloadCompanion, method));
            }
        }

        HashSet<string> withCompanions = classified
            .Where(c => c.Kind == TargetKind.OverloadCompanion)
            .Select(c => c.BelongsTo!)
            .ToHashSet(StringComparer.Ordinal);

        foreach ((string name, string value, TargetKind kind, string? belongsTo) in classified)
        {
            TargetKind final = kind == TargetKind.MethodNameOnly && withCompanions.Contains(name)
                ? TargetKind.MethodOverloads
                : kind;
            yield return new TargetEntry(owner.Name, name, final, value, belongsTo);
        }
    }
}
