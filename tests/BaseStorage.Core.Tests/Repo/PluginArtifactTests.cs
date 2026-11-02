namespace BaseStorage.Core.Tests.Repo;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

/// <summary>
/// GW-I-010 (AC-1, ADR-GWIN-013): the built BaseStorage.dll carries the [BepInPlugin] identity and the version constant.
/// The dll is read with System.Reflection.Metadata and never loaded, so BepInEx and Unity are not needed.
/// A missing dll fails this test; it never skips.
/// </summary>
[Trait("Category", "Repo")]
[Collection("MSBuildProcess")]
public sealed class PluginArtifactTests
{
    private const string PluginTypeName = "BaseStorage.Plugin.BaseStoragePlugin";
    private const string PluginGuid = "dawso.valheim.basestorage";
    private const string AssemblyName = "BaseStorage";
    private const string PluginName = "Don's QOL";
    private const string BindingTypeName = "BaseStorage.Plugin.Settings.BepInExSettingsSource";

    // GW-I-010
    [Fact]
    public void BuiltPluginDllCarriesTheBepInPluginIdentityAndTheVersionConstant()
    {
        AssemblyConfigurationAttribute? configuration = typeof(PluginArtifactTests).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>();
        Assert.NotNull(configuration);
        string configurationName = configuration.Configuration ?? string.Empty;

        string path = PluginDll.EnsureBuilt(configurationName);

        using var stream = File.OpenRead(path);
        using var pe = new PEReader(stream);
        MetadataReader metadata = pe.GetMetadataReader();

        Assert.Equal(AssemblyName, metadata.GetString(metadata.GetAssemblyDefinition().Name));

        string expectedVersion = RepoRoot.ValuesOf(ContractPaths.VersionProps, "Version").Single();
        List<(TypeDefinitionHandle Handle, string FullName, string Guid, string Name, string Version)> plugins = BepInPluginAttributes(metadata);

        var plugin = Assert.Single(plugins);
        Assert.Equal(PluginTypeName, plugin.FullName);
        Assert.Equal(PluginGuid, plugin.Guid);
        Assert.Equal(PluginName, plugin.Name);
        Assert.Equal(expectedVersion, plugin.Version);

        TypeDefinition pluginType = metadata.GetTypeDefinition(plugin.Handle);
        FieldDefinition versionField = Assert.Single(
            pluginType.GetFields().Select(handle => metadata.GetFieldDefinition(handle)).ToArray(),
            field => metadata.GetString(field.Name) == "PluginVersion");
        Constant constant = metadata.GetConstant(versionField.GetDefaultValue());
        BlobReader constantReader = metadata.GetBlobReader((BlobHandle)constant.Value);
        Assert.Equal(expectedVersion, constantReader.ReadUTF16(constantReader.Length));
    }

    private static List<(TypeDefinitionHandle Handle, string FullName, string Guid, string Name, string Version)> BepInPluginAttributes(MetadataReader metadata)
    {
        var found = new List<(TypeDefinitionHandle, string, string, string, string)>();
        foreach (TypeDefinitionHandle handle in metadata.TypeDefinitions)
        {
            TypeDefinition type = metadata.GetTypeDefinition(handle);
            foreach (CustomAttributeHandle attributeHandle in type.GetCustomAttributes())
            {
                CustomAttribute attribute = metadata.GetCustomAttribute(attributeHandle);
                if (!IsBepInPlugin(metadata, attribute))
                {
                    continue;
                }

                // Fixed arguments of BepInPlugin(string guid, string name, string version): prolog 0x0001, then three SerStrings.
                BlobReader reader = metadata.GetBlobReader(attribute.Value);
                Assert.Equal((ushort)1, reader.ReadUInt16());
                string guid = reader.ReadSerializedString() ?? string.Empty;
                string name = reader.ReadSerializedString() ?? string.Empty;
                string version = reader.ReadSerializedString() ?? string.Empty;
                found.Add((handle, FullNameOf(metadata, type), guid, name, version));
            }
        }

        return found;
    }

