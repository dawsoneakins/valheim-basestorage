namespace BaseStorage.Plugin.ChestNames;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Bases.Resolution;
using BaseStorage.Core.ChestCategories;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Settings;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Ui;
using BaseStorage.Plugin.ChestCategories;
using BaseStorage.Plugin.Ui;
using BaseStorage.Plugin.World;
using TMPro;
using UnityEngine;

/// <summary>
/// Floating labels above named and categorised chests near the player, inside a base. The chests near the player are looked up a few times a
/// second, so a chest out of range costs nothing per frame, and the labels sit in the HUD, so a menu or the hidden HUD hides them. Labels are
/// built once and reused. Main thread only.
/// </summary>
internal static class ChestLabels
{
    private const string LabelId = "chest.world.label";
    private const int MaxLabels = 24;
    private const float Height = 1.2f;
    private const float FontSize = 16f;
    private static readonly TimeSpan ScanInterval = TimeSpan.FromSeconds(0.5);
    private static readonly List<TMP_Text> Labels = new List<TMP_Text>();
    private static readonly List<Target> Targets = new List<Target>();
    private static readonly List<Container> Scratch = new List<Container>();
    private static readonly Dictionary<ContainerId, Container> ById = new Dictionary<ContainerId, Container>();

    private static PluginWorld? world;
    private static IClock? clock;
    private static BaseStorageSettings? settings;
    private static Hud? builtFor;
    private static TimeSpan nextScan = TimeSpan.Zero;

    /// <summary>Stores the services the labels read.</summary>
    /// <param name="current">The plugin world.</param>
    /// <param name="time">The clock.</param>
    /// <param name="config">The settings.</param>
    internal static void Initialize(PluginWorld current, IClock time, BaseStorageSettings config)
    {
        world = current;
        clock = time;
        settings = config;
    }

    /// <summary>Places the labels. Called once per frame.</summary>
    internal static void Tick()
    {
        Hud? hud = Hud.instance;
        Player? player = Player.m_localPlayer;
        Camera? camera = Utils.GetMainCamera();
        if (world == null || clock == null || settings == null || !settings.ChestLabels || hud == null || player == null || camera == null
            || Hud.IsUserHidden() || InventoryGui.IsVisible() || Menu.IsVisible() || Minimap.IsOpen())
        {
            Hide(0);
            return;
        }

        if (!ReferenceEquals(builtFor, hud))
        {
            builtFor = hud;
            Labels.Clear();
            Targets.Clear();
        }

        TimeSpan now = clock.Elapsed;
        if (now >= nextScan)
        {
            nextScan = now + ScanInterval;
            Scan(player.transform.position);
        }

        int used = 0;
        for (int i = 0; i < Targets.Count && used < MaxLabels; i++)
        {
            Target target = Targets[i];
            if (target.Chest == null)
            {
                continue;
            }

            Vector3 screen = Utils.WorldToScreenPointScaled(camera, target.Chest.transform.position + (Vector3.up * Height));
            if (screen.z <= 0f)
            {
                continue;
            }

            TMP_Text? label = LabelAt(hud, used);
            if (label == null)
            {
                break;
            }

            if (!string.Equals(label.text, target.Text, StringComparison.Ordinal))
            {
                label.text = target.Text;
            }

            label.rectTransform.position = screen;
            if (!label.gameObject.activeSelf)
            {
                label.gameObject.SetActive(true);
            }

            used++;
        }

        Hide(used);
    }

    private static void Hide(int from)
    {
        for (int i = from; i < Labels.Count; i++)
        {
            if (Labels[i] != null && Labels[i].gameObject.activeSelf)
            {
                Labels[i].gameObject.SetActive(false);
            }
        }
    }

    private static TMP_Text? LabelAt(Hud hud, int index)
    {
        while (Labels.Count <= index)
        {
            TMP_Text? created = VanillaButtonFactory.CreateText(hud.m_hoverName, hud.m_rootObject.transform, LabelId + "." + Labels.Count);
            if (created == null)
            {
                return null;
            }

            created.alignment = TextAlignmentOptions.Center;
            created.enableAutoSizing = false;
            created.fontSize = FontSize;
            created.textWrappingMode = TextWrappingModes.NoWrap;
            created.raycastTarget = false;
            created.richText = false;
            created.rectTransform.sizeDelta = new Vector2(320f, 26f);
            created.gameObject.SetActive(false);
            Labels.Add(created);
        }

        return Labels[index];
    }

    private static void Scan(Vector3 at)
    {
        Targets.Clear();
        var position = new WorldPosition(at.x, at.y, at.z);
        BaseResolution resolution = world!.Cache.Resolve(BaseStart.FromPosition(position), new BaseCacheOptions(settings!.BaseCacheSeconds, settings.MaxBaseStations));
        if (!resolution.IsInBase)
        {
            return;
        }

        ById.Clear();
        Scratch.Clear();
        world.Containers.CopyItemsTo(Scratch);
        for (int i = 0; i < Scratch.Count; i++)
        {
            Container chest = Scratch[i];
            ZDO? zdo = chest != null && chest.m_nview != null && chest.m_nview.IsValid() ? chest.m_nview.GetZDO() : null;
            if (zdo != null)
            {
                ById[new ContainerId(zdo.m_uid.UserID, zdo.m_uid.ID)] = chest!;
            }
        }

        float range = Math.Max(0, settings.ChestLabelDistance);
        float limit = range * range;
        for (int i = 0; i < resolution.Containers.Count; i++)
        {
            if (ById.TryGetValue(resolution.Containers[i].Id, out Container? chest)
                && (chest.transform.position - at).sqrMagnitude <= limit)
            {
                string text = TextOf(chest);
                if (text.Length > 0)
                {
                    Targets.Add(new Target(chest, text));
                }
            }
        }
    }

    private static string TextOf(Container chest)
    {
        string name = ChestNames.Read(chest);
        if (name.Length > 0)
        {
            return name;
        }

        CategoryAssignment? assignment = ChestCategoryWriter.Read(chest);
        return assignment == null ? string.Empty : ChestCategoryReader.Label(assignment, StringTable.English);
    }

    private readonly struct Target
    {
        internal Target(Container chest, string text)
        {
            Chest = chest;
            Text = text;
        }

        internal Container Chest { get; }

        internal string Text { get; }
    }
}
