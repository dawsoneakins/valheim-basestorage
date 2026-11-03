namespace BaseStorage.Core.Tests.Feeding;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using BaseStorage.Core.Bases.Membership;
using BaseStorage.Core.Feeding;
using BaseStorage.Core.Tests.PatchGuard;

/// <summary>
/// AC-5 (ADR-GWIN-180, contract 0.1 R1/R2 and section 1, 1): an IL-level dependency check on <c>BaseStorage.Core.Feeding</c>. It is a private
/// copy of the GW-U-575 walk in <c>OrganizeDependencyTests</c>. It collects every type that a Feeding type names, and every member that an IL
/// operand resolves to. Core use is limited to Feeding, Snapshots, Persistence (<c>CategoryAssignment</c> only) and Bases.Membership.
/// </summary>
public sealed class FeedDependencyTests
{
    private const string FeedingNamespace = "BaseStorage.Core.Feeding";
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
        "System.Globalization",
        "System.Runtime.CompilerServices",
    };

    private static readonly string[] BannedTypes =
    {
        "System.DateTime",
        "System.DateTimeOffset",
        "System.Environment",
        "System.Random",
        "System.Diagnostics.Stopwatch",
    };

    private static readonly string[] BannedNamespaces = { "System.IO", "System.Threading", "System.Net" };

    // GW-U-2683
    [Fact]
    public void FeedingUsesOnlyAllowedTypes()
    {
        var (types, _) = Collect();

        string[] offenders = types
            .SelectMany(use => Leaves(use.Type).Select(leaf => (Leaf: leaf, use.Origin)))
            .Where(item => !IsAllowed(item.Leaf))
            .Select(item => $"{item.Leaf.FullName ?? "(unnamed)"} used by {item.Origin}")
            .Distinct(StringComparer.Ordinal)
            .OrderBy(text => text, StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(offenders);
    }

    // GW-U-2683 (forbidden assemblies)
    [Fact]
    public void FeedingReferencesNoForbiddenAssembly()
    {
        var (types, _) = Collect();

        string[] forbidden = types
            .SelectMany(use => Leaves(use.Type).Select(leaf => (Leaf: leaf, use.Origin)))
            .Where(item => ForbiddenAssemblies.FamilyOf(item.Leaf.Assembly.GetName().Name ?? string.Empty) is not null)
            .Select(item => $"{item.Leaf.FullName ?? "(unnamed)"} from {item.Leaf.Assembly.GetName().Name} used by {item.Origin}")
            .OrderBy(text => text, StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(forbidden);
    }

    // GW-U-2683 (non-vacuous: red until the overload body calls the eligibility rule)
    [Fact]
    public void FeedingWalkSeesTheEligibilityRule()
    {
        var (types, members) = Collect();
        HashSet<Type> collected = types.SelectMany(use => Leaves(use.Type)).ToHashSet();

        Assert.Contains(typeof(ContainerEligibility), collected);
        Assert.Contains(typeof(EligibilityOptions), collected);
        Assert.Contains(members, member => member.Member.Name == "GetExclusionReason");
    }

    /// <summary>Every (type, origin) pair that a Feeding type uses, and every (member, origin) that its IL resolves to.</summary>
    private static (List<(Type Type, string Origin)> Types, List<(MemberInfo Member, string Origin)> Members) Collect()
    {
        Assembly core = typeof(FeedPlanner).Assembly;
        Module module = core.ManifestModule;
        var types = new List<(Type Type, string Origin)>();
        var members = new List<(MemberInfo Member, string Origin)>();

        Type[] owners = core.GetTypes()
            .Where(type => type.Namespace == FeedingNamespace)
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

    private static bool IsAllowed(Type leaf)
    {
        string ns = leaf.Namespace ?? string.Empty;
        string full = leaf.FullName ?? string.Empty;

        // The Coverlet tolerance of GW-U-2344: the injected tracker is not a Core dependency.
        if (full.StartsWith("Coverlet.Core.Instrumentation.Tracker.", StringComparison.Ordinal))
        {
            return true;
        }

        if (ns == FeedingNamespace || ns == "BaseStorage.Core.Snapshots" || ns == "BaseStorage.Core.Bases.Membership")
        {
            return true;
        }

        if (full == "BaseStorage.Core.Persistence.CategoryAssignment")
        {
            return true;
        }

        if (BannedTypes.Contains(full, StringComparer.Ordinal)
            || BannedNamespaces.Any(banned => ns == banned || ns.StartsWith(banned + ".", StringComparison.Ordinal)))
        {
            return false;
        }

        return AllowedSystemNamespaces.Contains(ns, StringComparer.Ordinal);
    }
}
