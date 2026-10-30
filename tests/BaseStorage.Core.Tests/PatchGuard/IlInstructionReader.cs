namespace BaseStorage.Core.Tests.PatchGuard;

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

/// <summary>One decoded IL instruction. Token is the metadata token of the operand, or 0 when the operand is not a token.</summary>
internal sealed record IlInstruction(OpCode OpCode, int Token);

/// <summary>
/// Decodes IL bytes of method bodies read through PEReader, without loading the assembly (ADR-GWIN-021).
/// The opcode table is built from the System.Reflection.Emit.OpCodes fields, so there is no hand-maintained list.
/// Not a test class.
/// </summary>
internal static class IlInstructionReader
{
    private const byte TwoByteOpCodePrefix = 0xFE;
    private static readonly Dictionary<ushort, OpCode> Table = BuildTable();

    /// <summary>Decodes the method body at the given relative virtual address.</summary>
    /// <param name="pe">The open PE image.</param>
    /// <param name="relativeVirtualAddress">The RVA of the method body.</param>
    /// <returns>The instructions in order.</returns>
    public static IReadOnlyList<IlInstruction> Decode(PEReader pe, int relativeVirtualAddress)
    {
        byte[] il = pe.GetMethodBody(relativeVirtualAddress).GetILBytes() ?? Array.Empty<byte>();
        var instructions = new List<IlInstruction>();
        int position = 0;
        while (position < il.Length)
        {
            int start = position;
            ushort key = il[position++];
            if (key == TwoByteOpCodePrefix)
            {
                key = (ushort)(0xFE00 | il[position++]);
            }

            if (!Table.TryGetValue(key, out OpCode opCode))
            {
                throw new InvalidDataException("Unknown IL opcode 0x" + key.ToString("X4") + " at offset " + start.ToString());
            }

            int token = 0;
            switch (opCode.OperandType)
            {
                case OperandType.InlineNone:
                    break;
                case OperandType.ShortInlineBrTarget:
                case OperandType.ShortInlineI:
                case OperandType.ShortInlineVar:
                    position += 1;
                    break;
                case OperandType.InlineVar:
                    position += 2;
                    break;
                case OperandType.InlineI8:
                case OperandType.InlineR:
                    position += 8;
                    break;
                case OperandType.ShortInlineR:
                    position += 4;
                    break;
                case OperandType.InlineI:
                case OperandType.InlineBrTarget:
                    position += 4;
                    break;
                case OperandType.InlineSwitch:
                    {
                        int count = BitConverter.ToInt32(il, position);
                        position += 4 + (4 * count);
                        break;
                    }

                case OperandType.InlineMethod:
                case OperandType.InlineField:
                case OperandType.InlineType:
                case OperandType.InlineTok:
                case OperandType.InlineSig:
                case OperandType.InlineString:
                    token = BitConverter.ToInt32(il, position);
                    position += 4;
                    break;
                default:
                    throw new NotSupportedException("Unsupported operand type " + opCode.OperandType.ToString() + " for " + (opCode.Name ?? "?"));
            }

            instructions.Add(new IlInstruction(opCode, token));
        }

        return instructions;
    }

    /// <summary>Decodes every method body in the assembly and reports how many were decoded.</summary>
    /// <param name="assemblyPath">The assembly to read (never loaded).</param>
    /// <returns>The number of method bodies and of instructions decoded.</returns>
    public static (int Methods, int Instructions) DecodeAll(string assemblyPath)
    {
        using var stream = new FileStream(assemblyPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var pe = new PEReader(stream);
        MetadataReader metadata = pe.GetMetadataReader();

        int methods = 0;
        int instructions = 0;
        foreach (MethodDefinitionHandle handle in metadata.MethodDefinitions)
        {
            MethodDefinition method = metadata.GetMethodDefinition(handle);
            if (method.RelativeVirtualAddress == 0)
            {
                continue;
            }

            methods++;
            instructions += Decode(pe, method.RelativeVirtualAddress).Count;
        }

        return (methods, instructions);
    }

    private static Dictionary<ushort, OpCode> BuildTable()
    {
        var table = new Dictionary<ushort, OpCode>();
        foreach (FieldInfo field in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.GetValue(null) is OpCode opCode)
            {
                table[unchecked((ushort)opCode.Value)] = opCode;
            }
        }

        return table;
    }
}
