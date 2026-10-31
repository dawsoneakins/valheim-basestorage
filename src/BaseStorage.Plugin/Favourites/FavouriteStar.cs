namespace BaseStorage.Plugin.Favourites;

using UnityEngine;

/// <summary>
/// The favourite marker's picture: a rounded star with a dark outline, drawn once in code. The fill is white with a soft top-to-bottom
/// shade, so the marker's own colour tints it and the outline stays dark.
/// </summary>
internal static class FavouriteStar
{
    private const int Size = 64;
    private const float Radius = 22f;
    private const float Waist = 0.5f;
    private const float Rounding = 3f;
    private const float Outline = 5f;
    private const float BottomShade = 0.78f;
    private const float MirrorX = 0.809017f;
    private const float MirrorY = -0.587785f;
    private static readonly Color OutlineColor = new Color(0.12f, 0.07f, 0.02f, 1f);
    private static Sprite? sprite;

    /// <summary>Gets the sprite.</summary>
    internal static Sprite Sprite => sprite ??= Draw();

    private static Sprite Draw()
    {
        var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, true);
        var pixels = new Color[Size * Size];
        float centre = (Size - 1) / 2f;
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                float distance = DistanceToStar(new Vector2(x - centre, y - centre + 2f)) - Rounding;
                float shade = Mathf.Lerp(BottomShade, 1f, (float)y / (Size - 1));
                Color colour = Color.Lerp(OutlineColor, new Color(shade, shade, shade, 1f), Mathf.Clamp01(0.5f - distance));
                colour.a = Mathf.Clamp01(0.5f - (distance - Outline));
                pixels[(y * Size) + x] = colour;
            }
        }

        texture.SetPixels(pixels);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Trilinear;
        texture.Apply(true);
        return Sprite.Create(texture, new Rect(0f, 0f, Size, Size), new Vector2(0.5f, 0.5f));
    }

    /// <summary>Folds the point into one tip of the star and measures to that edge: negative inside, positive outside.</summary>
    private static float DistanceToStar(Vector2 point)
    {
        var right = new Vector2(MirrorX, MirrorY);
        var left = new Vector2(-MirrorX, MirrorY);
        point.x = Mathf.Abs(point.x);
        point -= 2f * Mathf.Max(0f, Vector2.Dot(right, point)) * right;
        point -= 2f * Mathf.Max(0f, Vector2.Dot(left, point)) * left;
        point.x = Mathf.Abs(point.x);
        point.y -= Radius;

        var edge = new Vector2(Waist * -MirrorY, (Waist * MirrorX) - 1f);
        float along = Mathf.Clamp(Vector2.Dot(point, edge) / Vector2.Dot(edge, edge), 0f, Radius);
        return (point - (edge * along)).magnitude * Mathf.Sign((point.y * edge.x) - (point.x * edge.y));
    }
}
