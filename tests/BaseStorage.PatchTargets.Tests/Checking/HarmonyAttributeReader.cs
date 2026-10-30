namespace BaseStorage.PatchTargets.Tests.Checking;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

/// <summary>Resolves the type names that the Harmony attributes carry (contract 4.7.2).</summary>
internal interface ITypeNameResolver
{
    /// <summary>The type for a serialized name (nested types use <c>+</c>, arrays use <c>[]</c>), or null when unknown.</summary>
    Type? Resolve(string typeName);

    /// <summary>True when the type is defined in assembly_valheim.</summary>
    bool IsGameType(Type type);
}

/// <summary>The Harmony targets of one Plugin patch class. <c>Diagnostics</c> holds full BST2022 lines.</summary>
internal sealed record PatchClass(string ClassFullName, IReadOnlyList<string> TargetIds, IReadOnlyList<string> Diagnostics);

/// <summary>
/// Reads the Plugin's Harmony patch classes from metadata, without loading the assembly (contract 4.7.1, ADR-GWIN-015).
/// </summary>
internal static class HarmonyAttributeReader
{
    private const string PatchAttribute = "HarmonyLib.HarmonyPatch";
    private const string TypeText = "System.Type";
    private const string TypeArrayText = "System.Type[]";
    private const string StringText = "System.String";

    private const BindingFlags Declared = GameTargetChecker.Declared;

    /// <summary>
    /// Every class that directly carries <c>HarmonyLib.HarmonyPatch</c>. A null <paramref name="namespacePrefix"/> means no filter.
    /// </summary>
    public static IReadOnlyList<PatchClass> Read(string assemblyPath, string? namespacePrefix, ITypeNameResolver resolver)
    {
        using MemoryStream stream = new(File.ReadAllBytes(assemblyPath));
        using PEReader pe = new(stream);
        MetadataReader reader = pe.GetMetadataReader();
        List<PatchClass> result = [];

        foreach (TypeDefinitionHandle handle in reader.TypeDefinitions)
        {
            TypeDefinition type = reader.GetTypeDefinition(handle);
            bool isAbstract = (type.Attributes & TypeAttributes.Abstract) != 0;
            bool isSealed = (type.Attributes & TypeAttributes.Sealed) != 0;
            if (isAbstract && !isSealed)
            {
                continue;
            }

            string fullName = TypeNames.OfDefinition(reader, handle);
            if (namespacePrefix is not null && !fullName.StartsWith(namespacePrefix, StringComparison.Ordinal))
            {
                continue;
            }

            List<Info> classInfos = ReadPatchAttributes(reader, type.GetCustomAttributes(), out List<string> classProblems);
            if (classInfos.Count == 0 && classProblems.Count == 0)
            {
                continue;
            }

            result.Add(Resolve(reader, type, fullName, classInfos, classProblems, resolver));
        }

        return result.OrderBy(c => c.ClassFullName, StringComparer.Ordinal).ToList();
    }

    private static PatchClass Resolve(
        MetadataReader reader,
        TypeDefinition type,
        string className,
        List<Info> classInfos,
        List<string> classProblems,
        ITypeNameResolver resolver)
    {
        List<string> reasons = [.. classProblems];
        List<string> ids = [];

        if (DeclaresTargetMethod(reader, type))
        {
            reasons.Add("TargetMethod or TargetMethods is declared");
        }
        else
        {
            Info classLevel = Merge(new Info(), classInfos);
            List<Info> targets = [];
            foreach (MethodDefinitionHandle methodHandle in type.GetMethods())
            {
                MethodDefinition method = reader.GetMethodDefinition(methodHandle);
                List<Info> methodInfos = ReadPatchAttributes(reader, method.GetCustomAttributes(), out List<string> methodProblems);
                reasons.AddRange(methodProblems);
                if (methodInfos.Count > 0)
                {
                    targets.Add(Merge(classLevel, methodInfos));
                }
            }

            if (targets.Count == 0 && classProblems.Count == 0)
            {
                targets.Add(classLevel);
            }

            foreach (Info target in targets)
            {
                string? id = TargetId(target, resolver, reasons);
                if (id is not null)
                {
                    ids.Add(id);
                }
            }
        }

        string[] diagnostics = reasons
            .Distinct(StringComparer.Ordinal)
            .Select(r => "BST2022: Plugin patch class '" + className + "' has a Harmony target this check cannot resolve: " + r + ".")
            .ToArray();

        return new PatchClass(className, ids.Distinct(StringComparer.Ordinal).OrderBy(i => i, StringComparer.Ordinal).ToArray(), diagnostics);
    }

