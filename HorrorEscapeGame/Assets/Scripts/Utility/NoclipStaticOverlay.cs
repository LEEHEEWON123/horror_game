using UnityEngine;
using UnityEngine.UI;

public class NoclipStaticOverlay : MonoBehaviour
{
    private const int TextureSize = 192;
    private const int FrameCount = 6;
    private const int LineCount = 5;
    private const float FrameInterval = 0.045f;
    private const float LineInterval = 0.07f;

    private RawImage _static;
    private Texture2D[] _frames;
    private readonly RectTransform[] _lines = new RectTransform[LineCount];
    private readonly Image[] _lineImages = new Image[LineCount];
    public float CurrentIntensity => _intensity;

    private float _intensity;
    private int _frameIndex;
    private float _frameTimer;
    private float _lineTimer;

    public static NoclipStaticOverlay Create(Transform parent)
    {
        var go = new GameObject("NoclipStaticOverlay", typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var overlay = go.AddComponent<NoclipStaticOverlay>();
        overlay.Initialize();
        return overlay;
    }

    private void Initialize()
    {
        _frames = new Texture2D[FrameCount];
        for (int i = 0; i < FrameCount; i++)
            _frames[i] = BuildNoiseFrame(0.55f + i * 0.08f);

        var staticGo = new GameObject("Static", typeof(RectTransform));
        staticGo.transform.SetParent(transform, false);
        Stretch(staticGo.GetComponent<RectTransform>());
        _static = staticGo.AddComponent<RawImage>();
        _static.texture = _frames[0];
        _static.color = new Color(0.88f, 0.9f, 0.82f, 0f);
        _static.raycastTarget = false;

        for (int i = 0; i < LineCount; i++)
        {
            var lineGo = new GameObject($"Line_{i}", typeof(RectTransform));
            lineGo.transform.SetParent(transform, false);
            var lineRt = lineGo.GetComponent<RectTransform>();
            lineRt.anchorMin = new Vector2(0f, 0f);
            lineRt.anchorMax = new Vector2(0f, 1f);
            lineRt.pivot = new Vector2(0.5f, 0.5f);
            lineRt.sizeDelta = new Vector2(3f, 0f);

            var img = lineGo.AddComponent<Image>();
            img.color = new Color(0.04f, 0.06f, 0.04f, 0f);
            img.raycastTarget = false;
            _lines[i] = lineRt;
            _lineImages[i] = img;
        }

        RandomizeLines();
    }

    public void BringToFront()
    {
        transform.SetAsLastSibling();
    }

    public void SetAlpha(float alpha) => SetIntensity(alpha);

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

    public void SetIntensity(float intensity)
    {
        _intensity = Mathf.Clamp01(intensity);

        if (_static != null)
        {
            var c = _static.color;
            c.a = Mathf.Lerp(0.55f, 1f, _intensity);
            _static.color = c;
        }

        for (int i = 0; i < LineCount; i++)
        {
            if (_lineImages[i] == null) continue;
            var c = _lineImages[i].color;
            c.a = Mathf.Lerp(0.15f, 0.75f, _intensity);
            _lineImages[i].color = c;
        }
    }

    private void Update()
    {
        if (!gameObject.activeInHierarchy || _intensity <= 0.01f || _frames == null || _frames.Length == 0)
            return;

        _frameTimer += Time.unscaledDeltaTime;
        if (_frameTimer >= FrameInterval)
        {
            _frameTimer = 0f;
            _frameIndex = (_frameIndex + 1) % _frames.Length;
            _static.texture = _frames[_frameIndex];
        }

        _lineTimer += Time.unscaledDeltaTime;
        if (_lineTimer >= LineInterval)
        {
            _lineTimer = 0f;
            RandomizeLines();
        }
    }

    private void RandomizeLines()
    {
        for (int i = 0; i < LineCount; i++)
        {
            var rt = _lines[i];
            if (rt == null) continue;

            float x = Random.Range(0.05f, 0.95f);
            rt.anchorMin = new Vector2(x, 0f);
            rt.anchorMax = new Vector2(x, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(Random.Range(2f, 6f), 0f);
        }
    }

    private static Texture2D BuildNoiseFrame(float intensity)
    {
        var tex = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        var pixels = new Color32[TextureSize * TextureSize];
        byte min = (byte)Mathf.Lerp(10, 45, intensity);
        byte max = (byte)Mathf.Lerp(140, 255, intensity);

        for (int i = 0; i < pixels.Length; i++)
        {
            byte v = (byte)Random.Range(min, max);
            pixels[i] = new Color32(v, (byte)(v * 0.94f), (byte)(v * 0.82f), 255);
        }

        int bandCount = Random.Range(2, 5);
        for (int b = 0; b < bandCount; b++)
        {
            int y = Random.Range(0, TextureSize);
            int height = Random.Range(1, 6);
            byte band = (byte)Random.Range(0, 40);
            for (int row = y; row < Mathf.Min(y + height, TextureSize); row++)
            {
                int rowStart = row * TextureSize;
                for (int x = 0; x < TextureSize; x++)
                    pixels[rowStart + x] = new Color32(band, band, (byte)(band * 0.85f), 255);
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply(false);
        return tex;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private void OnDestroy()
    {
        if (_frames == null) return;

        foreach (var frame in _frames)
        {
            if (frame != null)
                Destroy(frame);
        }
    }
}
