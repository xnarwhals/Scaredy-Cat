using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Composites a low-resolution world render with a full-resolution world-space
/// UI render. The regular screen-space HUD stays above both outputs.
/// </summary>
[DisallowMultipleComponent]
public sealed class PixelComposition : MonoBehaviour
{
    [Header("Cameras")]
    public Camera worldCamera;
    [Tooltip("Renders world-space UI at display resolution. Created automatically when empty.")]
    public Camera worldUICamera;

    [Header("Outputs")]
    public RawImage pixelWorldOutput;
    [SerializeField] private RawImage worldUIOutput;
    [Min(64)] public int pixelHeight = 288;

    [Header("Layers")]
    [SerializeField] private LayerMask worldLayers = ~0;
    [SerializeField] private LayerMask worldUILayers = 1 << 5;

    private RenderTexture worldTexture;
    private RenderTexture worldUITexture;
    private Camera displayCamera;
    private GameObject generatedUICameraObject;
    private GameObject generatedUIOutputObject;
    private int width;
    private int height;
    private int appliedHeight;
    private RenderTexture originalTarget;
    private int originalCullingMask;
    private bool ownsWorldUICamera;

    private void OnEnable()
    {
        if (worldCamera == null || pixelWorldOutput == null)
        {
            Debug.LogError($"{nameof(PixelComposition)} on {name} requires a world camera and pixel output.", this);
            enabled = false;
            return;
        }

        originalTarget = worldCamera.targetTexture;
        originalCullingMask = worldCamera.cullingMask;
        pixelWorldOutput.raycastTarget = false;
        EnsureWorldUIRig();
        PutWorldSpaceCanvasesOnUILayer();
        CreateDisplayCamera();
        Refresh();
    }

    private void LateUpdate()
    {
        Refresh();
        MatchWorldUICamera();
    }

    private void OnDisable()
    {
        if (worldCamera != null)
        {
            if (worldCamera.targetTexture == worldTexture) worldCamera.targetTexture = originalTarget;
            worldCamera.cullingMask = originalCullingMask;
        }

        if (worldUICamera != null && worldUICamera.targetTexture == worldUITexture)
            worldUICamera.targetTexture = null;
        if (worldUICamera != null) worldUICamera.enabled = false;
        if (pixelWorldOutput != null && pixelWorldOutput.texture == worldTexture)
            pixelWorldOutput.texture = null;
        if (worldUIOutput != null && worldUIOutput.texture == worldUITexture)
            worldUIOutput.texture = null;

        if (displayCamera != null) Destroy(displayCamera.gameObject);
        if (ownsWorldUICamera && generatedUICameraObject != null) Destroy(generatedUICameraObject);
        if (generatedUIOutputObject != null) Destroy(generatedUIOutputObject);
        displayCamera = null;
        generatedUICameraObject = null;
        if (ownsWorldUICamera) worldUICamera = null;
        ownsWorldUICamera = false;
        generatedUIOutputObject = null;
        ReleaseTextures();
    }


    private void EnsureWorldUIRig()
    {
        if (worldUICamera == null)
        {
            generatedUICameraObject = new GameObject("World UI Camera", typeof(Camera));
            generatedUICameraObject.hideFlags = HideFlags.HideAndDontSave;
            generatedUICameraObject.transform.SetParent(worldCamera.transform, false);
            worldUICamera = generatedUICameraObject.GetComponent<Camera>();
            ownsWorldUICamera = true;
        }

        // A Canvas with World Space render mode must name the camera that
        // renders it. Layer filtering alone does not reliably route it.
        if (worldUIOutput == null)
        {
            generatedUIOutputObject = new GameObject("World UI Output", typeof(RectTransform), typeof(RawImage));
            generatedUIOutputObject.hideFlags = HideFlags.HideAndDontSave;
            generatedUIOutputObject.transform.SetParent(transform, false);
            RectTransform rect = generatedUIOutputObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            generatedUIOutputObject.transform.SetSiblingIndex(pixelWorldOutput.transform.GetSiblingIndex() + 1);
            worldUIOutput = generatedUIOutputObject.GetComponent<RawImage>();
        }

        worldUIOutput.raycastTarget = false;
    }

    private void PutWorldSpaceCanvasesOnUILayer()
    {
        int uiMask = worldUILayers.value;
        if (uiMask == 0) return;

        Canvas[] canvases = FindObjectsOfType<Canvas>(true);
        foreach (Canvas canvas in canvases)
        {
            if (canvas.renderMode != RenderMode.WorldSpace || (uiMask & (1 << canvas.gameObject.layer)) == 0)
                continue;

            canvas.worldCamera = worldUICamera;
            SetLayerRecursively(canvas.transform, canvas.gameObject.layer);
        }
    }

    private static void SetLayerRecursively(Transform root, int layer)
    {
        root.gameObject.layer = layer;
        for (int i = 0; i < root.childCount; i++)
            SetLayerRecursively(root.GetChild(i), layer);
    }