    // GW-I-011 (AC-1, AC-5, ADR-GWIN-015): the built binding compiled against the BepInEx config API. Read, never loaded.
    [Fact]
    public void BuiltBindingCompilesAgainstTheBepInExConfigApi()
    {
        string path = LocatePluginDll();

        using var stream = File.OpenRead(path);
        using var pe = new PEReader(stream);
        MetadataReader metadata = pe.GetMetadataReader();

        TypeDefinitionHandle[] bindings = metadata.TypeDefinitions
            .Where(handle => FullNameOf(metadata, metadata.GetTypeDefinition(handle)) == BindingTypeName)
            .ToArray();
        TypeDefinition binding = metadata.GetTypeDefinition(Assert.Single(bindings));
        Assert.Equal(TypeAttributes.NotPublic, binding.Attributes & TypeAttributes.VisibilityMask);

        bool implementsSource = binding.GetInterfaceImplementations()
            .Select(handle => metadata.GetInterfaceImplementation(handle))
            .Any(implementation => IsTypeReference(metadata, implementation.Interface, "BaseStorage.Core.Settings.ISettingsSource"));
        Assert.True(implementsSource, "BepInExSettingsSource does not implement BaseStorage.Core.Settings.ISettingsSource.");

        MemberReference[] members = metadata.MemberReferences.Select(handle => metadata.GetMemberReference(handle)).ToArray();
        Assert.True(
            members.Any(member => metadata.GetString(member.Name) == "Bind" && IsTypeReference(metadata, member.Parent, "BepInEx.Configuration.ConfigFile")),
            "The built dll has no call to BepInEx.Configuration.ConfigFile.Bind.");

        // GW-I-011 (AC-5): KeyboardShortcut.Empty is compiled in. In BepInEx 5.4.x, Empty is a static readonly field, not a property,
        // so the reference is a field reference named "Empty" (no get_Empty accessor is emitted).
        Assert.True(
            members.Any(member => metadata.GetString(member.Name) == "Empty"
                && member.GetKind() == MemberReferenceKind.Field
                && IsTypeReference(metadata, member.Parent, "BepInEx.Configuration.KeyboardShortcut")),
            "The built dll has no field reference to BepInEx.Configuration.KeyboardShortcut.Empty (expected a MemberReference named 'Empty' of kind Field).");
    }

    // Same locate-or-build step as GW-I-010, through the shared PluginDll helper.
    private static string LocatePluginDll()
    {
        AssemblyConfigurationAttribute? configuration = typeof(PluginArtifactTests).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>();
        Assert.NotNull(configuration);
        return PluginDll.EnsureBuilt(configuration.Configuration ?? string.Empty);
    }

    private static bool IsTypeReference(MetadataReader metadata, EntityHandle handle, string fullName)
    {
        if (handle.Kind != HandleKind.TypeReference)
        {
            return false;
        }

        TypeReference reference = metadata.GetTypeReference((TypeReferenceHandle)handle);
        string ns = metadata.GetString(reference.Namespace);
        string name = metadata.GetString(reference.Name);
        return (ns.Length == 0 ? name : ns + "." + name) == fullName;
    }

    private static bool IsBepInPlugin(MetadataReader metadata, CustomAttribute attribute)
    {
        if (attribute.Constructor.Kind != HandleKind.MemberReference)
        {
            return false;
        }

        MemberReference constructor = metadata.GetMemberReference((MemberReferenceHandle)attribute.Constructor);
        if (constructor.Parent.Kind != HandleKind.TypeReference)
        {
            return false;
        }

        TypeReference declaring = metadata.GetTypeReference((TypeReferenceHandle)constructor.Parent);
        return metadata.GetString(declaring.Namespace) == "BepInEx"
            && metadata.GetString(declaring.Name) == "BepInPlugin";
    }

    private static string FullNameOf(MetadataReader metadata, TypeDefinition type)
    {
        string ns = metadata.GetString(type.Namespace);
        string name = metadata.GetString(type.Name);
        return ns.Length == 0 ? name : ns + "." + name;
    }
}
