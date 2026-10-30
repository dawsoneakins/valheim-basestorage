namespace BaseStorage.Plugin.Ui;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Logging;
using BaseStorage.Core.Ui;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>The single place that creates buttons: clones of a vanilla button, labelled from the strings table.</summary>
internal static class VanillaButtonFactory
{
    private const float MinWidth = 120f;
    private const float MinHeight = 32f;

    private const string NamePrefix = "BaseStorage.";

    private static readonly object LogLock = new object();
    private static readonly HashSet<string> Logged = new HashSet<string>(StringComparer.Ordinal);

    /// <summary>Clones a vanilla button (or returns the existing clone) labelled from the strings table.</summary>
    /// <param name="template">The vanilla button to clone; null yields null.</param>
    /// <param name="parent">The transform the clone is parented to.</param>
    /// <param name="id">The design-system control id; the clone is named with a BaseStorage prefix.</param>
    /// <param name="labelKey">The strings table key of the label.</param>
    /// <param name="onClick">The click handler; it replaces every template listener.</param>
    /// <returns>The clone, the existing clone, or null when the template is missing.</returns>
    public static Button? Create(Button? template, Transform parent, string id, string labelKey, UnityAction onClick)
    {
        if (parent == null)
        {
            throw new ArgumentNullException(nameof(parent));
        }

        if (id is null)
        {
            throw new ArgumentNullException(nameof(id));
        }

        if (labelKey is null)
        {
            throw new ArgumentNullException(nameof(labelKey));
        }

        if (onClick is null)
        {
            throw new ArgumentNullException(nameof(onClick));
        }

        string name = NamePrefix + id;
        Transform? existing = parent.Find(name);
        if (existing != null)
        {
            return existing.GetComponent<Button>();
        }

        if (template == null)
        {
            LogOnce(id, "template missing; control not created");
            return null;
        }

        GameObject clone = UnityEngine.Object.Instantiate(template.gameObject, parent);
        clone.name = name;

        Button button = clone.GetComponent<Button>();
        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(onClick);

        if (!TrySetLabel(button, SingleLine.Of(StringTable.English.Format(labelKey))))
        {
            LogOnce(id, "fallback: no label component");
        }

        EnforceMinimumSize(clone);
        return button;
    }

    /// <summary>Creates a plain UI node for the BaseStorage menu, anchored top-left, so all UI objects are made here.</summary>
    /// <param name="parent">The transform the node is parented to.</param>
    /// <param name="id">The design-system control id.</param>
    /// <param name="size">The size in canvas units.</param>
    /// <param name="position">The position from the parent's top-left corner.</param>
    /// <param name="components">The components to add.</param>
    /// <returns>The node.</returns>
    public static GameObject CreateNode(Transform parent, string id, Vector2 size, Vector2 position, params Type[] components)
    {
        var node = new GameObject(NamePrefix + id, components);
        node.transform.SetParent(parent, false);
        var rect = node.GetComponent<RectTransform>() ?? node.AddComponent<RectTransform>();
        Pin(rect, size, position);
        return node;
    }

    /// <summary>Anchors a rect to its parent's top-left corner.</summary>
    /// <param name="rect">The rect.</param>
    /// <param name="size">The size in canvas units.</param>
    /// <param name="position">The position from the parent's top-left corner.</param>
    public static void Pin(RectTransform rect, Vector2 size, Vector2 position)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    /// <summary>Clones a vanilla TMP label (or returns the existing clone) with empty text.</summary>
    /// <param name="template">The vanilla label to clone; null yields null.</param>
    /// <param name="parent">The transform the clone is parented to.</param>
    /// <param name="id">The design-system control id; the clone is named with a BaseStorage prefix.</param>
    /// <returns>The clone, the existing clone, or null when the template is missing.</returns>
    public static TMP_Text? CreateText(TMP_Text? template, Transform parent, string id)
    {
        if (parent == null)
        {
            throw new ArgumentNullException(nameof(parent));
        }

        if (id is null)
        {
            throw new ArgumentNullException(nameof(id));
        }

        string name = NamePrefix + id;
        Transform? existing = parent.Find(name);
        if (existing != null)
        {
            return existing.GetComponent<TMP_Text>();
        }

        if (template == null)
        {
            LogOnce(id, "template missing; control not created");
            return null;
        }

        GameObject clone = UnityEngine.Object.Instantiate(template.gameObject, parent);
        clone.name = name;

        TMP_Text text = clone.GetComponent<TMP_Text>();
        text.text = string.Empty;
        return text;
    }

    /// <summary>Re-labels a button created by Create, from the strings table.</summary>
    /// <param name="button">The button to re-label.</param>
    /// <param name="labelKey">The strings table key of the label.</param>
    /// <param name="args">The placeholder values for the key's text.</param>
    public static void SetLabel(Button button, string labelKey, params object?[]? args)
    {
        if (button == null)
        {
            throw new ArgumentNullException(nameof(button));
        }

        if (labelKey is null)
        {
            throw new ArgumentNullException(nameof(labelKey));
        }

        if (!TrySetLabel(button, SingleLine.Of(StringTable.English.Format(labelKey, args))))
        {
            LogOnce(IdOf(button.name), "fallback: no label component");
        }
    }

    private static bool TrySetLabel(Button button, string text)
    {
        TMP_Text? tmp = button.GetComponentInChildren<TMP_Text>(true);
        if (tmp != null)
        {
            tmp.text = text;
            return true;
        }

        Text? legacy = button.GetComponentInChildren<Text>(true);
        if (legacy != null)
        {
            legacy.text = text;
            return true;
        }

        return false;
    }

    private static void EnforceMinimumSize(GameObject clone)
    {
        RectTransform? rect = clone.GetComponent<RectTransform>();
        if (rect == null)
        {
            return;
        }

        Vector2 size = rect.sizeDelta;
        if (size.x < MinWidth || size.y < MinHeight)
        {
            rect.sizeDelta = new Vector2(Mathf.Max(size.x, MinWidth), Mathf.Max(size.y, MinHeight));
        }
    }

    private static string IdOf(string name) =>
        name.StartsWith(NamePrefix, StringComparison.Ordinal) ? name.Substring(NamePrefix.Length) : name;

    private static void LogOnce(string id, string message)
    {
        if (!LogPolicy.IsEnabled(BaseStoragePlugin.LogSink, LogCategory.UiFallback))
        {
            return;
        }

        bool first;
        lock (LogLock)
        {
            first = Logged.Add(id + '\n' + message);
        }

        if (first)
        {
            LogPolicy.Write(BaseStoragePlugin.LogSink, LogCategory.UiFallback, "[" + id + "] " + message);
        }
    }
}
