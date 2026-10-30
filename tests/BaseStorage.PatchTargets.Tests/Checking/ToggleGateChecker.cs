namespace BaseStorage.PatchTargets.Tests.Checking;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

/// <summary>What counts as an E3 patch and what the gate is (contract craft-from-base-toggle 7.1).</summary>
internal sealed record ToggleGateRules(
    IReadOnlyList<string> Namespaces,
    string MarkerAttribute,
    string GateType,
    string GateMethod,
    IReadOnlyList<string> ExpectedClasses)
{
    /// <summary>The rules of the real Plugin (contract craft-from-base-toggle section 2).</summary>
    public static ToggleGateRules Plugin { get; } = new(
        ["BaseStorage.Plugin.Building", "BaseStorage.Plugin.Crafting", "BaseStorage.Plugin.Requirements"],
        "BaseStorage.Plugin.Patching.CraftFromBasePatchAttribute",
        "BaseStorage.Plugin.Patching.CraftFromBaseGate",
        "IsOpen",
        [
            "BaseStorage.Plugin.Building.PlayerHaveRequirementsPiecePatch",
            "BaseStorage.Plugin.Building.PlayerPlacePiecePatch",
            "BaseStorage.Plugin.Building.PlayerTryPlacePiecePatch",
            "BaseStorage.Plugin.Crafting.InventoryGuiDoCraftingPatch",
            "BaseStorage.Plugin.Crafting.PlayerConsumeResourcesPatch",
            "BaseStorage.Plugin.Crafting.PlayerHaveRequirementItemsPatch",
            "BaseStorage.Plugin.Requirements.HudSetupPieceInfoPatch",
            "BaseStorage.Plugin.Requirements.InventoryGuiSetupRequirementListPatch",
            "BaseStorage.Plugin.Requirements.InventoryGuiSetupRequirementPatch",
        ]);
}

/// <summary>The checker's findings. Lists are ordinal-sorted; Diagnostics hold full BST2030-BST2035 lines.</summary>
internal sealed record ToggleGateReport(IReadOnlyList<string> E3Classes, IReadOnlyList<string> CheckedMethods, IReadOnlyList<string> Diagnostics);

/// <summary>
/// Reads the E3 patch classes and their first instructions from metadata and IL, without loading the assembly (contract
/// craft-from-base-toggle 7.1, ADR-GWIN-137). It follows HarmonyAttributeReader: PEReader and System.Reflection.Metadata only.
/// It decodes only what the first-instruction rules need: nop, the call, callvirt and newobj tokens, the local loads and stores
/// that may sit between the gate call and its branch, and the branch opcodes.
/// </summary>
internal static class ToggleGateChecker
{
    private const string HarmonyPatchName = "HarmonyLib.HarmonyPatch";
    private const string PrefixKind = "Prefix";
    private const string PostfixKind = "Postfix";
    private const string TranspilerKind = "Transpiler";
    private const string FinalizerKind = "Finalizer";
    private const string IlManipulatorKind = "ILManipulator";
    private const byte CallOpcode = 0x28;
    private const byte CallVirtOpcode = 0x6F;
    private const byte NewObjOpcode = 0x73;
    private const byte TwoByteOpcodePrefix = 0xFE;

    private static readonly string[] KindNames = [PrefixKind, PostfixKind, TranspilerKind, FinalizerKind, IlManipulatorKind];

    private static readonly Dictionary<string, string> KindAttributes = new(StringComparer.Ordinal)
    {
        ["HarmonyLib.HarmonyPrefix"] = PrefixKind,
        ["HarmonyLib.HarmonyPostfix"] = PostfixKind,
        ["HarmonyLib.HarmonyTranspiler"] = TranspilerKind,
        ["HarmonyLib.HarmonyFinalizer"] = FinalizerKind,
        ["HarmonyLib.HarmonyILManipulator"] = IlManipulatorKind,
    };

