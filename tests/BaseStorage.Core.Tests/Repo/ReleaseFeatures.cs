namespace BaseStorage.Core.Tests.Repo;

using System.Collections.Generic;

/// <summary>One shipped feature of the 0.1.0 release: its CHANGELOG leads, its package/README.md literals and its docs/PLAYTEST.md headings.</summary>
/// <param name="Id">The table id, for example QF01.</param>
/// <param name="Name">The feature name.</param>
/// <param name="ChangelogLeads">Each lead is an ordinal prefix of exactly one CHANGELOG bullet.</param>
/// <param name="ReadmeLiterals">Each literal is an ordinal substring of package/README.md.</param>
/// <param name="PlaytestHeadings">Each heading is an exact level-2 line of docs/PLAYTEST.md, after TrimEnd.</param>
public sealed record ReleaseFeature(
    string Id,
    string Name,
    IReadOnlyList<string> ChangelogLeads,
    IReadOnlyList<string> ReadmeLiterals,
    IReadOnlyList<string> PlaytestHeadings);

/// <summary>
/// The single feature table for the 0.1.0 release (contract quality-audit 5.2, ADR-GWIN-163). The consistency tests in
/// ReleaseDocsConsistencyTests check the three documents against it. Not a test class.
/// </summary>
public static class ReleaseFeatures
{
    /// <summary>The 18 features QF01 to QF18, in table order.</summary>
    public static IReadOnlyList<ReleaseFeature> All { get; } =
    [
        new("QF01", "Base line on hover",
            ["- Hover text of a chest now shows one extra line with its base"],
            ["[In base: <N> stations]"],
            ["## Hover inspect (AC-2.6, AC-8.3)"]),
        new("QF02", "Organize base button and status line",
            ["- Organize base button and base status line on every crafting station panel in a base."],
            ["**Organize base**", "## Station buttons"],
            ["## Station buttons (AC-2.4, AC-4.7, AC-4.10)", "## Gamepad cursor (AC-8.2, AC-7.3)"]),
        new("QF03", "Put away inventory",
            ["- Put away inventory button next to Organize base on every crafting station panel in a base"],
            ["**Put away inventory"],
            ["## Put away inventory (AC-2.4, AC-4.8, AC-4.9, AC-4.10, AC-5.4)"]),
        new("QF04", "Base fill statistics",
            ["- Base fill statistics:"],
            ["P% full"],
            ["## Station buttons (AC-2.4, AC-4.7, AC-4.10)"]),
        new("QF05", "Bases",
            ["- Bases:"],
            ["A base is the connected crafting stations and the chests near them."],
            ["## Base membership (AC-2.3, AC-2.5, AC-5.3)"]),
        new("QF06", "Chest reservation",
            ["- Chest reservation adapter:"],
            ["in use by Erik"],
            ["## Chest reservation (AC-5.2, AC-5.3, AC-6.3)"]),
        new("QF07", "Requirement display",
            ["- Requirement display:"],
            ["The requirement lists show the chest amounts."],
            ["## Requirement display (AC-3.2)"]),
        new("QF08", "Build from base",
            ["- Building from base chests:"],
            ["building at a station use the materials in the chests of your base"],
            ["## Build from base (AC-2.4, AC-3.1, AC-3.4)"]),
        new("QF09", "Favourites",
            ["- Favourites:"],
            ["favourites (Alt+click a slot)"],
            ["## Favourites"]),
        new("QF10", "Categories file",
            ["- Categories file:"],
            ["BepInEx/config/BaseStorage.categories.txt"],
            ["## Category file"]),
        new("QF11", "Chest categories",
            ["- Chest category storage:", "- Category button on the open-chest panel:"],
            ["**Category** button", "CycleCategoryKey"],
            ["## Category assignment (AC-4.3, AC-8.2)"]),
        new("QF12", "Organize",
            ["- Organize runs in game:", "- The Organize preview now reads"],
            ["**Organize base.**", "OrganizeKey"],
            ["## Organize (AC-2.4, AC-4.7, AC-4.8, AC-5.4)"]),
        new("QF13", "Version handshake",
            ["- Version handshake:"],
            ["Don's QOL version mismatch: host X, you Y"],
            ["## Version handshake (AC-6.1, AC-6.3)"]),
        new("QF14", "Host-synced settings",
            ["- Host-synced settings:"],
            ["Host-synced settings take the host's value in multiplayer"],
            ["## Config sync (AC-6.2, AC-6.3)"]),
        new("QF15", "Settings and logging",
            ["- Settings and logging:"],
            ["BepInEx/config/dawso.valheim.basestorage.cfg", "DebugLogging"],
            ["## Load check", "## Debug logging"]),
        new("QF16", "Craft from base (incl. upgrade, multi-craft, toggle)",
            ["- Crafting from base chests:"],
            ["**Craft and build from your chests.**", "CraftFromBase"],
            ["## Craft from base (AC-2.4, AC-3.1, AC-3.4)", "## Upgrade and multi-craft from base (AC-3.5)", "## Craft from base off (AC-3.6)"]),
        new("QF17", "Console command",
            ["- Console command `dons-qol organize`:"],
            ["dons-qol organize"],
            ["## Organize console command (AC-2.4, AC-5.4)"]),
        new("QF18", "One message per action",
            ["- Crafting and building with materials from base chests now confirm with one short message"],
            ["Every Don's QOL action ends with one short message"],
            ["## Feedback messages (AC-8.1, AC-7.3)"]),
    ];
}
