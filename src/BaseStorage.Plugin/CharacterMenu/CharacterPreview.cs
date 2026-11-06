namespace BaseStorage.Plugin.CharacterMenu;

using System.Collections.Generic;
using BaseStorage.Plugin.Ui;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>
/// The live 3D view: a stripped copy of the player model, far under the world, seen by its own camera. The copy's game components are removed
/// before it ever wakes, so it never registers as a character or player. The objects belong to the scene and go with it; the texture does not, so
/// it is released by hand. Main thread only.
/// </summary>
internal static class CharacterPreview
{
    private const int ViewLayer = 31;
    private const float Distance = 4.4f;
    private const float FocusHeight = 0.95f;
    private const int BackdropSize = 256;
    private const float FieldOfView = 30f;
    private const float TextureScale = 1.5f;
    private static readonly Vector3 RigPosition = new Vector3(0f, -3000f, 0f);
    private static readonly List<Component> Scratch = new List<Component>();

    private static GameObject? rig;
    private static GameObject? character;
    private static VisEquipment? visuals;
    private static Camera? view;
    private static RenderTexture? texture;
    private static Texture2D? backdrop;
    private static RawImage? image;
    private static float yaw;
    private static bool failed;

    /// <summary>Creates the image the view is drawn on, on a panel that was just built.</summary>
    /// <param name="parent">The panel.</param>
    /// <param name="size">The image's size.</param>
    /// <param name="position">The image's position.</param>
    internal static void BuildImage(Transform parent, Vector2 size, Vector2 position)
    {
        Release();
        failed = false;
        GameObject node = VanillaButtonFactory.CreateNode(parent, "character.view", size, position, typeof(RectTransform), typeof(RawImage));
        node.AddComponent<CharacterViewDrag>();
        image = node.GetComponent<RawImage>();
        image.enabled = false;
        texture = new RenderTexture(Mathf.Max(64, (int)(size.x * TextureScale)), Mathf.Max(64, (int)(size.y * TextureScale)), 16, RenderTextureFormat.ARGB32);
    }

    /// <summary>Updates the view while the panel shows. A failure turns the view off and leaves the slots working.</summary>
    /// <param name="player">The player.</param>
    internal static void Refresh(Player player)
    {
        if (failed || image == null || texture == null)
        {
            return;
        }

        try
        {
            if (rig == null)
            {
                BuildRig();
            }

            character!.SetActive(true);
            view!.enabled = true;
            image.enabled = true;
            player.SetupVisEquipment(visuals, false);
            SetLayer(character.transform);
            character.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        }
        catch
        {
            failed = true;
            image.enabled = false;
            if (rig != null)
            {
                Object.Destroy(rig);
                rig = null;
            }

            throw;
        }
    }

    /// <summary>Stops drawing while the panel is hidden, so a closed inventory costs nothing.</summary>
    internal static void Pause()
    {
        if (view != null)
        {
            view.enabled = false;
        }

        if (character != null)
        {
            character.SetActive(false);
        }
    }

    /// <summary>Turns the character at once: drag events arrive every frame, while the panel's refresh runs only a few times a second.</summary>
    /// <param name="degrees">The degrees to turn.</param>
    internal static void Turn(float degrees)
    {
        yaw -= degrees;
        if (character != null)
        {
            character.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        }
    }

    private static void BuildRig()
    {
        GameObject prefab = Game.instance.m_playerPrefab;
        rig = VanillaButtonFactory.CreateWorldNode(null, "character.view.rig");
        rig.transform.position = RigPosition;

        // The copy is born asleep, so its game components can go before they ever wake.
        character = VanillaButtonFactory.CopyAsleep(prefab, rig.transform);
        Strip(character);
        Strip(character);
        if (character.GetComponent<Character>() != null)
        {
            throw new System.InvalidOperationException("The character view's copy kept its Character component.");
        }

        visuals = character.GetComponent<VisEquipment>();
        ZNetView.m_forceDisableInit = true;
        try
        {
            character.SetActive(true);
        }
        finally
        {
            ZNetView.m_forceDisableInit = false;
        }

        foreach (Animator animator in character.GetComponentsInChildren<Animator>(true))
        {
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            animator.fireEvents = false;
            animator.SetBool("onGround", true);
        }

        GameObject cameraNode = VanillaButtonFactory.CreateWorldNode(rig.transform, "character.view.camera");
        cameraNode.transform.position = RigPosition + new Vector3(0f, FocusHeight, Distance);
        cameraNode.transform.LookAt(RigPosition + new Vector3(0f, FocusHeight, 0f));
        view = cameraNode.AddComponent<Camera>();
        view.cullingMask = 1 << ViewLayer;
        view.clearFlags = CameraClearFlags.SolidColor;
        view.backgroundColor = new Color(0.07f, 0.045f, 0.03f, 1f);
        view.renderingPath = RenderingPath.Forward;
        view.fieldOfView = FieldOfView;
        view.nearClipPlane = 0.1f;
        view.farClipPlane = 20f;
        view.allowHDR = false;
        view.allowMSAA = false;
        view.depth = -100f;
        view.targetTexture = texture;
        cameraNode.AddComponent<CharacterViewFog>();
        backdrop = MakeBackdrop();
        var drawBackdrop = new CommandBuffer();
        drawBackdrop.Blit(backdrop, BuiltinRenderTextureType.CameraTarget);
        view.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, drawBackdrop);

