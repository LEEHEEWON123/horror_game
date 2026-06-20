using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneTransitioner : MonoBehaviour
{
    public static SceneTransitioner Instance { get; private set; }

    public Transform OverlayRoot => _overlayRoot;

    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float noclipStaticDuration = 1.0f;
    [SerializeField] private float noclipBlackDuration = 0.4f;
    [SerializeField] private int postLoadFrames = 2;

    private Image _fadePanel;
    private NoclipStaticOverlay _staticOverlay;
    private Transform _overlayRoot;
    private AudioSource _screamSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureFadeOverlay();
        EnsureScreamSource();
        StartCoroutine(FadeIn(fadeDuration));
    }

    public void LoadScene(string sceneName)
    {
        StartCoroutine(FadeAndLoad(sceneName));
    }

    public void LoadSceneNoclip(string sceneName)
    {
        StartCoroutine(StaticTransitionAndLoad(sceneName));
    }

    public void PrepareNoclipTransition()
    {
        EnsureFadeOverlay();
        BeginStaticTransition();
    }

    public void LoadSceneRealityEscape(string sceneName)
    {
        StartCoroutine(RealityEscapeAndLoad(sceneName));
    }

    public void LoadSceneThroughPortal(string sceneName)
    {
        StartCoroutine(PortalThresholdLoad(sceneName));
    }

    public void LoadSceneSeamless(string sceneName)
    {
        EnsureFadeOverlay();
        SetFadeAlpha(0f);
        _fadePanel.raycastTarget = false;
        HideStaticOverlay();
        SceneManager.LoadScene(sceneName);
    }

    public void ShowStaticOverlay(float alpha)
    {
        EnsureFadeOverlay();
        SetFadeAlpha(0f);
        _fadePanel.raycastTarget = false;

        if (_staticOverlay == null) return;

        _staticOverlay.BringToFront();
        _staticOverlay.SetVisible(true);
        _staticOverlay.SetAlpha(Mathf.Clamp01(alpha));
    }

    public IEnumerator FadeOutStaticRoutine(float duration, float targetAlpha = 0f)
    {
        if (_staticOverlay == null || duration <= 0f)
        {
            if (targetAlpha <= 0f)
                HideStaticOverlay();
            else
                _staticOverlay.SetAlpha(targetAlpha);
            yield break;
        }

        float startAlpha = _staticOverlay.CurrentIntensity;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            _staticOverlay.SetAlpha(Mathf.Lerp(startAlpha, targetAlpha, t));
            yield return null;
        }

        if (targetAlpha <= 0.01f)
            HideStaticOverlay();
        else
            _staticOverlay.SetAlpha(targetAlpha);
    }

    public void HideStaticOverlay()
    {
        if (_staticOverlay == null) return;
        _staticOverlay.SetVisible(false);
        _staticOverlay.SetAlpha(0f);
    }

    public IEnumerator FlashWhiteRoutine(float duration)
    {
        EnsureFadeOverlay();
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = 1f - Mathf.Clamp01(elapsed / duration);
            if (_fadePanel != null)
            {
                _fadePanel.color = new Color(0.92f, 0.94f, 0.96f, t * 0.85f);
                _fadePanel.raycastTarget = true;
            }

            yield return null;
        }

        if (_fadePanel != null)
        {
            _fadePanel.color = new Color(0f, 0f, 0f, 1f);
            _fadePanel.raycastTarget = true;
        }
    }

    public IEnumerator FadeInRoutine(float duration = -1f)
    {
        yield return FadeIn(duration < 0f ? fadeDuration : duration);
    }

    public void HoldBlack()
    {
        EnsureFadeOverlay();
        _fadePanel.color = Color.black;
        _fadePanel.raycastTarget = true;
    }

    public void RevealScene()
    {
        EnsureFadeOverlay();
        SetFadeAlpha(0f);
        _fadePanel.raycastTarget = false;
    }

    private void EnsureFadeOverlay()
    {
        if (_overlayRoot == null)
        {
            var canvasGo = new GameObject("TransitionFadeCanvas");
            canvasGo.transform.SetParent(transform, false);
            _overlayRoot = canvasGo.transform;

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;
            canvas.pixelPerfect = false;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            canvasGo.AddComponent<GraphicRaycaster>();
        }

        if (_staticOverlay == null)
        {
            var staticCanvasGo = new GameObject("TransitionStaticCanvas");
            staticCanvasGo.transform.SetParent(transform, false);

            var staticCanvas = staticCanvasGo.AddComponent<Canvas>();
            staticCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            staticCanvas.sortingOrder = 10050;
            staticCanvas.pixelPerfect = false;

            var staticScaler = staticCanvasGo.AddComponent<CanvasScaler>();
            staticScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            staticScaler.referenceResolution = new Vector2(1080, 1920);

            _staticOverlay = NoclipStaticOverlay.Create(staticCanvasGo.transform);
        }

        if (_fadePanel != null)
            return;

        var panelGo = new GameObject("FadePanel", typeof(RectTransform));
        panelGo.transform.SetParent(_overlayRoot, false);

        var rt = panelGo.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        _fadePanel = panelGo.AddComponent<Image>();
        _fadePanel.color = new Color(0f, 0f, 0f, 1f);
        _fadePanel.raycastTarget = true;
    }

    private void BeginStaticTransition()
    {
        EnsureFadeOverlay();
        HideStaticOverlay();
        SetFadeAlpha(0.35f);
        _fadePanel.raycastTarget = false;
    }

    private IEnumerator PortalThresholdLoad(string sceneName)
    {
        EnsureFadeOverlay();
        yield return StartCoroutine(FadeOut(fadeDuration * 0.55f));

        _fadePanel.color = Color.black;
        SceneManager.LoadScene(sceneName);

        for (int i = 0; i < postLoadFrames; i++)
            yield return null;

        yield return StartCoroutine(FadeIn(fadeDuration * 0.45f));
    }

    private IEnumerator FadeAndLoad(string sceneName)
    {
        EnsureFadeOverlay();
        yield return StartCoroutine(FadeOut(fadeDuration));

        _fadePanel.color = Color.black;
        SceneManager.LoadScene(sceneName);

        for (int i = 0; i < postLoadFrames; i++)
            yield return null;

        yield return StartCoroutine(FadeIn(fadeDuration));
    }

    private IEnumerator StaticTransitionAndLoad(string sceneName)
    {
        EnsureFadeOverlay();
        HideStaticOverlay();
        PlayScream();

        float totalFade = noclipStaticDuration + noclipBlackDuration;
        float elapsed = 0f;
        while (elapsed < totalFade)
        {
            elapsed += Time.unscaledDeltaTime;
            SetFadeAlpha(Mathf.Clamp01(elapsed / totalFade));
            yield return null;
        }

        SetFadeAlpha(1f);
        SceneManager.LoadScene(sceneName);

        for (int i = 0; i < postLoadFrames; i++)
            yield return null;

        if (NoclipEntryState.HasPending())
            HoldBlack();
        else if (sceneName == "Map_00")
            yield return StartCoroutine(FadeIn(fadeDuration));
        else
            HoldBlack();
    }

    private IEnumerator RealityEscapeAndLoad(string sceneName)
    {
        EnsureFadeOverlay();
        yield return StartCoroutine(FadeOut(fadeDuration));

        SceneManager.LoadScene(sceneName);

        for (int i = 0; i < postLoadFrames; i++)
            yield return null;

        HoldBlack();
    }

    private IEnumerator FadeOut(float duration)
    {
        if (_fadePanel == null) yield break;

        float elapsed = 0f;
        float startAlpha = _fadePanel.color.a;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetFadeAlpha(Mathf.Lerp(startAlpha, 1f, elapsed / duration));
            yield return null;
        }

        SetFadeAlpha(1f);
        _fadePanel.raycastTarget = true;
    }

    private IEnumerator FadeIn(float duration)
    {
        if (_fadePanel == null) yield break;

        float elapsed = 0f;
        SetFadeAlpha(1f);
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetFadeAlpha(1f - Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        SetFadeAlpha(0f);
        _fadePanel.raycastTarget = false;
    }

    private void EnsureScreamSource()
    {
        if (_screamSource != null) return;
        _screamSource = gameObject.AddComponent<AudioSource>();
        _screamSource.clip = FallScreamSynth.CreateClip();
        _screamSource.loop = false;
        _screamSource.spatialBlend = 0f;
        _screamSource.volume = 0.75f;
        _screamSource.playOnAwake = false;
    }

    private void PlayScream()
    {
        if (_screamSource == null) return;
        _screamSource.Stop();
        _screamSource.Play();
    }

    private void SetFadeAlpha(float alpha)
    {
        if (_fadePanel == null) return;
        var color = Color.black;
        color.a = Mathf.Clamp01(alpha);
        _fadePanel.color = color;
    }
}
