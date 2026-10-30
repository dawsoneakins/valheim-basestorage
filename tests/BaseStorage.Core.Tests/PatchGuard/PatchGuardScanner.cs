namespace BaseStorage.Core.Tests.PatchGuard;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text;

/// <summary>What the guard checks an assembly against (ADR-GWIN-021).</summary>
/// <param name="AttributeNamespace">The namespace of the Harmony attributes, for example HarmonyLib.</param>
/// <param name="HelperTypeFullName">The full name of the helper type, for example BaseStorage.Plugin.Patching.PatchSafety.</param>
/// <param name="TypeNamespacePrefix">Only types whose full name starts with this prefix are scanned. Null scans every type.</param>
/// <param name="GateMethodFullName">
/// The optional gate (ADR-GWIN-136, contract 8.3 errata 1), owner plus name, for example BaseStorage.Plugin.Patching.CraftFromBaseGate.IsOpen.
/// When a patch body's first non-nop instruction calls it, the body is a gated wrapper: the guard allows that gate call and one call to
/// the same-type &lt;wrapper name&gt;Core, and G1-G3 are evaluated on the Core IL. Null keeps the ungated rules.
/// </param>
public sealed record PatchGuardRules(string AttributeNamespace, string HelperTypeFullName, string? TypeNamespacePrefix = null, string? GateMethodFullName = null);

/// <summary>One rule broken by one patch body.</summary>
/// <param name="Method">The patch body as declaring type full name (with + for nested types), a dot and the method name.</param>
/// <param name="Rule">G1, G2 or G3.</param>
/// <param name="Reason">The human-readable reason.</param>
public sealed record PatchGuardViolation(string Method, string Rule, string Reason);

/// <summary>The patch bodies found and the violations, both in ordinal order.</summary>
/// <param name="PatchBodies">Every discovered patch body as Type.Method.</param>
/// <param name="Violations">Every violation.</param>
public sealed record PatchGuardResult(IReadOnlyList<string> PatchBodies, IReadOnlyList<PatchGuardViolation> Violations);

/// <summary>
/// Metadata scanner over a built assembly (ADR-GWIN-021). It finds patch bodies and checks G1 (routes through the helper),
/// G2 (nothing outside the helper) and G3 (the key identifies the patch). It never loads the assembly.
/// Not a test class.
/// </summary>
public static class PatchGuardScanner
{
    private static readonly string[] HelperMethods = { "Run", "RunPrefix", "RunTranspiler" };
    private static readonly string[] PatchNames = { "Prefix", "Postfix", "Transpiler", "Finalizer", "ILManipulator" };
    private static readonly string[] PatchAttributes = { "HarmonyPrefix", "HarmonyPostfix", "HarmonyTranspiler", "HarmonyFinalizer", "HarmonyILManipulator" };
    private static readonly string[] ExcludedAttributes = { "HarmonyReversePatch", "HarmonyPrepare", "HarmonyCleanup", "HarmonyTargetMethod", "HarmonyTargetMethods" };
    private static readonly string[] ExcludedNames = { "Prepare", "Cleanup", "TargetMethod", "TargetMethods" };
    private static readonly string[] PlainOpcodes =
    {
        "nop", "ldnull", "ldstr", "dup", "pop", "ret", "br", "br.s", "brtrue", "brtrue.s", "brfalse", "brfalse.s", "ldobj", "stobj", "ldftn",
    };
    private static readonly string[] PlainOpcodePrefixes = { "ldarg", "ldarga", "starg", "ldloc", "ldloca", "stloc", "ldc.", "ldind.", "stind." };