    private static string? TargetId(Info target, ITypeNameResolver resolver, List<string> reasons)
    {
        if (target.DeclaringType is null)
        {
            reasons.Add("declaring type is not set");
            return null;
        }

        if (target.MethodName is null)
        {
            reasons.Add("method name is not set");
            return null;
        }

        string declaringName = target.DeclaringType;
        Type? declaring = resolver.Resolve(declaringName);
        if (declaring is null || !resolver.IsGameType(declaring))
        {
            reasons.Add("declaring type '" + declaringName + "' is not in assembly_valheim");
            return null;
        }

        string typeFullName = declaring.FullName ?? declaringName;
        IReadOnlyList<string> arguments;
        if (target.ArgumentTypes is not null)
        {
            arguments = target.ArgumentTypes;
        }
        else
        {
            MethodInfo[] candidates = declaring.GetMethods(Declared).Where(m => m.Name == target.MethodName).ToArray();
            if (candidates.Length != 1)
            {
                reasons.Add("method '" + typeFullName + "." + target.MethodName + "' has " + candidates.Length + " declared overloads and no argument types are given");
                return null;
            }

            arguments = candidates[0].GetParameters().Select(p => p.ParameterType.FullName ?? string.Empty).ToArray();
        }

        return typeFullName + "." + target.MethodName + "(" + string.Join(",", arguments) + ")";
    }

