namespace BaseStorage.Plugin.BaseMenu;

using System;
using System.IO;
using System.Reflection;
using UnityEngine;

/// <summary>The Base Storage icon, a gold chest embedded in the plugin, loaded once.</summary>
internal static class BaseMenuIcon
{
    private const string ResourceName = "BaseStorage.icon.png";
    private const string ConversionType = "UnityEngine.ImageConversion, UnityEngine.ImageConversionModule";
    private const string LoadImage = "LoadImage";
    private static Sprite? sprite;

    /// <summary>Gets the sprite, or null when the resource cannot be read.</summary>
    internal static Sprite? Sprite => sprite ??= Load();

    private static Sprite? Load()
    {
        using Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName);
        if (stream == null)
        {
            return null;
        }

        var bytes = new byte[stream.Length];
        int read = stream.Read(bytes, 0, bytes.Length);
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (read != bytes.Length || !Decode(texture, bytes))
        {
            return null;
        }

        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
    }

    // By reflection: the module needs netstandard 2.1, which a net462 project cannot compile against, but the game has it at runtime.
    private static bool Decode(Texture2D texture, byte[] bytes)
    {
        Type? conversion = Type.GetType(ConversionType);
        MethodInfo? load = conversion?.GetMethod(LoadImage, new[] { typeof(Texture2D), typeof(byte[]) });
        return load != null && (bool)load.Invoke(null, new object[] { texture, bytes });
    }
}