    /// <summary>Finds every patch body in the assembly and checks it against the rules. The assembly is read, never loaded.</summary>
    /// <param name="assemblyPath">The path of the assembly.</param>
    /// <param name="rules">The rules to apply.</param>
    /// <returns>The patch bodies and the violations.</returns>
    public static PatchGuardResult Scan(string assemblyPath, PatchGuardRules rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        using FileStream stream = Open(assemblyPath);
        using var pe = new PEReader(stream);
        MetadataReader metadata = pe.GetMetadataReader();

        var bodies = new List<string>();
        var violations = new List<PatchGuardViolation>();
        string? prefix = rules.TypeNamespacePrefix;
        foreach (TypeDefinitionHandle typeHandle in metadata.TypeDefinitions)
        {
            string typeName = TypeDefinitionFullName(metadata, typeHandle);
            if (prefix is not null && !typeName.StartsWith(prefix, StringComparison.Ordinal))
            {
                continue;
            }

            TypeDefinition type = metadata.GetTypeDefinition(typeHandle);
            bool patchClass = AttributeNames(metadata, type.GetCustomAttributes(), rules.AttributeNamespace)
                .Any(name => name.StartsWith("HarmonyPatch", StringComparison.Ordinal));

            foreach (MethodDefinitionHandle methodHandle in type.GetMethods())
            {
                MethodDefinition method = metadata.GetMethodDefinition(methodHandle);
                string methodName = metadata.GetString(method.Name);
                string? kind = PatchKind(metadata, method, methodName, patchClass, rules.AttributeNamespace);
                if (kind is null)
                {
                    continue;
                }

                bodies.Add(typeName + "." + methodName);
                CheckBody(pe, metadata, type, typeName, method, typeName + "." + methodName, kind, rules, violations);
            }
        }

        IReadOnlyList<PatchGuardViolation> sorted = violations
            .OrderBy(violation => violation.Method, StringComparer.Ordinal)
            .ThenBy(violation => violation.Rule, StringComparer.Ordinal)
            .ToArray();
        return new PatchGuardResult(bodies.OrderBy(name => name, StringComparer.Ordinal).ToArray(), sorted);
    }

    /// <summary>Builds the failure message: a header with the counts, then one ordinal-sorted line per violation.</summary>
    /// <param name="result">The scan result.</param>
    /// <returns>The message, with lines separated by LF.</returns>
    public static string FormatViolations(PatchGuardResult result)
    {
        var builder = new StringBuilder();
        builder.Append($"Patch guard scanned {result.PatchBodies.Count} patch bodies and found {result.Violations.Count} violations.");
        foreach (string line in result.Violations.Select(violation => violation.Method + ": " + violation.Reason).OrderBy(text => text, StringComparer.Ordinal))
        {
            builder.Append('\n').Append(line);
        }

        return builder.ToString();
    }

    /// <summary>The names of the methods declared by one type, read from metadata.</summary>
    /// <param name="assemblyPath">The path of the assembly.</param>
    /// <param name="typeFullName">The full name of the type, with + for nested types.</param>
    /// <returns>The method names.</returns>
    public static IReadOnlyList<string> MethodNamesOf(string assemblyPath, string typeFullName)
    {
        using FileStream stream = Open(assemblyPath);
        using var pe = new PEReader(stream);
        MetadataReader metadata = pe.GetMetadataReader();

        var names = new List<string>();
        foreach (TypeDefinitionHandle typeHandle in metadata.TypeDefinitions)
        {
            if (TypeDefinitionFullName(metadata, typeHandle) != typeFullName)
            {
                continue;
            }

            foreach (MethodDefinitionHandle methodHandle in metadata.GetTypeDefinition(typeHandle).GetMethods())
            {
                names.Add(metadata.GetString(metadata.GetMethodDefinition(methodHandle).Name));
            }
        }

        return names;
    }

    private static FileStream Open(string path) => new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

    private static void CheckBody(
        PEReader pe,
        MetadataReader metadata,
        TypeDefinition type,
        string typeName,
        MethodDefinition method,
        string subject,
        string kind,
        PatchGuardRules rules,
        List<PatchGuardViolation> violations)
    {
        IReadOnlyList<IlInstruction> il = DecodeBody(pe, method);
        int gateIndex = GateCallIndex(metadata, il, rules.GateMethodFullName);
        if (gateIndex < 0)
        {
            CheckRules(metadata, il, subject, kind, rules, violations);
            return;
        }

        // Gated wrapper (ADR-GWIN-136): G2 on the wrapper allows the gate call and exactly one call to the same-type <name>Core.
        // G1, G2 and G3 are then evaluated on the Core IL, with the subject and key of the wrapper.
        string coreName = metadata.GetString(method.Name) + "Core";
        int coreIndex = CoreCallIndex(metadata, il, typeName, coreName);
        for (int index = 0; index < il.Count; index++)
        {
            if (index == gateIndex || index == coreIndex)
            {
                continue;
            }

            string? reason = OutsideReason(metadata, il[index], rules.HelperTypeFullName);
            if (reason is not null)
            {
                violations.Add(new PatchGuardViolation(subject, "G2", reason));
                break;
            }
        }

        MethodDefinition? core = coreIndex < 0 ? null : FindMethod(metadata, type, coreName);
        if (core is null)
        {
            violations.Add(new PatchGuardViolation(subject, "G1", "does not call PatchSafety"));
            return;
        }

        CheckRules(metadata, DecodeBody(pe, core.Value), subject, kind, rules, violations);
    }

