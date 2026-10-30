namespace BaseStorage.Core.Tests.StationPanel;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using BaseStorage.Core.StationPanel;
using BaseStorage.Core.Tests.PatchGuard;

/// <summary>
/// AC-4 (SP2, ADR-GWIN-041): an IL-level dependency check on BaseStorage.Core.StationPanel. It is a private copy of the Locking walk
/// (LockingDependencyTests stays unedited, per the ADR-GWIN-015 precedent). It covers every type named in signatures, bases, fields, locals and
/// IL operands, including non-public and compiler-generated types, and it reads the Core dll through PEReader.
/// </summary>
public sealed class StationPanelDependencyTests
{
    private const string StationPanelNamespace = "BaseStorage.Core.StationPanel";
    private const BindingFlags AllDeclared = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static
        | BindingFlags.DeclaredOnly;

    private const int TypeRefTable = 0x01;
    private const int TypeDefTable = 0x02;
    private const int FieldDefTable = 0x04;
    private const int MethodDefTable = 0x06;
    private const int MemberRefTable = 0x0A;
    private const int TypeSpecTable = 0x1B;
    private const int MethodSpecTable = 0x2B;

    private static readonly string[] AllowedSystemNamespaces =
    {
        "System",
        "System.Runtime.CompilerServices",
    };

    private static readonly string[] BannedSystemTypes =
    {
        "System.DateTime",
        "System.DateTimeOffset",
        "System.Environment",
        "System.Random",
        "System.Console",
        "System.GC",
    };

    private static readonly string[] AllowedCoreTypes =
    {
        "BaseStorage.Core.Ui.StringTable",
        "BaseStorage.Core.Ui.StringKeys",
        "BaseStorage.Core.Ui.Plural",
        "BaseStorage.Core.Ui.SingleLine",
    };

    private static readonly string[] RequiredForANonVacuousCheck =
    {
        "BaseStorage.Core.Ui.StringTable",
        "BaseStorage.Core.Ui.Plural",
        "BaseStorage.Core.Ui.SingleLine",
    };

