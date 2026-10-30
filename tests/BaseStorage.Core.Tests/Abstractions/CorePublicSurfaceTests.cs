namespace BaseStorage.Core.Tests.Abstractions;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BaseStorage.Core.Tests.Snapshots;

/// <summary>AC-4: every type in a public or protected signature is a BCL type or a BaseStorage.Core type from an allowed assembly.</summary>
public sealed class CorePublicSurfaceTests
{
    private const BindingFlags Declared = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
        | BindingFlags.Static | BindingFlags.DeclaredOnly;

    // GW-U-031
    [Fact]
    public void EveryTypeInAPublicOrProtectedSignatureIsSystemOrCore()
    {
        ContractTypes.SurfaceTypes();
        Type[] exported = ContractTypes.CoreAssembly().GetExportedTypes();
        var offenders = new List<string>();

        foreach (Type owner in exported)
        {
            var uses = new List<(string Origin, Type Used)>();
            foreach ((string origin, Type used) in SignatureTypes(owner))
            {
                Expand(origin, used, uses);
            }

            foreach ((string origin, Type used) in uses)
            {
                string ns = used.Namespace ?? string.Empty;
                string assembly = used.Assembly.GetName().Name ?? string.Empty;
                bool allowedNamespace = ns.StartsWith("System", StringComparison.Ordinal)
                    || ns.StartsWith("BaseStorage.Core", StringComparison.Ordinal);
                if (!allowedNamespace || ForbiddenAssemblies.IsForbidden(assembly))
                {
                    offenders.Add($"{origin} uses {used.FullName} (assembly {assembly})");
                }
            }
        }

        Assert.True(
            offenders.Count == 0,
            $"{offenders.Count} type(s) leak into the Core public surface:{Environment.NewLine}"
            + string.Join(Environment.NewLine, offenders));
    }

    private static bool IsExposedMethod(MethodBase method) => method.IsPublic || method.IsFamily || method.IsFamilyOrAssembly;

    private static IEnumerable<(string Origin, Type Used)> SignatureTypes(Type owner)
    {
        string name = owner.FullName ?? owner.Name;

        if (owner.BaseType is not null)
        {
            yield return (name + " : base", owner.BaseType);
        }

        foreach (Type contract in owner.GetInterfaces())
        {
            yield return (name + " : interface", contract);
        }

        foreach (FieldInfo field in owner.GetFields(Declared))
        {
            if (field.IsPublic || field.IsFamily || field.IsFamilyOrAssembly)
            {
                yield return (name + "." + field.Name, field.FieldType);
            }
        }

        foreach (PropertyInfo property in owner.GetProperties(Declared))
        {
            if (property.GetAccessors(nonPublic: true).Any(IsExposedMethod))
            {
                yield return (name + "." + property.Name, property.PropertyType);
            }
        }

        foreach (EventInfo evt in owner.GetEvents(Declared))
        {
            if (evt.EventHandlerType is not null && evt.AddMethod is not null && IsExposedMethod(evt.AddMethod))
            {
                yield return (name + "." + evt.Name, evt.EventHandlerType);
            }
        }

        foreach (ConstructorInfo ctor in owner.GetConstructors(Declared))
        {
            if (!IsExposedMethod(ctor))
            {
                continue;
            }

            foreach (ParameterInfo parameter in ctor.GetParameters())
            {
                yield return (name + " .ctor " + parameter.Name, parameter.ParameterType);
            }
        }

        foreach (MethodInfo method in owner.GetMethods(Declared))
        {
            if (!IsExposedMethod(method))
            {
                continue;
            }

            yield return (name + "." + method.Name + " return", method.ReturnType);
            foreach (ParameterInfo parameter in method.GetParameters())
            {
                yield return (name + "." + method.Name + " " + parameter.Name, parameter.ParameterType);
            }
        }
    }

    private static void Expand(string origin, Type used, List<(string Origin, Type Used)> sink)
    {
        if (used.HasElementType)
        {
            Expand(origin, used.GetElementType()!, sink);
            return;
        }

        if (used.IsGenericParameter)
        {
            return;
        }

        if (used.IsGenericType)
        {
            foreach (Type argument in used.GetGenericArguments())
            {
                Expand(origin, argument, sink);
            }
        }

        sink.Add((origin, used));
    }
}