    private static IReadOnlyList<IlInstruction> DecodeBody(PEReader pe, MethodDefinition method) =>
        method.RelativeVirtualAddress == 0
            ? Array.Empty<IlInstruction>()
            : IlInstructionReader.Decode(pe, method.RelativeVirtualAddress);

    /// <summary>The index of the first non-nop instruction when it is a call to the gate, otherwise -1.</summary>
    private static int GateCallIndex(MetadataReader metadata, IReadOnlyList<IlInstruction> il, string? gate)
    {
        if (gate is null)
        {
            return -1;
        }

        int index = 0;
        while (index < il.Count && il[index].OpCode.Name == "nop")
        {
            index++;
        }

        return index < il.Count && il[index].OpCode.Name == "call" && TokenTarget(metadata, il[index].Token) == gate ? index : -1;
    }

    /// <summary>The index of the first call to the same-type method named coreName, otherwise -1.</summary>
    private static int CoreCallIndex(MetadataReader metadata, IReadOnlyList<IlInstruction> il, string typeName, string coreName)
    {
        for (int index = 0; index < il.Count; index++)
        {
            if (il[index].OpCode.Name != "call" || il[index].Token == 0)
            {
                continue;
            }

            (string? owner, string name) = MemberOwnerAndName(metadata, EntityOf(il[index].Token));
            if (owner == typeName && name == coreName)
            {
                return index;
            }
        }

        return -1;
    }

    private static MethodDefinition? FindMethod(MetadataReader metadata, TypeDefinition type, string name)
    {
        foreach (MethodDefinitionHandle handle in type.GetMethods())
        {
            MethodDefinition candidate = metadata.GetMethodDefinition(handle);
            if (metadata.GetString(candidate.Name) == name)
            {
                return candidate;
            }
        }

        return null;
    }

    private static void CheckRules(
        MetadataReader metadata,
        IReadOnlyList<IlInstruction> il,
        string subject,
        string kind,
        PatchGuardRules rules,
        List<PatchGuardViolation> violations)
    {
        // G1: the body calls the helper (a transpiler must call RunTranspiler).
        var helperCalls = il
            .Where(instruction => instruction.OpCode.Name == "call" && IsHelperCall(metadata, instruction.Token, rules.HelperTypeFullName))
            .Select(instruction => MemberOwnerAndName(metadata, EntityOf(instruction.Token)).Name)
            .ToList();
        bool transpiler = kind == "Transpiler";
        bool callsHelper = helperCalls.Count > 0;
        bool callsRunOrRunPrefix = helperCalls.Any(name => name is "Run" or "RunPrefix");
        bool callsRunTranspiler = helperCalls.Contains("RunTranspiler");
        if (!callsHelper || (!transpiler && !callsRunOrRunPrefix))
        {
            violations.Add(new PatchGuardViolation(subject, "G1", "does not call PatchSafety"));
        }
        else if (transpiler && !callsRunTranspiler)
        {
            violations.Add(new PatchGuardViolation(subject, "G1", "transpiler must use PatchSafety.RunTranspiler"));
        }

        // G2: every instruction is on the allow-list. One violation per body is enough to fail it.
        foreach (IlInstruction instruction in il)
        {
            string? reason = OutsideReason(metadata, instruction, rules.HelperTypeFullName);
            if (reason is not null)
            {
                violations.Add(new PatchGuardViolation(subject, "G2", reason));
                break;
            }
        }

        // G3: the body passes its own key as a string literal.
        bool keyed = il.Any(instruction => instruction.OpCode.Name == "ldstr" && UserString(metadata, instruction.Token) == subject);
        if (!keyed)
        {
            violations.Add(new PatchGuardViolation(subject, "G3", "does not pass patch key '" + subject + "'"));
        }
    }

    private static string? OutsideReason(MetadataReader metadata, IlInstruction instruction, string helper)
    {
        string name = instruction.OpCode.Name ?? string.Empty;
        if (IsPlain(name))
        {
            return null;
        }

        bool allowed = name switch
        {
            "ldsfld" or "stsfld" => SimpleName(MemberOwnerAndName(metadata, EntityOf(instruction.Token)).Owner) == "<>c",
            "ldfld" or "stfld" => SimpleName(MemberOwnerAndName(metadata, EntityOf(instruction.Token)).Owner).StartsWith("<>c__DisplayClass", StringComparison.Ordinal),
            "newobj" => ConstructorAllowed(metadata, instruction.Token, helper),
            "call" => IsHelperCall(metadata, instruction.Token, helper),
            _ => false,
        };
        if (allowed)
        {
            return null;
        }

        string target = TokenTarget(metadata, instruction.Token);
        return "uses " + name + (target.Length == 0 ? string.Empty : " " + target) + " outside PatchSafety";
    }