    // GW-U-549 (AC-4, SP2, allowlist): every type that a StationPanel type uses is System, a StationPanel type, or one of the four allowlisted Ui types.
    // This row passes on the stubs, because the stubs use only System and StationPanel types.
    [Fact]
    public void EveryDependencyIsASystemOrStationPanelOrAllowlistedUiType()
    {
        string[] offenders = CollectUses()
            .SelectMany(use => Leaves(use.Used).Select(leaf => (Leaf: leaf, use.Origin)))
            .Where(item => !IsAllowed(item.Leaf))
            .Select(item => $"{item.Leaf.FullName ?? "(unnamed)"} used by {item.Origin}")
            .Distinct(StringComparer.Ordinal)
            .OrderBy(text => text, StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(offenders);
    }

    // GW-U-549 (AC-4, SP2, not vacuous): the collected set contains the three Ui types that the text code uses. This row is red on the stubs,
    // because the text code is not written yet.
    [Fact]
    public void TheCollectedSetContainsTheUiTypesOfTheTextCode()
    {
        HashSet<string> collected = CollectUses()
            .SelectMany(use => Leaves(use.Used))
            .Select(leaf => leaf.FullName ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        foreach (string required in RequiredForANonVacuousCheck)
        {
            Assert.Contains(required, collected);
        }
    }

    // GW-U-549 (AC-4, SP2): no assembly behind any collected dependency is Unity, Valheim or BepInEx.
    [Fact]
    public void NoDependencyComesFromAForbiddenAssembly()
    {
        string[] forbidden = CollectUses()
            .SelectMany(use => Leaves(use.Used).Select(leaf => (Leaf: leaf, use.Origin)))
            .Where(item => ForbiddenAssemblies.IsForbidden(item.Leaf.Assembly.GetName().Name ?? string.Empty))
            .Select(item => $"{item.Leaf.FullName ?? "(unnamed)"} from {item.Leaf.Assembly.GetName().Name} used by {item.Origin}")
            .OrderBy(text => text, StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(forbidden);
    }

    private static List<(Type Used, string Origin)> CollectUses()
    {
        Assembly core = typeof(PanelRect).Assembly;
        Module module = core.ManifestModule;
        var uses = new List<(Type Used, string Origin)>();

        Type[] types = core.GetTypes()
            .Where(type => type.Namespace == StationPanelNamespace)
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToArray();
        Assert.NotEmpty(types);

        using var stream = new FileStream(core.Location, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var pe = new PEReader(stream);
        MetadataReader metadata = pe.GetMetadataReader();

        foreach (Type type in types)
        {
            string owner = type.FullName ?? type.Name;
            if (type.BaseType is not null)
            {
                uses.Add((type.BaseType, owner + " : base"));
            }

            foreach (Type contract in type.GetInterfaces())
            {
                uses.Add((contract, owner + " : interface"));
            }

            foreach (FieldInfo field in type.GetFields(AllDeclared))
            {
                uses.Add((field.FieldType, owner + "." + field.Name + " (field)"));
            }

            foreach (PropertyInfo property in type.GetProperties(AllDeclared))
            {
                uses.Add((property.PropertyType, owner + "." + property.Name + " (property)"));
            }

            foreach (MethodBase method in type.GetConstructors(AllDeclared).Cast<MethodBase>().Concat(type.GetMethods(AllDeclared)))
            {
                string origin = owner + "." + method.Name;
                foreach (ParameterInfo parameter in method.GetParameters())
                {
                    uses.Add((parameter.ParameterType, origin + " (parameter)"));
                }

                if (method is MethodInfo info)
                {
                    uses.Add((info.ReturnType, origin + " (return)"));
                }

                AddBodyUses(module, metadata, pe, method, origin, uses);
            }
        }

        return uses;
    }

    private static void AddBodyUses(
        Module module,
        MetadataReader metadata,
        PEReader pe,
        MethodBase method,
        string origin,
        List<(Type Used, string Origin)> uses)
    {
        MethodDefinition definition = metadata.GetMethodDefinition(MetadataTokens.MethodDefinitionHandle(method.MetadataToken));
        if (definition.RelativeVirtualAddress == 0)
        {
            return;
        }

        MethodBody? body = method.GetMethodBody();
        if (body is not null)
        {
            foreach (LocalVariableInfo local in body.LocalVariables)
            {
                uses.Add((local.LocalType, origin + " (local)"));
            }
        }

        foreach (IlInstruction instruction in IlInstructionReader.Decode(pe, definition.RelativeVirtualAddress))
        {
            if (instruction.Token == 0)
            {
                continue;
            }

            string where = origin + " (IL " + instruction.OpCode.Name + ")";
            AddToken(module, instruction.Token, where, uses);
        }
    }

    private static void AddToken(Module module, int token, string origin, List<(Type Used, string Origin)> uses)
    {
        switch (token >> 24)
        {
            case TypeRefTable:
            case TypeDefTable:
            case TypeSpecTable:
                uses.Add((module.ResolveType(token), origin));
                break;
            case FieldDefTable:
                FieldInfo? definedField = module.ResolveField(token);
                if (definedField is not null)
                {
                    AddField(definedField, origin, uses);
                }

                break;
            case MethodDefTable:
            case MethodSpecTable:
                MethodBase? resolvedMethod = module.ResolveMethod(token);
                if (resolvedMethod is not null)
                {
                    AddMethod(resolvedMethod, origin, uses);
                }

                break;
            case MemberRefTable:
                MemberInfo? member = module.ResolveMember(token);
                if (member is FieldInfo field)
                {
                    AddField(field, origin, uses);
                }
                else if (member is MethodBase method)
                {
                    AddMethod(method, origin, uses);
                }

                break;
            default:
                break;
        }
    }

    private static void AddField(FieldInfo field, string origin, List<(Type Used, string Origin)> uses)
    {
        if (field.DeclaringType is not null)
        {
            uses.Add((field.DeclaringType, origin));
        }

        uses.Add((field.FieldType, origin));
    }

    private static void AddMethod(MethodBase method, string origin, List<(Type Used, string Origin)> uses)
    {
        if (method.DeclaringType is not null)
        {
            uses.Add((method.DeclaringType, origin));
        }

        foreach (ParameterInfo parameter in method.GetParameters())
        {
            uses.Add((parameter.ParameterType, origin));
        }

        if (method is MethodInfo info)
        {
            uses.Add((info.ReturnType, origin));
            foreach (Type argument in info.GetGenericArguments())
            {
                uses.Add((argument, origin));
            }
        }
    }

    private static IEnumerable<Type> Leaves(Type type)
    {
        if (type.HasElementType)
        {
            foreach (Type element in Leaves(type.GetElementType()!))
            {
                yield return element;
            }

            yield break;
        }

        yield return type.IsGenericType ? type.GetGenericTypeDefinition() : type;

        if (type.IsGenericType)
        {
            foreach (Type argument in type.GetGenericArguments())
            {
                foreach (Type leaf in Leaves(argument))
                {
                    yield return leaf;
                }
            }
        }
    }

    private static bool IsAllowed(Type leaf)
    {
        string ns = leaf.Namespace ?? string.Empty;
        string full = leaf.FullName ?? string.Empty;
        // Edited by S-E009-001 (quality-audit QA1.2): coverlet's injected tracker is not a Core dependency.
        if (full.StartsWith("Coverlet.Core.Instrumentation.Tracker.", StringComparison.Ordinal))
        {
            return true;
        }

        if (ns == StationPanelNamespace)
        {
            return true;
        }

        if (AllowedCoreTypes.Contains(full, StringComparer.Ordinal))
        {
            return true;
        }

        if (BannedSystemTypes.Contains(full, StringComparer.Ordinal))
        {
            return false;
        }

        return AllowedSystemNamespaces.Contains(ns, StringComparer.Ordinal);
    }
}
