namespace BaseStorage.Core.Tests.Consumption;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Availability;
using BaseStorage.Core.Consumption;
using BaseStorage.Core.Reservations;
using BaseStorage.Core.Settings;
using BaseStorage.Core.Tests.PatchGuard;
using Xunit;

/// <summary>
/// AC-1 (ADR-GWIN-031, CX1, CX2): an IL-level dependency check on BaseStorage.Core.Consumption. It is a private copy of the LockingDependencyTests
/// walk, restricted to the Consumption namespace, and it includes nested and compiler-generated types. The Core dll is read through PEReader.
/// </summary>
public sealed class ConsumptionDependencyTests
{
    private const string ConsumptionNamespace = "BaseStorage.Core.Consumption";
    private const BindingFlags AllDeclared = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static
        | BindingFlags.DeclaredOnly;

    private const int TypeRefTable = 0x01;
    private const int TypeDefTable = 0x02;
    private const int FieldDefTable = 0x04;
    private const int MethodDefTable = 0x06;
    private const int MemberRefTable = 0x0A;
    private const int TypeSpecTable = 0x1B;
    private const int MethodSpecTable = 0x2B;

    private static readonly string[] BannedNamespaces = { "System.Threading", "System.Diagnostics", "System.Linq", "System.IO" };

    private static readonly Type[] RequiredUses =
    {
        typeof(ChestReservations<>),
        typeof(Reservation<>),
        typeof(IContainerLayer),
        typeof(BaseStorageSettings),
        typeof(ConsumptionPlan),
    };

    // GW-U-541 (AC-1, CX2): no call or callvirt in the Consumption types targets IClock.get_Elapsed; time goes only through Reservation.
    [Fact]
    public void NoConsumptionMethodCallsTheClockElapsedGetter()
    {
        Assembly core = typeof(ConsumptionExecutor).Assembly;
        Module module = core.ManifestModule;
        using FileStream stream = new(core.Location, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using PEReader pe = new(stream);
        MetadataReader metadata = pe.GetMetadataReader();

        List<string> offenders = new();
        foreach (MethodBase method in ConsumptionMethods(core))
        {
            MethodDefinition definition = metadata.GetMethodDefinition(MetadataTokens.MethodDefinitionHandle(method.MetadataToken));
            if (definition.RelativeVirtualAddress == 0)
            {
                continue;
            }

            foreach (IlInstruction instruction in IlInstructionReader.Decode(pe, definition.RelativeVirtualAddress))
            {
                if (instruction.Token == 0 || (instruction.OpCode != OpCodes.Call && instruction.OpCode != OpCodes.Callvirt))
                {
                    continue;
                }

                MethodBase? target = module.ResolveMethod(instruction.Token);
                if (target is not null && target.Name == "get_Elapsed" && target.DeclaringType == typeof(IClock))
                {
                    offenders.Add(method.Name + " calls IClock.get_Elapsed");
                }
            }
        }

        Assert.Empty(offenders);
    }

    // GW-U-541 (AC-1, CX1): no used type is in System.Threading, System.Diagnostics, System.Linq or System.IO, and none comes from a forbidden assembly.
    [Fact]
    public void NoDependencyIsBannedOrFromAForbiddenAssembly()
    {
        List<string> offenders = new();
        foreach ((Type used, string origin) in CollectUses())
        {
            foreach (Type leaf in Leaves(used))
            {
                string ns = leaf.Namespace ?? string.Empty;
                if (BannedNamespaces.Any(banned => ns == banned || ns.StartsWith(banned + ".", StringComparison.Ordinal)))
                {
                    offenders.Add((leaf.FullName ?? "(unnamed)") + " used by " + origin);
                }

                if (ForbiddenAssemblies.IsForbidden(leaf.Assembly.GetName().Name ?? string.Empty))
                {
                    offenders.Add((leaf.FullName ?? "(unnamed)") + " from a forbidden assembly, used by " + origin);
                }
            }
        }

        Assert.Empty(offenders.Distinct(StringComparer.Ordinal).OrderBy(text => text, StringComparer.Ordinal));
    }

    // GW-U-541 (AC-1, ADR-GWIN-031): the collected set contains the reservation, container, settings and plan dependencies, so the walk is not vacuous.
    [Fact]
    public void TheCollectedSetContainsTheConsumptionDependencies()
    {
        HashSet<Type> collected = CollectUses().SelectMany(use => Leaves(use.Used)).ToHashSet();

        foreach (Type required in RequiredUses)
        {
            Assert.Contains(required, collected);
        }
    }

    private static List<MethodBase> ConsumptionMethods(Assembly core) =>
        core.GetTypes()
            .Where(type => type.Namespace == ConsumptionNamespace)
            .SelectMany(type => type.GetConstructors(AllDeclared).Cast<MethodBase>().Concat(type.GetMethods(AllDeclared)))
            .ToList();

    private static List<(Type Used, string Origin)> CollectUses()
    {
        Assembly core = typeof(ConsumptionExecutor).Assembly;
        Module module = core.ManifestModule;
        using FileStream stream = new(core.Location, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using PEReader pe = new(stream);
        MetadataReader metadata = pe.GetMetadataReader();
        List<(Type Used, string Origin)> uses = new();

        Type[] types = core.GetTypes()
            .Where(type => type.Namespace == ConsumptionNamespace)
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToArray();

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
            if (instruction.Token != 0)
            {
                AddToken(module, instruction.Token, origin + " (IL " + instruction.OpCode.Name + ")", uses);
            }
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
                if (module.ResolveField(token) is FieldInfo definedField)
                {
                    AddField(definedField, origin, uses);
                }

                break;
            case MethodDefTable:
            case MethodSpecTable:
                AddMethodToken(module, token, origin, uses);
                break;
            case MemberRefTable:
                MemberInfo? member = module.ResolveMember(token);
                if (member is FieldInfo memberField)
                {
                    AddField(memberField, origin, uses);
                }
                else if (member is MethodBase)
                {
                    AddMethodToken(module, token, origin, uses);
                }

                break;
            default:
                break;
        }
    }

    private static void AddMethodToken(Module module, int token, string origin, List<(Type Used, string Origin)> uses)
    {
        MethodBase? method = module.ResolveMethod(token);
        if (method is not null)
        {
            AddMethod(method, origin, uses);
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
}
