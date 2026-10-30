namespace BaseStorage.Core.Tests.PerFrame;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using BaseStorage.Core.Availability;
using BaseStorage.Core.Bases;
using BaseStorage.Core.Bases.Membership;
using BaseStorage.Core.Bases.Resolution;
using BaseStorage.Core.Categories.AutoAssign;
using BaseStorage.Core.Organize;
using BaseStorage.Core.Organize.Deposit;
using BaseStorage.Core.Tests.PatchGuard;

/// <summary>One forbidden callee: declaring type full name (+ for nested), simple method name, and its category.</summary>
/// <param name="TypeFullName">The declaring type full name.</param>
/// <param name="MethodName">The simple method name.</param>
/// <param name="Category">The category: base resolver, cache rebuild or planner.</param>
public sealed record PerFrameForbiddenCall(string TypeFullName, string MethodName, string Category);

/// <summary>One direct call from an audited frame method to a forbidden callee.</summary>
/// <param name="Method">Audited method as DeclaringTypeFullName + "." + name, for example BaseStorage.Plugin.BaseStoragePlugin.Update.</param>
/// <param name="Callee">Forbidden callee as TypeFullName + "." + MethodName.</param>
/// <param name="Category">"base resolver", "cache rebuild" or "planner".</param>
public sealed record PerFrameViolation(string Method, string Callee, string Category);

/// <summary>The audited methods and the violations, both in ordinal order.</summary>
/// <param name="AuditedMethods">Every audited frame method as Type.Method.</param>
/// <param name="Violations">Every violation.</param>
public sealed record PerFrameAuditResult(IReadOnlyList<string> AuditedMethods, IReadOnlyList<PerFrameViolation> Violations);

/// <summary>Metadata-only IL audit of Update/LateUpdate methods (AC-9.2). Never loads the assembly. Not a test class.</summary>
public static class PerFrameAuditScanner
{
    private const string PluginSnapshotSource = "BaseStorage.Plugin.World.GameSnapshotSource";

    /// <summary>Gets the frame method names: exactly "Update" and "LateUpdate".</summary>
    public static IReadOnlyList<string> FrameMethodNames { get; } = new[] { "Update", "LateUpdate" };

    /// <summary>Gets the forbidden callees of contract table 3.3, in table order.</summary>
    public static IReadOnlyList<PerFrameForbiddenCall> ForbiddenCalls { get; } = new PerFrameForbiddenCall[]
    {
        new(typeof(BaseResolver).FullName!, nameof(BaseResolver.Resolve), "base resolver"),
        new(typeof(StationGraph).FullName!, nameof(StationGraph.FindBase), "base resolver"),
        new(typeof(BaseMembership).FullName!, nameof(BaseMembership.FindMembers), "base resolver"),
        new(typeof(BaseCache).FullName!, nameof(BaseCache.Resolve), "cache rebuild"),
        new(typeof(IBaseSnapshotSource).FullName!, nameof(IBaseSnapshotSource.GetStations), "cache rebuild"),
        new(typeof(IBaseSnapshotSource).FullName!, nameof(IBaseSnapshotSource.GetContainers), "cache rebuild"),
        new(PluginSnapshotSource, "GetStations", "cache rebuild"),
        new(PluginSnapshotSource, "GetContainers", "cache rebuild"),
        new(typeof(OrganizePlanner).FullName!, nameof(OrganizePlanner.Plan), "planner"),
        new(typeof(DepositPlanner).FullName!, nameof(DepositPlanner.Plan), "planner"),
        new(typeof(ConsumptionPlanner).FullName!, nameof(ConsumptionPlanner.Plan), "planner"),
        new(typeof(AutoAssigner).FullName!, nameof(AutoAssigner.Resolve), "planner"),
    };

