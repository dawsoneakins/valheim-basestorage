namespace BaseStorage.Core.Tests.Abstractions;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

/// <summary>
/// BaseStorage.Core.xml member IDs for the doc checks (ADR-GWIN-006). Lifted out of CoreXmlDocTests so the
/// Persistence check (GW-U-079) uses the same formatter (ADR-GWIN-013). The formatting rules are unchanged.
/// </summary>
public static class XmlDocMembers
{
    private const BindingFlags Members = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    /// <summary>
    /// Every type, constructor, method, property and (for enums) field ID of the given types, with the parameter names
    /// that need a param and whether a returns is needed.
    /// </summary>
    public static IEnumerable<(string Id, string[] Parameters, bool ReturnsValue)> TargetsFor(IEnumerable<Type> types)
    {
        foreach (Type type in types)
        {
            string typeName = DocName(type);
            yield return ("T:" + typeName, Array.Empty<string>(), false);

            foreach (ConstructorInfo ctor in type.GetConstructors(BindingFlags.Public | BindingFlags.Instance))
            {
                yield return (
                    "M:" + typeName + ".#ctor" + ParameterList(ctor.GetParameters()),
                    ParameterNames(ctor.GetParameters()),
                    false);
            }

            foreach (MethodInfo method in type.GetMethods(Members))
            {
                if (method.IsSpecialName && !method.Name.StartsWith("op_", StringComparison.Ordinal))
                {
                    continue;
                }

                yield return (
                    "M:" + typeName + "." + method.Name + ParameterList(method.GetParameters()),
                    ParameterNames(method.GetParameters()),
                    method.ReturnType != typeof(void));
            }

            foreach (PropertyInfo property in type.GetProperties(Members))
            {
                yield return ("P:" + typeName + "." + property.Name, Array.Empty<string>(), false);
            }

            if (type.IsEnum)
            {
                foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
                {
                    yield return ("F:" + typeName + "." + field.Name, Array.Empty<string>(), false);
                }
            }
        }
    }

    /// <summary>The F: IDs of the public static const and readonly fields declared on a static class such as PersistenceKeys.</summary>
    public static IEnumerable<(string Id, string[] Parameters, bool ReturnsValue)> ConstantTargets(Type type)
    {
        foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
        {
            yield return ("F:" + DocName(type) + "." + field.Name, Array.Empty<string>(), false);
        }
    }

    private static string DocName(Type type) => type.FullName!.Replace('+', '.');

    // The compiler writes no parentheses for a member without parameters (M:Type.GetHashCode), so the list is omitted too.
    private static string ParameterList(ParameterInfo[] parameters) =>
        parameters.Length == 0 ? string.Empty : "(" + ParameterIds(parameters) + ")";

    private static string ParameterIds(ParameterInfo[] parameters) =>
        string.Join(",", parameters.Select(p => DocId(p.ParameterType)));

    private static string[] ParameterNames(ParameterInfo[] parameters) =>
        parameters.Select(p => p.Name ?? string.Empty).ToArray();

    private static string DocId(Type type)
    {
        if (type.IsArray)
        {
            return DocId(type.GetElementType()!) + "[]";
        }

        if (type.IsGenericType)
        {
            string definition = type.GetGenericTypeDefinition().FullName!;
            string prefix = definition[..definition.IndexOf('`')];
            return prefix + "{" + string.Join(",", type.GetGenericArguments().Select(DocId)) + "}";
        }

        return DocName(type);
    }
}