    private static bool DeclaresTargetMethod(MetadataReader reader, TypeDefinition type)
    {
        foreach (MethodDefinitionHandle handle in type.GetMethods())
        {
            MethodDefinition method = reader.GetMethodDefinition(handle);
            string name = reader.GetString(method.Name);
            if (name is "TargetMethod" or "TargetMethods")
            {
                return true;
            }

            foreach (CustomAttributeHandle attribute in method.GetCustomAttributes())
            {
                string attributeName = TypeNames.OfAttribute(reader, reader.GetCustomAttribute(attribute));
                if (attributeName is "HarmonyLib.HarmonyTargetMethod" or "HarmonyLib.HarmonyTargetMethods")
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static Info Merge(Info baseInfo, IEnumerable<Info> overrides)
    {
        Info merged = baseInfo;
        foreach (Info next in overrides)
        {
            merged = new Info
            {
                DeclaringType = next.DeclaringType ?? merged.DeclaringType,
                MethodName = next.MethodName ?? merged.MethodName,
                ArgumentTypes = next.ArgumentTypes ?? merged.ArgumentTypes,
            };
        }

        return merged;
    }

    private static List<Info> ReadPatchAttributes(MetadataReader reader, CustomAttributeHandleCollection handles, out List<string> problems)
    {
        List<Info> infos = [];
        problems = [];
        AttributeTypeProvider provider = new();

        foreach (CustomAttributeHandle handle in handles)
        {
            CustomAttribute attribute = reader.GetCustomAttribute(handle);
            if (TypeNames.OfAttribute(reader, attribute) != PatchAttribute)
            {
                continue;
            }

            CustomAttributeValue<string> value = attribute.DecodeValue(provider);
            string[] shape = value.FixedArguments.Select(a => a.Type).ToArray();
            Info info = new();

            if (shape.Length == 0)
            {
                // [HarmonyPatch()] contributes nothing; the info stays empty and other attributes supply the target.
            }
            else if (Is(shape, TypeText))
            {
                info.DeclaringType = TypeNames.Strip((string)value.FixedArguments[0].Value!);
            }
            else if (Is(shape, TypeText, StringText))
            {
                info.DeclaringType = TypeNames.Strip((string)value.FixedArguments[0].Value!);
                info.MethodName = (string?)value.FixedArguments[1].Value;
            }
            else if (Is(shape, TypeText, StringText, TypeArrayText))
            {
                info.DeclaringType = TypeNames.Strip((string)value.FixedArguments[0].Value!);
                info.MethodName = (string?)value.FixedArguments[1].Value;
                info.ArgumentTypes = TypeList(value.FixedArguments[2]);
            }
            else if (Is(shape, StringText))
            {
                info.MethodName = (string?)value.FixedArguments[0].Value;
            }
            else if (Is(shape, StringText, TypeArrayText))
            {
                info.MethodName = (string?)value.FixedArguments[0].Value;
                info.ArgumentTypes = TypeList(value.FixedArguments[1]);
            }
            else if (Is(shape, TypeArrayText))
            {
                info.ArgumentTypes = TypeList(value.FixedArguments[0]);
            }
            else
            {
                problems.Add("unsupported HarmonyPatch constructor (" + string.Join(", ", shape) + ")");
                continue;
            }

            infos.Add(info);
        }

        return infos;
    }

    private static bool Is(string[] actual, params string[] expected) =>
        actual.Length == expected.Length && actual.Zip(expected).All(pair => pair.First == pair.Second);

    private static string[]? TypeList(CustomAttributeTypedArgument<string> argument)
    {
        if (argument.Value is not ImmutableArray<CustomAttributeTypedArgument<string>> items)
        {
            return null;
        }

        return items.Select(i => TypeNames.Strip((string)i.Value!)).ToArray();
    }

    private sealed class Info
    {
        public string? DeclaringType { get; set; }

        public string? MethodName { get; set; }

        public string[]? ArgumentTypes { get; set; }
    }

    private static class TypeNames
    {
        public static string OfAttribute(MetadataReader reader, CustomAttribute attribute)
        {
            switch (attribute.Constructor.Kind)
            {
                case HandleKind.MethodDefinition:
                    MethodDefinition method = reader.GetMethodDefinition((MethodDefinitionHandle)attribute.Constructor);
                    return OfDefinition(reader, method.GetDeclaringType());
                case HandleKind.MemberReference:
                    MemberReference member = reader.GetMemberReference((MemberReferenceHandle)attribute.Constructor);
                    return member.Parent.Kind switch
                    {
                        HandleKind.TypeReference => OfReference(reader, (TypeReferenceHandle)member.Parent),
                        HandleKind.TypeDefinition => OfDefinition(reader, (TypeDefinitionHandle)member.Parent),
                        _ => string.Empty,
                    };
                default:
                    return string.Empty;
            }
        }

        public static string OfDefinition(MetadataReader reader, TypeDefinitionHandle handle)
        {
            TypeDefinition type = reader.GetTypeDefinition(handle);
            string name = reader.GetString(type.Name);
            TypeDefinitionHandle declaring = type.GetDeclaringType();
            if (!declaring.IsNil)
            {
                return OfDefinition(reader, declaring) + "+" + name;
            }

            string ns = reader.GetString(type.Namespace);
            return ns.Length == 0 ? name : ns + "." + name;
        }

        public static string OfReference(MetadataReader reader, TypeReferenceHandle handle)
        {
            TypeReference type = reader.GetTypeReference(handle);
            string name = reader.GetString(type.Name);
            if (type.ResolutionScope.Kind == HandleKind.TypeReference)
            {
                return OfReference(reader, (TypeReferenceHandle)type.ResolutionScope) + "+" + name;
            }

            string ns = reader.GetString(type.Namespace);
            return ns.Length == 0 ? name : ns + "." + name;
        }

        /// <summary>
        /// Removes the assembly qualification of a serialized type name. Known limit: it cuts at the first comma, so
        /// generic or assembly-qualified generic names (for example <c>List`1[[A, asm]], asm</c>) are not supported;
        /// Harmony patch targets in this mod use plain, non-generic game types.
        /// </summary>
        public static string Strip(string serialized)
        {
            int comma = serialized.IndexOf(',', StringComparison.Ordinal);
            return comma < 0 ? serialized : serialized[..comma].Trim();
        }
    }

    private sealed class AttributeTypeProvider : ICustomAttributeTypeProvider<string>
    {
        public string GetPrimitiveType(PrimitiveTypeCode typeCode) => "System." + typeCode;

        public string GetSystemType() => TypeText;

        public string GetSZArrayType(string elementType) => elementType + "[]";

        public string GetTypeFromDefinition(MetadataReader metadataReader, TypeDefinitionHandle handle, byte rawTypeKind) =>
            TypeNames.OfDefinition(metadataReader, handle);

        public string GetTypeFromReference(MetadataReader metadataReader, TypeReferenceHandle handle, byte rawTypeKind) =>
            TypeNames.OfReference(metadataReader, handle);

        public string GetTypeFromSerializedName(string name) => name;

        public PrimitiveTypeCode GetUnderlyingEnumType(string type) => PrimitiveTypeCode.Int32;

        public bool IsSystemType(string type) => type == TypeText;
    }
}
