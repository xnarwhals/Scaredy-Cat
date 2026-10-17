using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>
/// Renders a crisp skybox directly to the display, then composites a
/// pixel-resolution 3D world and full-resolution world-space UI over it.
/// </summary>
[DisallowMultipleComponent]
public sealed class PixelComposition : MonoBehaviour
{
    [Header("Cameras")]
    [SerializeField] private Camera worldCamera;
    [SerializeField] private Camera worldUICamera;
    [SerializeField] private Camera skyboxCamera;

    [Header("Output")]
    [SerializeField] private RawImage pixelWorldOutput;
    [SerializeField] private RawImage worldUIOutput;
    [SerializeField, Min(1)] private int pixelHeight = 288;

    [Header("Layers")]
    [SerializeField] private LayerMask worldLayers = 23;
    [SerializeField] private LayerMask worldUILayers = 64;

    private RenderTexture worldTexture;
    private RenderTexture worldUITexture;
    private int outputWidth;
    private int outputHeight;
    private int appliedPixelHeight;

    private void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += HandleBeginCameraRendering;
        ConfigureCameras();
        EnsureRenderTextures();
    }

    private void Update()
    {
        EnsureRenderTextures();
    }

    private void LateUpdate()
    {
        // DynamicCamera can update the master lens during Update/coroutines.
        // Copy it after that work and before any camera renders this frame.
        MatchAuxiliaryCameraLenses();
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= HandleBeginCameraRendering;
        ReleaseRenderTextures();
    }

    private void HandleBeginCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        if (camera == worldCamera)
        {
            ClearRenderTexture(worldTexture, worldCamera.backgroundColor);
        }
        else if (camera == worldUICamera)
        {
            ClearRenderTexture(worldUITexture, Color.clear);
        }
    }

    private static void ClearRenderTexture(RenderTexture texture, Color clearColor)
    {
        if (texture == null || !texture.IsCreated())
        {
            return;
        }

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = texture;
        GL.Clear(true, true, clearColor);
        RenderTexture.active = previous;
    }

    private void ConfigureCameras()
    {
        if (worldCamera == null || worldUICamera == null || skyboxCamera == null)
        {
            Debug.LogError(
                $"{nameof(PixelCameraComposition)} on {name} requires world, World UI, and skybox cameras.",
                this);
            enabled = false;
            return;
        }

        // The skybox is the only camera that renders directly to the display.
        // It supplies a stable, opaque background before the two transparent
        // RenderTextures are drawn by the Screen Space Overlay canvas.
        skyboxCamera.cullingMask = 0;
        skyboxCamera.clearFlags = CameraClearFlags.Skybox;
        skyboxCamera.targetTexture = null;

        worldCamera.cullingMask = worldLayers;
        worldCamera.clearFlags = CameraClearFlags.SolidColor;
        worldCamera.backgroundColor = Color.clear;

        worldUICamera.cullingMask = worldUILayers;
        worldUICamera.clearFlags = CameraClearFlags.SolidColor;
        worldUICamera.backgroundColor = Color.clear;
        worldUICamera.allowHDR = false;
        worldUICamera.allowMSAA = false;
    }

    private void EnsureRenderTextures()
    {
        if (!enabled || worldCamera == null || worldUICamera == null)
        {
            return;
        }

        if (!TryGetOutputSize(out int screenWidth, out int screenHeight))
        {
            return;
        }

        int requestedPixelHeight = Mathf.Max(1, pixelHeight);
        if (worldTexture != null && worldUITexture != null &&
            outputWidth == screenWidth && outputHeight == screenHeight &&
            appliedPixelHeight == requestedPixelHeight)
        {
            return;
        }

        float aspect = (float)screenWidth / screenHeight;
        int pixelWidth = Mathf.Max(1, Mathf.RoundToInt(requestedPixelHeight * aspect));
        RenderTexture replacementWorld = CreateRenderTexture(
            "Pixel World (Runtime)", pixelWidth, requestedPixelHeight, FilterMode.Point, 24);
        RenderTexture replacementWorldUI = CreateRenderTexture(
            "World UI (Runtime)", screenWidth, screenHeight, FilterMode.Bilinear, 24);

        // Keep displaying the last valid frame if Unity briefly reports an
        // invalid Game-view size during a domain reload or layout transition.
        if (replacementWorld == null || replacementWorldUI == null)
        {
            ReleaseRenderTexture(ref replacementWorld);
            ReleaseRenderTexture(ref replacementWorldUI);
            return;
        }

        RenderTexture previousWorld = worldTexture;
        RenderTexture previousWorldUI = worldUITexture;
        worldTexture = replacementWorld;
        worldUITexture = replacementWorldUI;
        outputWidth = screenWidth;
        outputHeight = screenHeight;
        appliedPixelHeight = requestedPixelHeight;

        worldCamera.targetTexture = worldTexture;
        worldUICamera.targetTexture = worldUITexture;

        if (pixelWorldOutput != null)
        {
            pixelWorldOutput.texture = worldTexture;
        }

        if (worldUIOutput != null)
        {
            worldUIOutput.texture = worldUITexture;
        }

        ReleaseRenderTexture(ref previousWorld);
        ReleaseRenderTexture(ref previousWorldUI);
        MatchAuxiliaryCameraLenses();
    }

    private bool TryGetOutputSize(out int width, out int height)
    {
        width = Screen.width;
        height = Screen.height;
        if (IsValidOutputSize(width, height))
        {
            return true;
        }

        if (Display.main != null)
        {
            width = Display.main.renderingWidth;
            height = Display.main.renderingHeight;
            if (IsValidOutputSize(width, height))
            {
                return true;
            }
        }

        // When valid textures already exist, retaining them is safer than
        // replacing them with dimensions reported during an editor transition.
        width = outputWidth;
        height = outputHeight;
        return worldTexture != null && worldUITexture != null &&
            IsValidOutputSize(width, height);
    }

    private static bool IsValidOutputSize(int width, int height)
    {
        if (width < 64 || height < 64 ||
            width > SystemInfo.maxTextureSize || height > SystemInfo.maxTextureSize)
        {
            return false;
        }

        float aspect = (float)width / height;
        return aspect >= 0.5f && aspect <= 4f;
    }

    private static RenderTexture CreateRenderTexture(
        string textureName,
        int width,
        int height,
        FilterMode filterMode,
        int depthBits)
    {
        var texture = new RenderTexture(
            width,
            height,
            depthBits,
            RenderTextureFormat.ARGB32,
            RenderTextureReadWrite.Default)
        {
            name = textureName,
            filterMode = filterMode,
            wrapMode = TextureWrapMode.Clamp,
            useMipMap = false,
            autoGenerateMips = false,
            antiAliasing = 1,
            hideFlags = HideFlags.DontSave
        };

        if (texture.Create() && texture.IsCreated())
        {
            return texture;
        }

        Destroy(texture);
        return null;
    }

    private void MatchAuxiliaryCameraLenses()
    {
        if (worldCamera == null || worldUICamera == null || skyboxCamera == null)
        {
            return;
        }

        // The UI camera is a zeroed child of the world camera, so it already
        // shares its pose and shake. Matching the lens makes a world-space
        // canvas land on the exact same screen coordinates in both textures.
        worldUICamera.orthographic = worldCamera.orthographic;
        worldUICamera.orthographicSize = worldCamera.orthographicSize;
        worldUICamera.fieldOfView = worldCamera.fieldOfView;
        worldUICamera.nearClipPlane = worldCamera.nearClipPlane;
        worldUICamera.farClipPlane = worldCamera.farClipPlane;
        worldUICamera.usePhysicalProperties = worldCamera.usePhysicalProperties;
        worldUICamera.sensorSize = worldCamera.sensorSize;
        worldUICamera.lensShift = worldCamera.lensShift;
        worldUICamera.gateFit = worldCamera.gateFit;

        skyboxCamera.orthographic = worldCamera.orthographic;
        skyboxCamera.orthographicSize = worldCamera.orthographicSize;
        skyboxCamera.fieldOfView = worldCamera.fieldOfView;
        skyboxCamera.nearClipPlane = worldCamera.nearClipPlane;
        skyboxCamera.farClipPlane = worldCamera.farClipPlane;
        skyboxCamera.usePhysicalProperties = worldCamera.usePhysicalProperties;
        skyboxCamera.sensorSize = worldCamera.sensorSize;
        skyboxCamera.lensShift = worldCamera.lensShift;
        skyboxCamera.gateFit = worldCamera.gateFit;

        if (outputHeight > 0)
        {
            float aspect = (float)outputWidth / outputHeight;
            worldCamera.aspect = aspect;
            worldUICamera.aspect = aspect;
            skyboxCamera.aspect = aspect;
        }
    }

    private void ReleaseRenderTextures()
    {
        if (worldCamera != null && worldCamera.targetTexture == worldTexture)
        {
            worldCamera.targetTexture = null;
        }

        if (worldUICamera != null && worldUICamera.targetTexture == worldUITexture)
        {
            worldUICamera.targetTexture = null;
        }

        ReleaseRenderTexture(ref worldTexture);
        ReleaseRenderTexture(ref worldUITexture);
    }

    private static void ReleaseRenderTexture(ref RenderTexture texture)
    {
        if (texture == null)
        {
            return;
        }

        texture.Release();
        Destroy(texture);
        texture = null;
    }
}
