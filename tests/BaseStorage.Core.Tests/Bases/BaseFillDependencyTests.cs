namespace BaseStorage.Core.Tests.Bases;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using BaseStorage.Core.Bases.Fill;
using BaseStorage.Core.Tests.PatchGuard;

/// <summary>
/// GW-U-2250 (AC-2, BF1, BF2, BF7, base-fill contract 3.4): an IL-level dependency check on BaseStorage.Core.Bases.Fill. It is a private copy
/// of the StationPanelDependencyTests walk, scoped to this namespace. It covers every type named in signatures, bases, fields, locals and IL
/// operands, including compiler-generated types, and it reads the Core dll through PEReader. Compile-red until BaseFill exists.
/// </summary>
public sealed class BaseFillDependencyTests
{
    private const string FillNamespace = "BaseStorage.Core.Bases.Fill";
    private const BindingFlags AllDeclared = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static
        | BindingFlags.DeclaredOnly;

    private const int TypeRefTable = 0x01;
    private const int TypeDefTable = 0x02;
    private const int FieldDefTable = 0x04;
    private const int MethodDefTable = 0x06;
    private const int MemberRefTable = 0x0A;
    private const int TypeSpecTable = 0x1B;
    private const int MethodSpecTable = 0x2B;

    private static readonly string[] AllowedNamespaces =
    {
        "System",
        "System.Collections.Generic",
        "System.Runtime.CompilerServices",
        "BaseStorage.Core.Snapshots",
        FillNamespace,
    };

    private static readonly string[] BannedTypes =
    {
        "System.DateTime",
        "System.DateTimeOffset",
        "System.Environment",
        "System.Random",
        "System.Console",
        "System.GC",
        "System.MidpointRounding",
        "System.Double",
        "System.Single",
        "System.Decimal",
    };

    // GW-U-2250 (BF1, BF2, BF7): every type that a Fill type uses is System, System.Collections.Generic, System.Runtime.CompilerServices or a
    // snapshot type, none of the banned types is used, and the walk actually collected the BaseFill type.
    [Fact]
    public void BaseFillDependsOnlyOnSystemCollectionsAndSnapshots()
    {
        List<(Type Used, string Origin)> uses = CollectUses();

        string[] offenders = uses
            .SelectMany(use => Leaves(use.Used).Select(leaf => (Leaf: leaf, use.Origin)))
            .Where(item => !IsAllowed(item.Leaf))
            .Select(item => $"{item.Leaf.FullName ?? "(unnamed)"} used by {item.Origin}")
            .Distinct(StringComparer.Ordinal)
            .OrderBy(text => text, StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(offenders);
        Assert.Contains(FillNamespace + ".BaseFill", uses.SelectMany(use => Leaves(use.Used)).Select(leaf => leaf.FullName ?? string.Empty), StringComparer.Ordinal);
    }

    private static List<(Type Used, string Origin)> CollectUses()
    {
        Assembly core = typeof(BaseFill).Assembly;
        Module module = core.ManifestModule;
        var uses = new List<(Type Used, string Origin)>();

        Type[] types = core.GetTypes()
            .Where(type => type.Namespace == FillNamespace)
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

        if (BannedTypes.Contains(full, StringComparer.Ordinal))
        {
            return false;
        }

        return AllowedNamespaces.Contains(ns, StringComparer.Ordinal);
    }
}
