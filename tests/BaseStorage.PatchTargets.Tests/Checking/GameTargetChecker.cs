namespace BaseStorage.PatchTargets.Tests.Checking;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

/// <summary>
/// Checks one classified entry against a game assembly (contract 4.6.2). Returns null when the entry passes, or the
/// BST2010 to BST2014 text (several lines joined with <c>\n</c> for an overload-mode method, see ADR-GWIN-015).
/// <paramref name="entries"/> is the full classified list, so an overload-mode method can find its companions.
/// </summary>
internal static class GameTargetChecker
{
    internal const BindingFlags Declared =
        BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

    public static string? Check(
        TargetEntry entry,
        IReadOnlyList<TargetEntry> entries,
        Func<string, Type?> findType,
        string dllPath)
    {
        TargetEntry? typeEntry = entries.FirstOrDefault(e => e.Owner == entry.Owner && e.Kind == TargetKind.TypeEntry);
        string typeName = typeEntry?.Value ?? string.Empty;
        Type? type = typeEntry is null ? null : findType(typeName);
        if (type is null)
        {
            return "BST2010: Patch target type '" + typeName + "' (PatchTargetNames." + entry.Owner + ".TypeName) not found in '" + dllPath + "'.";
        }

        return entry.Kind switch
        {
            TargetKind.TypeEntry => null,
            TargetKind.MethodNameOnly => CheckNameOnly(entry, type, dllPath),
            TargetKind.MethodOverloads => CheckAllCompanions(entry, entries, type, dllPath),
            TargetKind.OverloadCompanion => CheckOverload(entry, entries, type, dllPath),
            TargetKind.FieldEntry => CheckField(entry, entry, type, dllPath, checkType: false),
            TargetKind.FieldTypeCompanion => CheckField(
                entries.First(e => e.Owner == entry.Owner && e.Const == entry.BelongsTo),
                entry,
                type,
                dllPath,
                checkType: true),
            _ => throw new ArgumentOutOfRangeException(nameof(entry)),
        };
    }

    private static string? CheckNameOnly(TargetEntry entry, Type type, string dllPath)
    {
        int count = type.GetMethods(Declared).Count(m => m.Name == entry.Value);
        if (count == 1)
        {
            return null;
        }

        return "BST2011: Patch target method '" + type.FullName + "." + entry.Value + "' (PatchTargetNames." + entry.Owner + "." + entry.Const
            + ") has " + count + " declared methods with that name in '" + dllPath + "'; expected exactly 1.";
    }

    private static string? CheckAllCompanions(TargetEntry method, IReadOnlyList<TargetEntry> entries, Type type, string dllPath)
    {
        List<string> failures = entries
            .Where(e => e.Owner == method.Owner && e.Kind == TargetKind.OverloadCompanion && e.BelongsTo == method.Const)
            .OrderBy(e => e.Const, StringComparer.Ordinal)
            .Select(companion => CheckOverload(companion, entries, type, dllPath))
            .OfType<string>()
            .ToList();

        return failures.Count == 0 ? null : string.Join("\n", failures);
    }

    private static string? CheckOverload(TargetEntry companion, IReadOnlyList<TargetEntry> entries, Type type, string dllPath)
    {
        TargetEntry method = entries.First(e => e.Owner == companion.Owner && e.Const == companion.BelongsTo);
        // Rule PT2 (contract version-rpc-contracts.md 6.2): parameter types render canonically, so a non-generic parameter keeps its
        // FullName and a generic method parameter inside a constructed delegate appears by name, e.g. System.Action`2[ZRpc,T].
        List<string> declared = type.GetMethods(Declared)
            .Where(m => m.Name == method.Value)
            .Select(m => string.Join(",", m.GetParameters().Select(p => Canonical(p.ParameterType))))
            .ToList();

        if (declared.Count(p => string.Equals(p, companion.Value, StringComparison.Ordinal)) == 1)
        {
            return null;
        }

        string listed = declared.Count == 0
            ? "(none)"
            : string.Join("; ", declared.Select(p => method.Value + "(" + p + ")").OrderBy(s => s, StringComparer.Ordinal));

        return "BST2012: Patch target overload '" + type.FullName + "." + method.Value + "(" + companion.Value + ")' (PatchTargetNames."
            + companion.Owner + "." + companion.Const + ") not found in '" + dllPath + "'. Declared overloads: " + listed + ".";
    }

    private static string? CheckField(TargetEntry field, TargetEntry row, Type type, string dllPath, bool checkType)
    {
        FieldInfo? found = type.GetField(field.Value, Declared);
        if (found is null)
        {
            return "BST2013: Patch target field '" + type.FullName + "." + field.Value + "' (PatchTargetNames." + field.Owner + "." + field.Const
                + ") not found in '" + dllPath + "'.";
        }

        string actualType = Canonical(found.FieldType);
        if (checkType && !string.Equals(actualType, row.Value, StringComparison.Ordinal))
        {
            return "BST2014: Patch target field '" + type.FullName + "." + field.Value + "' (PatchTargetNames." + field.Owner + "." + field.Const
                + ") has type '" + actualType + "' in '" + dllPath + "'; expected '" + row.Value + "'.";
        }

        return null;
    }

    /// <summary>
    /// The canonical name of a field type (rule PT1, contract S-E004-014 section 6.3): <c>FullName</c> for any type that is not a
    /// constructed generic, so every pre-existing row is unchanged; for a constructed generic, the definition's canonical name followed
    /// by the arguments' canonical names in brackets, joined by commas, with no spaces and no assembly qualification.
    /// Works on runtime and MetadataLoadContext types alike.
    /// </summary>
    internal static string Canonical(Type type)
    {
        if (!type.IsGenericType || type.IsGenericTypeDefinition)
        {
            return type.FullName ?? type.Name;
        }

        string definition = Canonical(type.GetGenericTypeDefinition());
        string arguments = string.Join(",", type.GetGenericArguments().Select(Canonical));
        return definition + "[" + arguments + "]";
    }
}
