namespace BaseStorage.Core.Tests.Bases;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BaseStorage.Core.Bases.Fill;

/// <summary>
/// GW-U-2251 (base-fill contract section 1): the exported types of BaseStorage.Core.Bases.Fill are exactly BaseFill, and its public members
/// are exactly the section 1 list. Compile-red until BaseFill exists.
/// </summary>
public sealed class BaseFillSurfaceTests
{
    private const string FillNamespace = "BaseStorage.Core.Bases.Fill";

    // GW-U-2251 (section 1): the namespace exports exactly one type, BaseFill, with exactly the public members of section 1.
    [Fact]
    public void TheFillNamespaceExportsExactlyBaseFill()
    {
        Type[] exported = typeof(BaseFill).Assembly.GetExportedTypes()
            .Where(type => type.Namespace == FillNamespace)
            .ToArray();
        Assert.Equal(new[] { typeof(BaseFill) }, exported);

        string[] expected =
        {
            "ctor(Int32,Int32)",
            "property OccupiedSlots:Int32",
            "property Percent:Int32",
            "property TotalSlots:Int32",
            "static FromContainers(IEnumerable`1):BaseFill",
            "Equals(BaseFill):Boolean",
            "Equals(Object):Boolean",
            "GetHashCode():Int32",
            "static op_Equality(BaseFill,BaseFill):Boolean",
            "static op_Inequality(BaseFill,BaseFill):Boolean",
        };

        string[] actual = Signatures(typeof(BaseFill)).OrderBy(text => text, StringComparer.Ordinal).ToArray();
        Assert.Equal(expected.OrderBy(text => text, StringComparer.Ordinal), actual);
    }

    private static IEnumerable<string> Signatures(Type type)
    {
        const BindingFlags Flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        foreach (ConstructorInfo constructor in type.GetConstructors(Flags))
        {
            yield return "ctor(" + Parameters(constructor) + ")";
        }

        foreach (PropertyInfo property in type.GetProperties(Flags))
        {
            yield return "property " + property.Name + ":" + property.PropertyType.Name + (property.CanWrite ? ":set" : string.Empty);
        }

        foreach (MethodInfo method in type.GetMethods(Flags))
        {
            if (method.IsSpecialName && (method.Name.StartsWith("get_", StringComparison.Ordinal) || method.Name.StartsWith("set_", StringComparison.Ordinal)))
            {
                continue;
            }

            yield return (method.IsStatic ? "static " : string.Empty) + method.Name + "(" + Parameters(method) + "):" + method.ReturnType.Name;
        }

        foreach (FieldInfo field in type.GetFields(Flags))
        {
            yield return "field " + field.Name;
        }
    }

    private static string Parameters(MethodBase method) =>
        string.Join(",", method.GetParameters().Select(parameter => parameter.ParameterType.Name));
}
