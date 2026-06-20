using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PortalNextMapPreview : MonoBehaviour
{
    private const int RenderSize = 768;
    private const float PreviewWorldOffset = 10000f;

    private Transform _previewRoot;
    private Camera _portalCamera;
    private Camera _mainCamera;
    private RenderTexture _renderTexture;
    private bool _loading;
    private int _mainCameraMask = -1;

    public string NextSceneName => PortalTransitionState.NextScene;

    public void Initialize(PortalTransitionKind kind, Renderer portalView)
    {
        if (kind != PortalTransitionKind.Dimension || portalView == null)
            return;

        string current = SceneManager.GetActiveScene().name;
        if (!DimensionMapPool.IsRandomMap(current))
            return;

        if (!PortalTransitionState.HasNext)
            PortalTransitionState.Prepare(current);

        StartCoroutine(LoadPreviewRoutine(portalView));
    }

    private IEnumerator LoadPreviewRoutine(Renderer portalView)
    {
        if (_loading || string.IsNullOrEmpty(PortalTransitionState.NextScene))
            yield break;

        _loading = true;
        string sceneName = PortalTransitionState.NextScene;

        // Build geometry-only preview in the active scene. Loading the full next map
        // additively duplicates cameras/lights and breaks portal scene transitions.
        _previewRoot = new GameObject("PortalPreviewRoot").transform;
        _previewRoot.SetPositionAndRotation(
            new Vector3(PreviewWorldOffset, MapFloor.WalkY, PreviewWorldOffset),
            Quaternion.identity);

        PortalTransitionState.UseGenerationSeed();
        var build = DimensionMapPreviewBuilder.Build(sceneName, _previewRoot);
        MapGenerationContext.Clear();

        if (build.Root == null)
        {
            UnloadPreview();
            yield break;
        }

        PositionPreview(_previewRoot, build.Bounds);
        PortalPreviewLayer.ApplyRecursively(_previewRoot.gameObject);
        PortalPreviewLayer.DisablePhysicsRecursively(_previewRoot.gameObject);
        AddPreviewLighting(_previewRoot);
        SetupPortalCamera(_previewRoot);
        if (_portalCamera != null)
            _portalCamera.Render();

        if (!PortalViewMaterial.TryApplyTexture(portalView, _renderTexture))
            Debug.LogWarning("[PortalNextMapPreview] Could not assign portal preview material; keeping fallback color.");

        HidePreviewFromMainCamera();

        _loading = false;
        yield break;
    }

    private void PositionPreview(Transform previewRoot, Bounds bounds)
    {
        if (previewRoot.childCount > 0)
        {
            var mapRoot = previewRoot.GetChild(0);
            mapRoot.localPosition = new Vector3(-bounds.center.x, 0f, -bounds.min.z + 3f);
        }
    }

    private void AddPreviewLighting(Transform previewRoot)
    {
        var sunGo = new GameObject("PreviewSun");
        sunGo.transform.SetParent(previewRoot, false);
        sunGo.transform.rotation = Quaternion.Euler(58f, 25f, 0f);
        var sun = sunGo.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 0.95f;
        sun.color = new Color(0.9f, 0.96f, 0.82f);
        sun.shadows = LightShadows.None;
        PortalPreviewLayer.ApplyRecursively(sunGo);

        var fillGo = new GameObject("PreviewFill");
        fillGo.transform.SetParent(previewRoot, false);
        fillGo.transform.localPosition = new Vector3(0f, 2f, 4f);
        var fill = fillGo.AddComponent<Light>();
        fill.type = LightType.Point;
        fill.range = 24f;
        fill.intensity = 0.55f;
        fill.color = new Color(0.82f, 0.9f, 0.76f);
        PortalPreviewLayer.ApplyRecursively(fillGo);
    }

    private void SetupPortalCamera(Transform previewRoot)
    {
        var camGo = new GameObject("PortalPreviewCamera");
        camGo.transform.SetParent(previewRoot, false);
        camGo.transform.localPosition = new Vector3(0f, 1.35f, -3f);
        camGo.transform.localRotation = Quaternion.identity;

        _portalCamera = camGo.AddComponent<Camera>();
        _portalCamera.clearFlags = CameraClearFlags.SolidColor;
        _portalCamera.backgroundColor = new Color(0.05f, 0.06f, 0.05f, 1f);
        _portalCamera.fieldOfView = 72f;
        _portalCamera.nearClipPlane = 0.08f;
        _portalCamera.farClipPlane = 90f;
        _portalCamera.depth = -10f;
        _portalCamera.useOcclusionCulling = false;
        _portalCamera.cullingMask = PortalPreviewLayer.Mask;

        _renderTexture = new RenderTexture(RenderSize, RenderSize, 16, RenderTextureFormat.ARGB32)
        {
            name = "PortalPreviewRT",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        _renderTexture.Create();
        _portalCamera.targetTexture = _renderTexture;
    }

    private void HidePreviewFromMainCamera()
    {
        _mainCamera = Camera.main;
        if (_mainCamera == null) return;

        _mainCameraMask = _mainCamera.cullingMask;
        PortalPreviewLayer.HideFromCamera(_mainCamera);
    }

    public void UnloadPreview()
    {
        if (_mainCameraMask >= 0)
        {
            PortalPreviewLayer.RestoreCamera(_mainCamera != null ? _mainCamera : Camera.main, _mainCameraMask);
            _mainCameraMask = -1;
            _mainCamera = null;
        }

        if (_portalCamera != null)
            Destroy(_portalCamera.gameObject);

        if (_renderTexture != null)
        {
            _renderTexture.Release();
            Destroy(_renderTexture);
        }

        if (_previewRoot != null)
            Destroy(_previewRoot.gameObject);

        _previewRoot = null;
        _portalCamera = null;
        _renderTexture = null;
        _loading = false;
    }

    private void OnDestroy()
    {
        UnloadPreview();
    }
}