    /// <summary>Applies D1-D4, T1, T7 and T8 to every top-level type of the assembly. Never throws for a readable PE file.</summary>
    public static ToggleGateReport Check(string assemblyPath, ToggleGateRules rules)
    {
        using MemoryStream stream = new(File.ReadAllBytes(assemblyPath));
        using PEReader pe = new(stream);
        MetadataReader reader = pe.GetMetadataReader();
        List<string> e3 = [];
        List<string> checkedMethods = [];
        List<string> diagnostics = [];

        foreach (TypeDefinitionHandle handle in reader.TypeDefinitions)
        {
            TypeDefinition type = reader.GetTypeDefinition(handle);
            if (!type.GetDeclaringType().IsNil)
            {
                continue;
            }

            string className = TypeNames.OfDefinition(reader, handle);
            string ns = reader.GetString(type.Namespace);
            bool hasPatch = HasAttribute(reader, type.GetCustomAttributes(), HarmonyPatchName);
            bool hasMarker = HasAttribute(reader, type.GetCustomAttributes(), rules.MarkerAttribute);

            if (hasMarker && !hasPatch)
            {
                diagnostics.Add("BST2034: class '" + className + "' carries [" + ShortName(rules.MarkerAttribute) + "] but no HarmonyLib.HarmonyPatch attribute.");
            }

            bool inE3Namespace = rules.Namespaces.Any(name => ns == name || ns.StartsWith(name + ".", StringComparison.Ordinal));
            if (!hasPatch || !(inE3Namespace || hasMarker))
            {
                continue;
            }

            e3.Add(className);
            if (!hasMarker)
            {
                diagnostics.Add("BST2032: E3 patch class '" + className + "' is in an E3 namespace but lacks [" + ShortName(rules.MarkerAttribute) + "].");
            }

            CheckClass(pe, reader, handle, className, rules, diagnostics, checkedMethods);
        }

        foreach (string expected in rules.ExpectedClasses)
        {
            if (!e3.Contains(expected, StringComparer.Ordinal))
            {
                diagnostics.Add("BST2035: expected E3 patch class '" + expected + "' was not found.");
            }
        }

        return new ToggleGateReport(
            e3.Distinct(StringComparer.Ordinal).OrderBy(name => name, StringComparer.Ordinal).ToArray(),
            checkedMethods.Distinct(StringComparer.Ordinal).OrderBy(name => name, StringComparer.Ordinal).ToArray(),
            diagnostics.Distinct(StringComparer.Ordinal).OrderBy(line => line, StringComparer.Ordinal).ToArray());
    }

    private static void CheckClass(
        PEReader pe,
        MetadataReader reader,
        TypeDefinitionHandle handle,
        string className,
        ToggleGateRules rules,
        List<string> diagnostics,
        List<string> checkedMethods)
    {
        TypeDefinition type = reader.GetTypeDefinition(handle);
        SortedSet<string> kinds = new(StringComparer.Ordinal);
        List<(string Name, MethodDefinition Method)> patches = [];

        foreach (MethodDefinitionHandle methodHandle in type.GetMethods())
        {
            MethodDefinition method = reader.GetMethodDefinition(methodHandle);
            string name = reader.GetString(method.Name);
            string? kind = KindOf(reader, method, name);
            if (kind is null)
            {
                continue;
            }

            kinds.Add(kind);
            if (kind is PrefixKind or PostfixKind)
            {
                patches.Add((name, method));
            }
        }

        bool hasPrefixOrPostfix = kinds.Contains(PrefixKind) || kinds.Contains(PostfixKind);
        bool forbidden = kinds.Contains(TranspilerKind) || kinds.Contains(FinalizerKind) || kinds.Contains(IlManipulatorKind);
        if (!hasPrefixOrPostfix || forbidden)
        {
            string found = kinds.Count == 0 ? "none" : string.Join(", ", kinds);
            diagnostics.Add("BST2033: E3 patch class '" + className + "' must declare a Prefix or Postfix and no Transpiler, Finalizer or ILManipulator (found: " + found + ").");
        }

        foreach (var patch in patches)
        {
            string subject = className + "." + patch.Name;
            checkedMethods.Add(subject);
            CheckFirstInstruction(pe, reader, patch.Method, subject, rules, diagnostics);
        }
    }

    private static void CheckFirstInstruction(
        PEReader pe,
        MetadataReader reader,
        MethodDefinition method,
        string subject,
        ToggleGateRules rules,
        List<string> diagnostics)
    {
        string gate = rules.GateType + "." + rules.GateMethod + "()";
        byte[] il = method.RelativeVirtualAddress == 0
            ? Array.Empty<byte>()
            : pe.GetMethodBody(method.RelativeVirtualAddress).GetILBytes() ?? Array.Empty<byte>();

        int position = 0;
        while (position < il.Length && il[position] == 0x00)
        {
            position++;
        }

        if (position >= il.Length)
        {
            diagnostics.Add("BST2030: E3 patch method '" + subject + "' does not call " + gate + " as its first instruction (found: end of body).");
            return;
        }

        byte opcode = il[position];
        string found;
        if (opcode is CallOpcode or CallVirtOpcode or NewObjOpcode && position + 4 < il.Length)
        {
            int token = BitConverter.ToInt32(il, position + 1);
            string verb = opcode == CallOpcode ? "call" : opcode == CallVirtOpcode ? "callvirt" : "newobj";
            if (TryResolve(reader, token, out string owner, out string name, out BlobHandle signature))
            {
                if (opcode == CallOpcode && owner == rules.GateType && name == rules.GateMethod && ReturnsBoolWithNoParameters(reader, signature))
                {
                    if (!BranchesOnResultAfter(il, position + 5))
                    {
                        diagnostics.Add("BST2031: E3 patch method '" + subject + "' does not branch on the " + gate + " result.");
                    }

                    return;
                }

                found = verb + " " + owner + "::" + name;
            }
            else
            {
                found = verb + " <unresolved>";
            }
        }
        else if (opcode == TwoByteOpcodePrefix && position + 1 < il.Length)
        {
            found = "opcode 0xFE" + il[position + 1].ToString("X2");
        }
        else
        {
            found = "opcode 0x" + opcode.ToString("X2");
        }

        diagnostics.Add("BST2030: E3 patch method '" + subject + "' does not call " + gate + " as its first instruction (found: " + found + ").");
    }

