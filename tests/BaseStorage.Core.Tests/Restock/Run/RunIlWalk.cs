namespace BaseStorage.Core.Tests.Restock.Run;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using BaseStorage.Core.Tests.PatchGuard;

/// <summary>
/// The IL walk of S-QR-002 (GW-U-2617 and GW-U-2546, ADR-GWIN-176). It is the GW-U-2344 walk generalised over an owner predicate: every
/// signature, field, property, local and IL token of the owner types is resolved to the types and members it names.
/// </summary>
internal static class RunIlWalk
{
    private const BindingFlags AllDeclared = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static
        | BindingFlags.DeclaredOnly;

    private const int TypeRefTable = 0x01;
    private const int TypeDefTable = 0x02;
    private const int FieldDefTable = 0x04;
    private const int MethodDefTable = 0x06;
    private const int MemberRefTable = 0x0A;
    private const int TypeSpecTable = 0x1B;
    private const int MethodSpecTable = 0x2B;

    /// <summary>The owner types, and every (type, origin) and (member, origin) that they resolve to.</summary>
    /// <param name="Owners">The owner types, ordered by full name.</param>
    /// <param name="Types">Every type use, with the place that uses it.</param>
    /// <param name="Members">Every member the IL resolves to, with the place that resolves it.</param>
    internal sealed record Walk(IReadOnlyList<Type> Owners, List<(Type Type, string Origin)> Types, List<(MemberInfo Member, string Origin)> Members);

    /// <summary>Walks the owner types of an assembly.</summary>
    /// <param name="assembly">The assembly that holds the owners.</param>
    /// <param name="isOwner">Selects the owner types.</param>
    /// <returns>The walk.</returns>
    public static Walk Collect(Assembly assembly, Func<Type, bool> isOwner)
    {
        Module module = assembly.ManifestModule;
        var types = new List<(Type Type, string Origin)>();
        var members = new List<(MemberInfo Member, string Origin)>();
        Type[] owners = assembly.GetTypes()
            .Where(isOwner)
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToArray();

        using var stream = new FileStream(assembly.Location, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
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

        return new Walk(owners, types, members);
    }

    /// <summary>The types a type is built from: its array and by-ref element, and its generic definition and arguments.</summary>
    /// <param name="type">The type.</param>
    /// <returns>The leaf types.</returns>
    public static IEnumerable<Type> Leaves(Type type)
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

    /// <summary>Coverlet's injected tracker is instrumentation, not a dependency of the code under test (GW-U-2344, S-E009-001).</summary>
    /// <param name="fullName">The full name of the leaf type.</param>
    /// <returns>True when the type is the tracker.</returns>
    public static bool IsCoverletTracker(string fullName) =>
        fullName.StartsWith("Coverlet.Core.Instrumentation.Tracker.", StringComparison.Ordinal);

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
}
