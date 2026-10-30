namespace BaseStorage.Core.Tests.Repo;

using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

/// <summary>AC-13: only the DeployToGame target may write into the game folder.</summary>
[Trait("Category", "Repo")]
public sealed class GameFolderWriteRuleTests
{
    private static readonly string[] WriteTasks = ["Copy", "MakeDir", "WriteLinesToFile", "Delete", "Exec", "Touch"];

    // GW-U-019
    [Fact]
    public void NoWriteTaskOutsideDeployToGameTouchesValheimDirOrBepInEx()
    {
        IReadOnlyList<string> files = RepoRoot.MsBuildFiles();
        Assert.Contains(ContractPaths.PluginProject, files);

        var offenders = new List<string>();
        foreach (string file in files)
        {
            foreach (XElement element in RepoRoot.LoadXml(file).Descendants())
            {
                if (IsInsideDeployToGame(element))
                {
                    continue;
                }

                if (WriteTasks.Contains(element.Name.LocalName)
                    && element.Attributes().Any(a => MentionsGameFolder(a.Value)))
                {
                    offenders.Add(file + " <" + element.Name.LocalName + "> outside DeployToGame");
                }

                if (element.Name.LocalName == "PropertyGroup")
                {
                    foreach (XElement property in element.Elements())
                    {
                        if (property.Value.Contains(@"$(ValheimDir)\BepInEx", StringComparison.OrdinalIgnoreCase))
                        {
                            offenders.Add(file + " <" + property.Name.LocalName + "> defines a BepInEx path outside DeployToGame");
                        }
                    }
                }
            }
        }

        Assert.True(offenders.Count == 0, string.Join(System.Environment.NewLine, offenders));
    }

    private static bool IsInsideDeployToGame(XElement element) =>
        element.Ancestors().Any(a => a.Name.LocalName == "Target" && RepoRoot.Attr(a, "Name") == "DeployToGame");

    private static bool MentionsGameFolder(string value) =>
        value.Contains("ValheimDir", StringComparison.OrdinalIgnoreCase)
        || value.Contains("BepInEx", StringComparison.OrdinalIgnoreCase);
}
