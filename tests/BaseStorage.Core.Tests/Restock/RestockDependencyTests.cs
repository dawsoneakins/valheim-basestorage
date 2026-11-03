namespace BaseStorage.Core.Tests.Restock;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using BaseStorage.Core.Bases.Membership;
using BaseStorage.Core.Restock;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Tests.PatchGuard;

/// <summary>
/// GW-U-2531 (S-QR-001 AC-6, R1/R2, P1, contract section 1): an IL-level dependency check on BaseStorage.Core.Restock and
/// BaseStorage.Core.Snapshots.Identities. It is a private copy of the GW-U-575 walk in OrganizeDependencyTests (ADR-GWIN-172).
/// It keeps the Coverlet tolerance of GW-U-2344. It is red until the planner and the identity constructor bodies land, because
/// the non-vacuous assertions need the ContainerEligibility and SnapshotGuard call sites that only real bodies contain.
/// </summary>
public sealed class RestockDependencyTests
{
    private const string RestockNamespace = "BaseStorage.Core.Restock";
    private const string IdentitiesNamespace = "BaseStorage.Core.Snapshots.Identities";
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
        "System.Collections.Generic",
        "System.Collections.ObjectModel",
        "System.Runtime.CompilerServices",
    };

    private static readonly string[] BannedSystemTypes =
    {
        "System.DateTime",
        "System.DateTimeOffset",
        "System.Environment",
        "System.Random",
        "System.Diagnostics.Stopwatch",
    };

    private static readonly string[] BannedSystemNamespacePrefixes = { "System.IO", "System.Threading", "System.Net" };

    private static readonly string[] AllowedCoreTypes =
    {
        "BaseStorage.Core.Snapshots.ContainerSnapshot",
        "BaseStorage.Core.Snapshots.ContainerId",
        "BaseStorage.Core.Snapshots.WorldPosition",
        "BaseStorage.Core.Snapshots.ItemStackSnapshot",
        "BaseStorage.Core.Snapshots.ContainerFlags",
        "BaseStorage.Core.Persistence.CategoryAssignment",
        "BaseStorage.Core.Bases.Membership.ContainerEligibility",
        "BaseStorage.Core.Bases.Membership.ContainerPurpose",
        "BaseStorage.Core.Bases.Membership.EligibilityOptions",
        "BaseStorage.Core.Bases.Membership.ExclusionReason",
    };

    // GW-U-2531 (allowlist, forbidden assemblies and the non-vacuous check, one walk)
    [Fact]
    public void RestockAndIdentitiesUseOnlyAllowedTypes()
    {
        var (types, members) = Collect();

        string[] offenders = types
            .SelectMany(use => Leaves(use.Type).Select(leaf => (Leaf: leaf, use.Origin)))
            .Where(item => !IsAllowed(item.Leaf, item.Origin))
            .Select(item => $"{item.Leaf.FullName ?? "(unnamed)"} used by {item.Origin}")
            .Distinct(StringComparer.Ordinal)
            .OrderBy(text => text, StringComparer.Ordinal)
            .ToArray();
        Assert.Empty(offenders);

        string[] forbidden = types
            .SelectMany(use => Leaves(use.Type).Select(leaf => (Leaf: leaf, use.Origin)))
            .Where(item => ForbiddenAssemblies.FamilyOf(item.Leaf.Assembly.GetName().Name ?? string.Empty) is not null)
            .Select(item => $"{item.Leaf.FullName ?? "(unnamed)"} from {item.Leaf.Assembly.GetName().Name} used by {item.Origin}")
            .OrderBy(text => text, StringComparer.Ordinal)
            .ToArray();
        Assert.Empty(forbidden);

        HashSet<Type> collected = types.SelectMany(use => Leaves(use.Type)).ToHashSet();
        Type? snapshotGuard = typeof(ItemStackSnapshot).Assembly.GetType("BaseStorage.Core.Snapshots.SnapshotGuard", throwOnError: false);
        Assert.NotNull(snapshotGuard);
        Assert.Contains(collected, type => type == typeof(ContainerEligibility));
        Assert.Contains(
            members,
            pair => pair.Member is MethodInfo method && method.Name == nameof(ContainerEligibility.GetExclusionReason)
                && method.DeclaringType == typeof(ContainerEligibility));
        Assert.Contains(
            members,
            pair => pair.Member is MethodInfo method && method.Name == "Finite" && method.DeclaringType == snapshotGuard);
    }

    /// <summary>Every (type, origin) pair that a restock or identity type uses, and every (member, origin) that its IL resolves to.</summary>
    private static (List<(Type Type, string Origin)> Types, List<(MemberInfo Member, string Origin)> Members) Collect()
    {
        Assembly core = typeof(RestockPlanner).Assembly;
        Module module = core.ManifestModule;
        var types = new List<(Type Type, string Origin)>();
        var members = new List<(MemberInfo Member, string Origin)>();

        Type[] owners = core.GetTypes()
            .Where(type => type.Namespace == RestockNamespace || type.Namespace == IdentitiesNamespace)
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToArray();
        Assert.NotEmpty(owners);

        using var stream = new FileStream(core.Location, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var pe = new PEReader(stream);
        MetadataReader metadata = pe.GetMetadataReader();

        foreach (Type type in owners)
        {
            string owner = type.FullName ?? type.Name;
            if (type.BaseType is not null)
            {
                types.Add((type.BaseType, owner + " : base"));
            }

            foreach (Type contract in type.GetInterfaces())
            {
                types.Add((contract, owner + " : interface"));
            }

            foreach (FieldInfo field in type.GetFields(AllDeclared))
            {
                types.Add((field.FieldType, owner + "." + field.Name + " (field)"));
            }

            foreach (PropertyInfo property in type.GetProperties(AllDeclared))
            {
                types.Add((property.PropertyType, owner + "." + property.Name + " (property)"));
            }

            foreach (MethodBase method in type.GetConstructors(AllDeclared).Cast<MethodBase>().Concat(type.GetMethods(AllDeclared)))
            {
                string origin = owner + "." + method.Name;
                foreach (ParameterInfo parameter in method.GetParameters())
                {
                    types.Add((parameter.ParameterType, origin + " (parameter)"));
                }

                if (method is MethodInfo info)
                {
                    types.Add((info.ReturnType, origin + " (return)"));
                }

                AddBodyUses(module, metadata, pe, method, origin, types, members);
            }
        }

        return (types, members);
    }

    private static void AddBodyUses(
        Module module,
        MetadataReader metadata,
        PEReader pe,
        MethodBase method,
        string origin,
        List<(Type Type, string Origin)> types,
        List<(MemberInfo Member, string Origin)> members)
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
                types.Add((local.LocalType, origin + " (local)"));
            }
        }

        foreach (IlInstruction instruction in IlInstructionReader.Decode(pe, definition.RelativeVirtualAddress))
        {
            if (instruction.Token == 0)
            {
                continue;
            }

            string where = origin + " (IL " + instruction.OpCode.Name + ")";
            switch (instruction.Token >> 24)
            {
                case TypeRefTable:
                case TypeDefTable:
                case TypeSpecTable:
                    types.Add((module.ResolveType(instruction.Token), where));
                    break;
                case FieldDefTable:
                case MemberRefTable:
                    AddMember(module.ResolveMember(instruction.Token), where, types, members);
                    break;
                case MethodDefTable:
                case MethodSpecTable:
                    AddMember(module.ResolveMethod(instruction.Token), where, types, members);
                    break;
                default:
                    break;
            }
        }
    }

    private static void AddMember(
        MemberInfo? member,
        string origin,
        List<(Type Type, string Origin)> types,
        List<(MemberInfo Member, string Origin)> members)
    {
        if (member is null)
        {
            return;
        }

        members.Add((member, origin));
        if (member.DeclaringType is not null)
        {
            types.Add((member.DeclaringType, origin));
        }

        if (member is FieldInfo field)
        {
            types.Add((field.FieldType, origin));
        }
        else if (member is MethodInfo method)
        {
            types.Add((method.ReturnType, origin));
            foreach (ParameterInfo parameter in method.GetParameters())
            {
                types.Add((parameter.ParameterType, origin));
            }
        }
        else if (member is ConstructorInfo constructor)
        {
            foreach (ParameterInfo parameter in constructor.GetParameters())
            {
                types.Add((parameter.ParameterType, origin));
            }
        }
    }

    /// <summary>The types a type is built from: its array and by-ref element, and its generic definition and arguments.</summary>
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

    private static bool IsAllowed(Type leaf, string origin)
    {
        string ns = leaf.Namespace ?? string.Empty;
        string full = leaf.FullName ?? string.Empty;

        // Coverlet's injected tracker is not a Core dependency (GW-U-2344, S-E009-001).
        if (full.StartsWith("Coverlet.Core.Instrumentation.Tracker.", StringComparison.Ordinal))
        {
            return true;
        }

        if (ns == RestockNamespace || ns == IdentitiesNamespace)
        {
            return true;
        }

        // SnapshotGuard is allowed only for the identity namespace (contract 1).
        if (full == "BaseStorage.Core.Snapshots.SnapshotGuard")
        {
            return origin.StartsWith(IdentitiesNamespace + ".", StringComparison.Ordinal);
        }

        if (AllowedCoreTypes.Contains(full, StringComparer.Ordinal))
        {
            return true;
        }

        if (BannedSystemTypes.Contains(full, StringComparer.Ordinal))
        {
            return false;
        }

        if (BannedSystemNamespacePrefixes.Any(prefix => ns == prefix || ns.StartsWith(prefix + ".", StringComparison.Ordinal)))
        {
            return false;
        }

        return AllowedSystemNamespaces.Contains(ns, StringComparer.Ordinal);
    }
}
