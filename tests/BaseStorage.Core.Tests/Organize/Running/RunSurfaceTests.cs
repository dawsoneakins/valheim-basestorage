namespace BaseStorage.Core.Tests.Organize.Running;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using BaseStorage.Core.Organize.Run;
using BaseStorage.Core.Tests.Abstractions;

/// <summary>GW-U-1376..1378 (contract 1, 2, 4; R9): the exported surface of the Run namespace, the enum values and the XML docs.</summary>
public sealed class RunSurfaceTests
{
    private const string Namespace = "BaseStorage.Core.Organize.Run";

    private static Type[] Exported() => typeof(OrganizeRunCoordinator).Assembly.GetExportedTypes()
        .Where(type => type.Namespace == Namespace)
        .OrderBy(type => type.Name, StringComparer.Ordinal)
        .ToArray();

    // GW-U-1376
    [Fact]
    public void TheExportedTypesAreExactlyTheNineOfTheContract()
    {
        Assert.Equal(
            new[]
            {
                "HotkeyChord",
                "IOrganizeRunPort",
                "OrganizeBaseFacts",
                "OrganizeLayerRules",
                "OrganizeNoticePlacement",
                "OrganizeRunCoordinator",
                "OrganizeRunState",
                "OrganizeRunText",
                "OrganizeStartOutcome",
            },
            Exported().Select(t => t.Name).ToArray());

        foreach (Type type in Exported().Where(t => t.IsClass))
        {
            Assert.True(type.IsSealed || type.IsAbstract, type.Name + " must be sealed or static.");
            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                Assert.True(property.SetMethod is null || !property.SetMethod.IsPublic, type.Name + "." + property.Name + " has a public setter.");
            }
        }
    }

    // GW-U-1377
    [Fact]
    public void TheEnumsHaveTheContractedValues()
    {
        Assert.Equal(new[] { 0, 1, 2 }, Enum.GetValues(typeof(OrganizeRunState)).Cast<int>().ToArray());
        Assert.Equal(new[] { "Idle", "Previewing", "Running" }, Enum.GetNames(typeof(OrganizeRunState)));
        Assert.Equal(new[] { 0, 1, 2, 3, 4, 5 }, Enum.GetValues(typeof(OrganizeStartOutcome)).Cast<int>().ToArray());
        Assert.Equal(new[] { "Started", "AlreadyActive", "NoPlayer", "NotInBase", "NoChests", "LockRefused" }, Enum.GetNames(typeof(OrganizeStartOutcome)));
        Assert.Equal(new[] { "Centre", "TopLeft" }, Enum.GetNames(typeof(OrganizeNoticePlacement)));
        Assert.Equal(typeof(int), Enum.GetUnderlyingType(typeof(OrganizeRunState)));
        Assert.Equal(typeof(int), Enum.GetUnderlyingType(typeof(OrganizeStartOutcome)));
        Assert.Equal(typeof(int), Enum.GetUnderlyingType(typeof(OrganizeNoticePlacement)));
    }

    // GW-U-1377: the coordinator and the port expose the contracted members by name.
    [Fact]
    public void TheCoordinatorAndThePortHaveTheContractedMembers()
    {
        string[] coordinator = typeof(OrganizeRunCoordinator).GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(m => m.Name).Where(n => !n.StartsWith("get_", StringComparison.Ordinal) && n != ".ctor").OrderBy(n => n, StringComparer.Ordinal).ToArray();
        // Edited by S-E004-015 (ADR-GWIN-143): plus StartDeposit (deposit-button contract 2.2).
        // Edited by S-E010-002 (organize-console-command 3): plus RefusedBy.
        Assert.Equal(new[] { "Cancel", "Confirm", "Plan", "RefusedBy", "Start", "StartDeposit", "State", "Tick" }, coordinator);
        Assert.Equal(6, typeof(OrganizeRunCoordinator).GetConstructors().Single().GetParameters().Length);

        string[] port = typeof(IOrganizeRunPort).GetMembers()
            .Select(m => m.Name).Where(n => !n.StartsWith("get_", StringComparison.Ordinal)).OrderBy(n => n, StringComparer.Ordinal).ToArray();
        // Edited by S-E004-015 (ADR-GWIN-143): plus CaptureInventory, Favourites and CreateDepositLayer (deposit-button contract 2.1).
        Assert.Equal(
            new[] { "CaptureInventory", "Catalog", "CreateDepositLayer", "CreateLayer", "Favourites", "GatherFacts", "HolderId", "HolderName", "InWorld", "ItemKinds", "SaveCategoryRules", "ShowNotice", "ShowPreview" },
            port);
    }

    // GW-U-1378 (R9, CS1591): every public member of the 9 types is documented in BaseStorage.Core.xml.
    [Fact]
    public void EveryTypeMemberAndEnumMemberOfTheNamespaceIsDocumented()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "BaseStorage.Core.xml");
        Assert.True(File.Exists(path), "BaseStorage.Core.xml is missing from " + AppContext.BaseDirectory + ".");
        Dictionary<string, XElement> members = XDocument.Load(path).Descendants("member")
            .Where(m => m.Attribute("name") is not null)
            .ToDictionary(m => m.Attribute("name")!.Value, StringComparer.Ordinal);

        Type[] types = Exported();
        Assert.Equal(9, types.Length);
        var problems = new List<string>();
        foreach (var (id, parameters, returnsValue) in XmlDocMembers.TargetsFor(types))
        {
            if (!members.TryGetValue(id, out XElement? member))
            {
                problems.Add("missing <member>: " + id);
                continue;
            }

            if (string.IsNullOrWhiteSpace(member.Element("summary")?.Value))
            {
                problems.Add("blank or missing <summary>: " + id);
            }

            foreach (string parameter in parameters)
            {
                if (!member.Elements("param").Any(p => p.Attribute("name")?.Value == parameter))
                {
                    problems.Add("missing <param name=\"" + parameter + "\">: " + id);
                }
            }

            if (returnsValue && member.Element("returns") is null)
            {
                problems.Add("missing <returns>: " + id);
            }
        }

        foreach (var (id, _, _) in XmlDocMembers.ConstantTargets(typeof(OrganizeLayerRules)).Concat(XmlDocMembers.ConstantTargets(typeof(OrganizeRunText))))
        {
            if (!members.ContainsKey(id))
            {
                problems.Add("missing <member>: " + id);
            }
        }

        Assert.Empty(problems);
    }
}