    private static bool IsPlain(string name) =>
        PlainOpcodes.Contains(name) || PlainOpcodePrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal));

    private static bool ConstructorAllowed(MetadataReader metadata, int token, string helper)
    {
        string? owner = MemberOwnerAndName(metadata, EntityOf(token)).Owner;
        if (owner is null)
        {
            return false;
        }

        return owner.StartsWith("System.Action", StringComparison.Ordinal)
            || owner.StartsWith("System.Func", StringComparison.Ordinal)
            || owner.StartsWith(helper + "+", StringComparison.Ordinal)
            || SimpleName(owner).StartsWith("<>c__DisplayClass", StringComparison.Ordinal);
    }

    private static bool IsHelperCall(MetadataReader metadata, int token, string helper)
    {
        (string? owner, string name) = MemberOwnerAndName(metadata, EntityOf(token));
        return owner == helper && HelperMethods.Contains(name);
    }

    private static string TokenTarget(MetadataReader metadata, int token)
    {
        if (token == 0)
        {
            return string.Empty;
        }

        (string? owner, string name) = MemberOwnerAndName(metadata, EntityOf(token));
        return owner is null ? name : owner + "." + name;
    }

    private static string? PatchKind(MetadataReader metadata, MethodDefinition method, string name, bool patchClass, string attributeNamespace)
    {
        if (name.StartsWith("<", StringComparison.Ordinal))
        {
            return null;
        }

        string[] attributes = AttributeNames(metadata, method.GetCustomAttributes(), attributeNamespace);
        if (attributes.Any(attribute => ExcludedAttributes.Contains(attribute)) || ExcludedNames.Contains(name))
        {
            return null;
        }

        foreach (string attribute in attributes)
        {
            if (PatchAttributes.Contains(attribute))
            {
                return attribute.Substring("Harmony".Length);
            }
        }

        if (!PatchNames.Contains(name))
        {
            return null;
        }

        bool marked = patchClass || attributes.Any(attribute => attribute.StartsWith("HarmonyPatch", StringComparison.Ordinal));
        return marked ? name : null;
    }

    private static string[] AttributeNames(MetadataReader metadata, CustomAttributeHandleCollection handles, string attributeNamespace)
    {
        string prefix = attributeNamespace + ".";
        var names = new List<string>();
        foreach (CustomAttributeHandle handle in handles)
        {
            CustomAttribute attribute = metadata.GetCustomAttribute(handle);
            string? full = DeclaringTypeFullName(metadata, attribute.Constructor);
            if (full is not null && full.StartsWith(prefix, StringComparison.Ordinal))
            {
                names.Add(full.Substring(prefix.Length));
            }
        }

        return names.ToArray();
    }

    private static EntityHandle EntityOf(int token) => MetadataTokens.EntityHandle(token);

    private static string UserString(MetadataReader metadata, int token) =>
        metadata.GetUserString(MetadataTokens.UserStringHandle(token & 0x00FFFFFF));

    /// <summary>The declaring type and the simple name of a field or method reference, resolved by namespace and name.</summary>
    private static (string? Owner, string Name) MemberOwnerAndName(MetadataReader metadata, EntityHandle handle)
    {
        switch (handle.Kind)
        {
            case HandleKind.MethodDefinition:
                {
                    MethodDefinition method = metadata.GetMethodDefinition((MethodDefinitionHandle)handle);
                    return (TypeDefinitionFullName(metadata, method.GetDeclaringType()), metadata.GetString(method.Name));
                }

            case HandleKind.FieldDefinition:
                {
                    FieldDefinition field = metadata.GetFieldDefinition((FieldDefinitionHandle)handle);
                    return (TypeDefinitionFullName(metadata, field.GetDeclaringType()), metadata.GetString(field.Name));
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

    private static string? DeclaringTypeFullName(MetadataReader metadata, EntityHandle handle) =>
        MemberOwnerAndName(metadata, handle).Owner;

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

    private static string SimpleName(string? fullName)
    {
        if (fullName is null)
        {
            return string.Empty;
        }

        int index = Math.Max(fullName.LastIndexOf('+'), fullName.LastIndexOf('.'));
        return index < 0 ? fullName : fullName.Substring(index + 1);
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