        GameObject lightNode = VanillaButtonFactory.CreateWorldNode(rig.transform, "character.view.light");
        lightNode.transform.rotation = Quaternion.Euler(30f, 200f, 0f);
        Light light = lightNode.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.1f;
        light.cullingMask = 1 << ViewLayer;
        light.shadows = LightShadows.None;

        image!.texture = texture;
    }

    /// <summary>Paints the backdrop: a dark recess in the wood with a soft firelight glow at the character's feet. A faint dither hides colour bands.</summary>
    private static Texture2D MakeBackdrop()
    {
        var shadow = new Color(0.05f, 0.032f, 0.022f);
        var recess = new Color(0.1f, 0.065f, 0.04f);
        var ember = new Color(0.46f, 0.23f, 0.08f);
        var pixels = new Color32[BackdropSize * BackdropSize];
        for (int y = 0; y < BackdropSize; y++)
        {
            float up = y / (BackdropSize - 1f);
            for (int x = 0; x < BackdropSize; x++)
            {
                float across = (x / (BackdropSize - 1f)) - 0.5f;
                Color color = Color.Lerp(recess, shadow, up);
                float distance = Mathf.Sqrt((across * across * 3f) + (up * up * 1.6f));
                color = Color.Lerp(color, ember, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(1f - (distance * 1.25f))) * 0.75f);
                float dither = (((x * 37) + (y * 17)) % 7 - 3) * 0.0012f;
                color = new Color(color.r + dither, color.g + dither, color.b + dither, 1f);
                pixels[(y * BackdropSize) + x] = color;
            }
        }

        var painted = new Texture2D(BackdropSize, BackdropSize, TextureFormat.RGBA32, false);
        painted.wrapMode = TextureWrapMode.Clamp;
        painted.SetPixels32(pixels);
        painted.Apply();
        return painted;
    }

    /// <summary>Removes everything from the copy except what draws it. Run twice, because a component another one requires can only go second.</summary>
    private static void Strip(GameObject copy)
    {
        copy.GetComponentsInChildren(true, Scratch);
        for (int i = Scratch.Count - 1; i >= 0; i--)
        {
            Component part = Scratch[i];
            if (part != null && !(part is Transform || part is VisEquipment || part is ZNetView || part is Animator || part is Renderer || part is MeshFilter || part is LODGroup))
            {
                Object.DestroyImmediate(part);
            }
        }

        Scratch.Clear();
    }

    /// <summary>Puts the copy and any gear attached since on the layer only the view's camera sees.</summary>
    private static void SetLayer(Transform node)
    {
        node.gameObject.layer = ViewLayer;
        for (int i = 0; i < node.childCount; i++)
        {
            SetLayer(node.GetChild(i));
        }
    }

    private static void Release()
    {
        if (rig != null)
        {
            Object.Destroy(rig);
        }

        rig = null;
        character = null;
        visuals = null;
        view = null;
        if (texture != null)
        {
            texture.Release();
            Object.Destroy(texture);
        }

        texture = null;
        if (backdrop != null)
        {
            Object.Destroy(backdrop);
        }

        backdrop = null;
        image = null;
    }
}

/// <summary>Turns the world's fog off for the view's camera alone, because the character is seen far below the world, where it would wash out.</summary>
internal sealed class CharacterViewFog : MonoBehaviour
{
    private bool fog;

    private void OnPreRender()
    {
        fog = RenderSettings.fog;
        RenderSettings.fog = false;
    }

    private void OnPostRender() => RenderSettings.fog = fog;
}

/// <summary>Turns the character when the view is dragged.</summary>
internal sealed class CharacterViewDrag : MonoBehaviour, IDragHandler
{
    /// <inheritdoc />
    public void OnDrag(PointerEventData eventData) => CharacterPreview.Turn(eventData.delta.x);
}