    /// <summary>After the gate call, skips nop, local loads and stores and the matching short forms; the next opcode must be a branch.</summary>
    private static bool BranchesOnResultAfter(byte[] il, int start)
    {
        int position = start;
        while (position < il.Length)
        {
            byte op = il[position];
            if (op == 0x00 || (op >= 0x06 && op <= 0x0D))
            {
                position++;
                continue;
            }

            if (op is 0x11 or 0x13)
            {
                position += 2;
                continue;
            }

            return op is 0x2C or 0x2D or 0x39 or 0x3A;
        }

        return false;
    }

    private static string? KindOf(MetadataReader reader, MethodDefinition method, string name)
    {
        if (Array.IndexOf(KindNames, name) >= 0)
        {
            return name;
        }

        foreach (CustomAttributeHandle attribute in method.GetCustomAttributes())
        {
            if (KindAttributes.TryGetValue(AttributeTypeName(reader, attribute), out string? kind))
            {
                return kind;
            }
        }

        return null;
    }

    private static bool TryResolve(MetadataReader reader, int token, out string owner, out string name, out BlobHandle signature)
    {
        owner = string.Empty;
        name = string.Empty;
        signature = default;
        EntityHandle handle = MetadataTokens.EntityHandle(token);
        switch (handle.Kind)
        {
            case HandleKind.MethodDefinition:
                MethodDefinition definition = reader.GetMethodDefinition((MethodDefinitionHandle)handle);
                owner = TypeNames.OfDefinition(reader, definition.GetDeclaringType());
                name = reader.GetString(definition.Name);
                signature = definition.Signature;
                return true;
            case HandleKind.MemberReference:
                MemberReference member = reader.GetMemberReference((MemberReferenceHandle)handle);
                owner = ParentName(reader, member.Parent);
                name = reader.GetString(member.Name);
                signature = member.Signature;
                return true;
            default:
                return false;
        }
    }

    private static bool ReturnsBoolWithNoParameters(MetadataReader reader, BlobHandle signature)
    {
        BlobReader blob = reader.GetBlobReader(signature);
        blob.ReadSignatureHeader();
        int parameters = blob.ReadCompressedInteger();
        return parameters == 0 && blob.ReadSignatureTypeCode() == SignatureTypeCode.Boolean;
    }

    private static bool HasAttribute(MetadataReader reader, CustomAttributeHandleCollection attributes, string fullName) =>
        attributes.Any(handle => AttributeTypeName(reader, handle) == fullName);

    private static string AttributeTypeName(MetadataReader reader, CustomAttributeHandle handle)
    {
        CustomAttribute attribute = reader.GetCustomAttribute(handle);
        return attribute.Constructor.Kind switch
        {
            HandleKind.MethodDefinition => TypeNames.OfDefinition(reader, reader.GetMethodDefinition((MethodDefinitionHandle)attribute.Constructor).GetDeclaringType()),
            HandleKind.MemberReference => ParentName(reader, reader.GetMemberReference((MemberReferenceHandle)attribute.Constructor).Parent),
            _ => string.Empty,
        };
    }

    private static string ParentName(MetadataReader reader, EntityHandle parent) =>
        parent.Kind switch
        {
            HandleKind.TypeReference => TypeNames.OfReference(reader, (TypeReferenceHandle)parent),
            HandleKind.TypeDefinition => TypeNames.OfDefinition(reader, (TypeDefinitionHandle)parent),
            _ => string.Empty,
        };

    private static string ShortName(string fullName)
    {
        string name = fullName.Substring(fullName.LastIndexOf('.') + 1);
        return name.EndsWith("Attribute", StringComparison.Ordinal)
            ? name.Substring(0, name.Length - "Attribute".Length)
            : name;
    }

    /// <summary>Type names in the serialized form: nested types use <c>+</c>, namespaces use <c>.</c>.</summary>
    private static class TypeNames
    {
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
    }
}
