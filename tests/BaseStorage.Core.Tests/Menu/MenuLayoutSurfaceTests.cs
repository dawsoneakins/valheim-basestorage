namespace BaseStorage.Core.Tests.Menu;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using BaseStorage.Core.Menu;
using BaseStorage.Core.Tests.Repo;
using BaseStorage.Core.Tests.Restock.Run;
using static BaseStorage.Core.Tests.Menu.MenuFixtures;

/// <summary>GW-U-2546: purity, the public surface and the XML docs of the Menu layout (S-QR-002 contract 2.1, 2.2 and section 1).</summary>
public sealed class MenuLayoutSurfaceTests
{
    private static readonly Type[] MenuTypes = { typeof(MenuRect), typeof(MenuButtonSpec), typeof(MenuLayout), typeof(MenuButtonLayout) };

    /// <summary>GW-U-2546: two calls give equal layouts, Buttons is read-only, MenuLayout has no public constructor or operator, the types use BCL types only, and every public member has an XML doc.</summary>
    [Fact]
    public void MenuLayoutIsPureAndBclOnly()
    {
        MenuLayout first = Std(560f);
        MenuLayout second = Std(560f);
        Assert.Equal(first.Buttons.Count, second.Buttons.Count);
        for (int i = 0; i < first.Buttons.Count; i++)
        {
            Assert.Equal(first.Buttons[i], second.Buttons[i]);
        }

        Assert.IsType<ReadOnlyCollection<MenuRect>>(first.Buttons);

        Assert.Empty(typeof(MenuLayout).GetConstructors(BindingFlags.Public | BindingFlags.Instance));
        Assert.DoesNotContain(typeof(MenuLayout).GetMethods(BindingFlags.Public | BindingFlags.Static), m => m.Name.StartsWith("op_", StringComparison.Ordinal));

        RunIlWalk.Walk walk = RunIlWalk.Collect(typeof(MenuLayout).Assembly, type => MenuTypes.Contains(type));
        Assert.Equal(MenuTypes.Length, walk.Owners.Count);

        var leaves = walk.Types.SelectMany(use => RunIlWalk.Leaves(use.Type).Select(leaf => (Leaf: leaf, use.Origin))).ToList();
        string[] foreign = leaves
            .Where(item => !IsBclOrMenu(item.Leaf))
            .Select(item => (item.Leaf.FullName ?? "(unnamed)") + " used by " + item.Origin)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(text => text, StringComparer.Ordinal)
            .ToArray();
        Assert.Empty(foreign);
        Assert.Contains(typeof(ReadOnlyCollection<>), leaves.Select(item => item.Leaf));

        var undocumented = new List<string>();
        foreach (string file in new[] { "MenuRect", "MenuButtonSpec", "MenuLayout", "MenuButtonLayout" })
        {
            string relative = "src/BaseStorage.Core/Menu/" + file + ".cs";
            string[] lines = RepoRoot.ReadText(relative).Replace("\r\n", "\n").Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (!line.StartsWith("public ", StringComparison.Ordinal))
                {
                    continue;
                }

                int previous = i - 1;
                while (previous >= 0 && lines[previous].Trim().StartsWith("[", StringComparison.Ordinal))
                {
                    previous--;
                }

                if (previous < 0 || !lines[previous].Trim().StartsWith("///", StringComparison.Ordinal))
                {
                    undocumented.Add(relative + ":" + (i + 1));
                }
            }
        }

        Assert.True(undocumented.Count == 0, "Public members without an XML doc: " + string.Join(", ", undocumented));
    }

    /// <summary>A leaf the Menu layout may name: a BCL type (mscorlib, netstandard, or a System assembly), a generic parameter, or one of the four Menu types.</summary>
    /// <param name="leaf">The leaf type.</param>
    /// <returns>True when the type is allowed.</returns>
    private static bool IsBclOrMenu(Type leaf)
    {
        if (leaf.IsGenericParameter || MenuTypes.Contains(leaf))
        {
            return true;
        }

        string name = leaf.Assembly.GetName().Name ?? string.Empty;
        return leaf.Assembly == typeof(object).Assembly || name == "mscorlib" || name == "netstandard" || name.StartsWith("System.", StringComparison.Ordinal);
    }
}