    private void CreateDisplayCamera()
    {
        GameObject displayObject = new GameObject("Pixel display", typeof(Camera));
        displayObject.hideFlags = HideFlags.HideAndDontSave;
        displayObject.transform.SetParent(transform, false);
        displayCamera = displayObject.GetComponent<Camera>();
        displayCamera.cullingMask = 0;
        displayCamera.clearFlags = CameraClearFlags.Skybox;
        displayCamera.depth = worldCamera.depth - 1;
        displayCamera.targetDisplay = worldCamera.targetDisplay;
        displayCamera.allowHDR = worldCamera.allowHDR;
        displayCamera.allowMSAA = false;
        displayCamera.enabled = true;
    }

    private void Refresh()
    {
        if (worldCamera == null || worldUICamera == null || pixelWorldOutput == null || worldUIOutput == null)
            return;

        int screenWidth = Screen.width;
        int screenHeight = Screen.height;
        if (screenWidth < 64 || screenHeight < 64 || screenWidth > SystemInfo.maxTextureSize || screenHeight > SystemInfo.maxTextureSize)
            return;

        int requestedHeight = Mathf.Clamp(pixelHeight, 64, SystemInfo.maxTextureSize);
        if (worldTexture != null && worldUITexture != null && width == screenWidth && height == screenHeight && appliedHeight == requestedHeight)
            return;

        float aspect = (float)screenWidth / screenHeight;
        int pixelWidth = Mathf.Clamp(Mathf.RoundToInt(requestedHeight * aspect), 1, SystemInfo.maxTextureSize);
        RenderTexture replacementWorld = CreateTexture("Cat Pixel World", pixelWidth, requestedHeight, FilterMode.Point, 24);
        RenderTexture replacementUI = CreateTexture("Cat World UI", screenWidth, screenHeight, FilterMode.Bilinear, 24);
        if (replacementWorld == null || replacementUI == null)
        {
            ReleaseTexture(replacementWorld);
            ReleaseTexture(replacementUI);
            return;
        }

        RenderTexture previousWorld = worldTexture;
        RenderTexture previousUI = worldUITexture;
        worldTexture = replacementWorld;
        worldUITexture = replacementUI;
        width = screenWidth;
        height = screenHeight;
        appliedHeight = requestedHeight;

        worldCamera.cullingMask = worldLayers & ~(int)worldUILayers;
        worldCamera.targetTexture = worldTexture;
        worldCamera.clearFlags = CameraClearFlags.SolidColor;
        worldCamera.backgroundColor = Color.clear;
        worldUICamera.cullingMask = worldUILayers;
        worldUICamera.targetTexture = worldUITexture;
        worldUICamera.clearFlags = CameraClearFlags.SolidColor;
        worldUICamera.backgroundColor = Color.clear;
        worldUICamera.allowHDR = false;
        worldUICamera.allowMSAA = false;
        worldUICamera.enabled = true;
        pixelWorldOutput.texture = worldTexture;
        worldUIOutput.texture = worldUITexture;

        MatchWorldUICamera();
        ReleaseTexture(previousWorld);
        ReleaseTexture(previousUI);
    }

    private void MatchWorldUICamera()
    {
        if (worldCamera == null || worldUICamera == null) return;
        worldUICamera.transform.SetPositionAndRotation(worldCamera.transform.position, worldCamera.transform.rotation);
        worldUICamera.orthographic = worldCamera.orthographic;
        worldUICamera.orthographicSize = worldCamera.orthographicSize;
        worldUICamera.fieldOfView = worldCamera.fieldOfView;
        worldUICamera.nearClipPlane = worldCamera.nearClipPlane;
        worldUICamera.farClipPlane = worldCamera.farClipPlane;
        worldUICamera.usePhysicalProperties = worldCamera.usePhysicalProperties;
        worldUICamera.sensorSize = worldCamera.sensorSize;
        worldUICamera.lensShift = worldCamera.lensShift;
        worldUICamera.gateFit = worldCamera.gateFit;
        worldUICamera.aspect = width > 0 && height > 0 ? (float)width / height : worldCamera.aspect;
    }

    private static RenderTexture CreateTexture(string textureName, int textureWidth, int textureHeight, FilterMode filter, int depthBits)
    {
        var texture = new RenderTexture(textureWidth, textureHeight, depthBits, RenderTextureFormat.ARGB32)
        {
            name = textureName,
            filterMode = filter,
            wrapMode = TextureWrapMode.Clamp,
            antiAliasing = 1,
            useMipMap = false,
            autoGenerateMips = false,
            hideFlags = HideFlags.DontSave
        };
        if (texture.Create() && texture.IsCreated()) return texture;
        ReleaseTexture(texture);
        return null;
    }

    private void ReleaseTextures()
    {
        ReleaseTexture(worldTexture);
        ReleaseTexture(worldUITexture);
        worldTexture = null;
        worldUITexture = null;
    }

    private static void ReleaseTexture(RenderTexture texture)
    {
        if (texture == null) return;
        texture.Release();
        Destroy(texture);
    }
}