    /// <summary>Audits every frame method of every type whose full name starts with the prefix (null: every type).</summary>
    /// <param name="assemblyPath">The path of the assembly. It is read, never loaded.</param>
    /// <param name="typeNamespacePrefix">Only types whose full name starts with this prefix are audited. Null audits every type.</param>
    /// <returns>The audited methods and the violations.</returns>
    public static PerFrameAuditResult Scan(string assemblyPath, string? typeNamespacePrefix)
    {
        using var stream = new FileStream(assemblyPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var pe = new PEReader(stream);
        MetadataReader metadata = pe.GetMetadataReader();

        var audited = new SortedSet<string>(StringComparer.Ordinal);
        var violations = new SortedSet<PerFrameViolation>(Comparer<PerFrameViolation>.Create(CompareViolations));
        foreach (TypeDefinitionHandle typeHandle in metadata.TypeDefinitions)
        {
            string typeName = TypeDefinitionFullName(metadata, typeHandle);
            if (typeNamespacePrefix is not null && !typeName.StartsWith(typeNamespacePrefix, StringComparison.Ordinal))
            {
                continue;
            }

            foreach (MethodDefinitionHandle methodHandle in metadata.GetTypeDefinition(typeHandle).GetMethods())
            {
                MethodDefinition method = metadata.GetMethodDefinition(methodHandle);
                string name = metadata.GetString(method.Name);
                if (method.RelativeVirtualAddress == 0 || !FrameMethodNames.Contains(name, StringComparer.Ordinal))
                {
                    continue;
                }

                string full = typeName + "." + name;
                audited.Add(full);
                foreach (IlInstruction instruction in IlInstructionReader.Decode(pe, method.RelativeVirtualAddress))
                {
                    if (instruction.OpCode.Name is not ("call" or "callvirt"))
                    {
                        continue;
                    }

                    (string? owner, string callee) = MemberOwnerAndName(metadata, MetadataTokens.EntityHandle(instruction.Token));
                    PerFrameForbiddenCall? hit = ForbiddenCalls.FirstOrDefault(
                        f => string.Equals(f.TypeFullName, owner, StringComparison.Ordinal) && string.Equals(f.MethodName, callee, StringComparison.Ordinal));
                    if (hit is not null)
                    {
                        violations.Add(new PerFrameViolation(full, hit.TypeFullName + "." + hit.MethodName, hit.Category));
                    }
                }
            }
        }

        return new PerFrameAuditResult(audited.ToList(), violations.ToList());
    }

    /// <summary>Builds the failure message of contract 3.6.</summary>
    /// <param name="result">The audit result.</param>
    /// <returns>The header line then one line per violation, joined with a line feed.</returns>
    public static string FormatViolations(PerFrameAuditResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        var lines = new List<string>
        {
            "Per-frame audit scanned " + result.AuditedMethods.Count + " Update/LateUpdate methods and found " + result.Violations.Count + " violations.",
        };
        lines.AddRange(result.Violations.Select(
            v => v.Method + " calls " + v.Callee + " (" + v.Category + ") directly; move it out of the frame loop (AC-9.2)."));
        return string.Join("\n", lines);
    }

    private static int CompareViolations(PerFrameViolation? a, PerFrameViolation? b)
    {
        int byMethod = string.CompareOrdinal(a!.Method, b!.Method);
        return byMethod != 0 ? byMethod : string.CompareOrdinal(a.Callee, b.Callee);
    }

    private static (string? Owner, string Name) MemberOwnerAndName(MetadataReader metadata, EntityHandle handle)
    {
        switch (handle.Kind)
        {
            case HandleKind.MethodDefinition:
                {
                    MethodDefinition method = metadata.GetMethodDefinition((MethodDefinitionHandle)handle);
                    return (TypeDefinitionFullName(metadata, method.GetDeclaringType()), metadata.GetString(method.Name));
                }

            case HandleKind.MemberReference:
                {
                    MemberReference reference = metadata.GetMemberReference((MemberReferenceHandle)handle);
                    return (ParentTypeFullName(metadata, reference.Parent), metadata.GetString(reference.Name));
                }

            case HandleKind.MethodSpecification:
                return MemberOwnerAndName(metadata, metadata.GetMethodSpecification((MethodSpecificationHandle)handle).Method);

            default:
                return (null, string.Empty);
        }
    }

    private static string? ParentTypeFullName(MetadataReader metadata, EntityHandle parent)
    {
        switch (parent.Kind)
        {
            case HandleKind.TypeReference:
                return TypeReferenceFullName(metadata, (TypeReferenceHandle)parent);
            case HandleKind.TypeDefinition:
                return TypeDefinitionFullName(metadata, (TypeDefinitionHandle)parent);
            case HandleKind.TypeSpecification:
                return metadata.GetTypeSpecification((TypeSpecificationHandle)parent).DecodeSignature(NameOnlyProvider.Instance, null);
            default:
                return null;
        }
    }

    private static string TypeDefinitionFullName(MetadataReader metadata, TypeDefinitionHandle handle)
    {
        TypeDefinition type = metadata.GetTypeDefinition(handle);
        string name = metadata.GetString(type.Name);
        TypeDefinitionHandle declaring = type.GetDeclaringType();
        if (!declaring.IsNil)
        {
            return TypeDefinitionFullName(metadata, declaring) + "+" + name;
        }

        string ns = metadata.GetString(type.Namespace);
        return ns.Length == 0 ? name : ns + "." + name;
    }

    private static string TypeReferenceFullName(MetadataReader metadata, TypeReferenceHandle handle)
    {
        TypeReference reference = metadata.GetTypeReference(handle);
        string name = metadata.GetString(reference.Name);
        if (reference.ResolutionScope.Kind == HandleKind.TypeReference)
        {
            return TypeReferenceFullName(metadata, (TypeReferenceHandle)reference.ResolutionScope) + "+" + name;
        }

        string ns = metadata.GetString(reference.Namespace);
        return ns.Length == 0 ? name : ns + "." + name;
    }

    /// <summary>Renders type specifications as names only (generic arguments and array shapes are dropped).</summary>
    private sealed class NameOnlyProvider : ISignatureTypeProvider<string, object?>
    {
        public static readonly NameOnlyProvider Instance = new();

        public string GetArrayType(string elementType, ArrayShape shape) => elementType + "[]";

        public string GetByReferenceType(string elementType) => elementType + "&";

        public string GetFunctionPointerType(MethodSignature<string> signature) => "fnptr";

        public string GetGenericInstantiation(string genericType, ImmutableArray<string> typeArguments) => genericType;

        public string GetGenericMethodParameter(object? genericContext, int index) => "!!" + index.ToString();

        public string GetGenericTypeParameter(object? genericContext, int index) => "!" + index.ToString();

        public string GetModifiedType(string modifier, string unmodifiedType, bool isRequired) => unmodifiedType;

        public string GetPinnedType(string elementType) => elementType;

        public string GetPointerType(string elementType) => elementType + "*";

        public string GetPrimitiveType(PrimitiveTypeCode typeCode) => typeCode.ToString();

        public string GetSZArrayType(string elementType) => elementType + "[]";

        public string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) =>
            TypeDefinitionFullName(reader, handle);

        public string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) =>
            TypeReferenceFullName(reader, handle);

        public string GetTypeFromSpecification(MetadataReader reader, object? genericContext, TypeSpecificationHandle handle, byte rawTypeKind) =>
            reader.GetTypeSpecification(handle).DecodeSignature(this, genericContext);
    }
}
