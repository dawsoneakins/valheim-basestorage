namespace BaseStorage.Plugin.BaseMenu;

using UnityEngine;

/// <summary>A small magnifying glass for the search box, drawn once: the game has no such icon to borrow.</summary>
internal static class SearchIcon
{
    private const int Size = 40;
    private const float RingX = 17f;
    private const float RingY = 23f;
    private const float RingRadius = 11f;
    private const float RingWidth = 3f;
    private const float HandleWidth = 3f;
    private static Sprite? sprite;

    /// <summary>Gets the sprite: white on clear, so an image tints it.</summary>
    internal static Sprite Sprite => sprite ??= Draw();

    private static Sprite Draw()
    {
        var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
        var pixels = new Color32[Size * Size];
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                byte alpha = (byte)(Mathf.Clamp01(Mathf.Max(Ring(x, y), Handle(x, y))) * 255f);
                pixels[(y * Size) + x] = new Color32(255, 255, 255, alpha);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, Size, Size), new Vector2(0.5f, 0.5f));
    }

    private static float Ring(int x, int y)
    {
        float distance = Mathf.Sqrt(((x - RingX) * (x - RingX)) + ((y - RingY) * (y - RingY)));
        return (RingWidth / 2f) + 0.5f - Mathf.Abs(distance - RingRadius);
    }

    /// <summary>The handle runs down and right from the ring, to the corner.</summary>
    private static float Handle(int x, int y)
    {
        float along = ((x - RingX) + (RingY - y)) / Mathf.Sqrt(2f);
        float across = Mathf.Abs((x - RingX) - (RingY - y)) / Mathf.Sqrt(2f);
        return along < RingRadius || along > RingRadius + 12f ? 0f : (HandleWidth / 2f) + 0.5f - across;
    }
}
