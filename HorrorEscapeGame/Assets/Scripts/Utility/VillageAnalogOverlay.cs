using UnityEngine;
using UnityEngine.UI;

public class VillageAnalogOverlay : MonoBehaviour
{
    private const int NoiseSize = 128;
    private const int NoiseFrames = 4;
    private const int LineCount = 3;
    private const float NoiseInterval = 0.09f;
    private const float LineInterval = 0.14f;
    private const float ScanlineDriftSpeed = 4f;

    private RawImage _noise;
    private RawImage _vignette;
    private Image _warmTint;
    private Image _scanBand;
    private Texture2D[] _noiseFrames;
    private Texture2D _vignetteTexture;
    private readonly RectTransform[] _lines = new RectTransform[LineCount];
    private readonly Image[] _lineImages = new Image[LineCount];
    private int _frameIndex;
    private float _frameTimer;
    private float _lineTimer;
    private float _scanlineOffset;
    private bool _subtle;

    public static VillageAnalogOverlay Attach(bool subtle = false)
    {
        var canvasGo = new GameObject(subtle ? "BackroomsAnalogCanvas" : "VillageAnalogCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 8200;
        canvas.pixelPerfect = false;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);

        var overlay = canvasGo.AddComponent<VillageAnalogOverlay>();
        overlay._subtle = subtle;
        overlay.Initialize();
        return overlay;
    }

    private void Initialize()
    {
        _noiseFrames = new Texture2D[NoiseFrames];
        for (int i = 0; i < NoiseFrames; i++)
            _noiseFrames[i] = BuildNoiseFrame(0.35f + i * 0.05f);

        _vignetteTexture = BuildVignetteTexture(256);

        CreateWarmTint();
        CreateVignette();
        CreateNoise();
        CreateScanBand();
        CreateTrackingLines();
    }

    private void CreateWarmTint()
    {
        var go = new GameObject("WarmTint", typeof(RectTransform));
        go.transform.SetParent(transform, false);
        Stretch(go.GetComponent<RectTransform>());
        _warmTint = go.AddComponent<Image>();
        _warmTint.color = new Color(0.96f, 0.88f, 0.72f, 0.07f);
        _warmTint.raycastTarget = false;
    }

    private void CreateVignette()
    {
        var go = new GameObject("Vignette", typeof(RectTransform));
        go.transform.SetParent(transform, false);
        Stretch(go.GetComponent<RectTransform>());
        _vignette = go.AddComponent<RawImage>();
        _vignette.texture = _vignetteTexture;
        _vignette.color = new Color(1f, 1f, 1f, _subtle ? 0.48f : 0.72f);
        _vignette.raycastTarget = false;
    }

    private void CreateNoise()
    {
        var go = new GameObject("Grain", typeof(RectTransform));
        go.transform.SetParent(transform, false);
        Stretch(go.GetComponent<RectTransform>());
        _noise = go.AddComponent<RawImage>();
        _noise.texture = _noiseFrames[0];
        _noise.color = new Color(0.82f, 0.8f, 0.72f, _subtle ? 0.055f : 0.1f);
        _noise.raycastTarget = false;
    }

    private void CreateScanBand()
    {
        var go = new GameObject("ScanBand", typeof(RectTransform));
        go.transform.SetParent(transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(1f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(0f, 3f);
        rt.anchoredPosition = Vector2.zero;

        _scanBand = go.AddComponent<Image>();
        _scanBand.color = new Color(0.04f, 0.05f, 0.04f, _subtle ? 0.06f : 0.12f);
        _scanBand.raycastTarget = false;
    }

    private void CreateTrackingLines()
    {
        for (int i = 0; i < LineCount; i++)
        {
            var lineGo = new GameObject($"TrackLine_{i}", typeof(RectTransform));
            lineGo.transform.SetParent(transform, false);
            var lineRt = lineGo.GetComponent<RectTransform>();
            lineRt.anchorMin = new Vector2(0f, 0f);
            lineRt.anchorMax = new Vector2(0f, 1f);
            lineRt.pivot = new Vector2(0.5f, 0.5f);
            lineRt.sizeDelta = new Vector2(2f, 0f);

            var img = lineGo.AddComponent<Image>();
            img.color = new Color(0.03f, 0.04f, 0.03f, _subtle ? 0.04f : 0.08f);
            img.raycastTarget = false;
            _lines[i] = lineRt;
            _lineImages[i] = img;
        }

        RandomizeLines();
    }

    private void Update()
    {
        float noiseInterval = _subtle ? 0.45f : NoiseInterval;
        float lineInterval  = _subtle ? 0.9f  : LineInterval;

        _frameTimer += Time.deltaTime;
        if (_frameTimer >= noiseInterval)
        {
            _frameTimer = 0f;
            _frameIndex = (_frameIndex + 1) % _noiseFrames.Length;
            _noise.texture = _noiseFrames[_frameIndex];

            var c = _noise.color;
            c.a = _subtle ? 0.04f : Random.Range(0.08f, 0.13f); // subtle은 고정 알파
            _noise.color = c;
        }

        _lineTimer += Time.deltaTime;
        if (_lineTimer >= lineInterval)
        {
            _lineTimer = 0f;
            RandomizeLines();
        }

        _scanlineOffset += Time.deltaTime * ScanlineDriftSpeed;
        if (_scanlineOffset > 1f)
            _scanlineOffset -= 1f;

        var scanRt = _scanBand.rectTransform;
        scanRt.anchorMin = new Vector2(0f, _scanlineOffset);
        scanRt.anchorMax = new Vector2(1f, _scanlineOffset);
        scanRt.anchoredPosition = Vector2.zero;
    }

    private void RandomizeLines()
    {
        for (int i = 0; i < LineCount; i++)
        {
            var rt = _lines[i];
            if (rt == null) continue;

            float x = Random.Range(0.08f, 0.92f);
            rt.anchorMin = new Vector2(x, 0f);
            rt.anchorMax = new Vector2(x, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(Random.Range(1.5f, 4f), 0f);
        }
    }

    private static Texture2D BuildNoiseFrame(float intensity)
    {
        var tex = new Texture2D(NoiseSize, NoiseSize, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        var pixels = new Color32[NoiseSize * NoiseSize];
        byte min = (byte)Mathf.Lerp(30, 55, intensity);
        byte max = (byte)Mathf.Lerp(120, 190, intensity);

        for (int i = 0; i < pixels.Length; i++)
        {
            byte v = (byte)Random.Range(min, max);
            pixels[i] = new Color32(v, (byte)(v * 0.96f), (byte)(v * 0.86f), 255);
        }

        tex.SetPixels32(pixels);
        tex.Apply(false);
        return tex;
    }

    private static Texture2D BuildVignetteTexture(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        float center = size * 0.5f;
        float radius = size * 0.52f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - center) / radius;
                float dy = (y - center) / radius;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.SmoothStep(0f, 0.62f, dist - 0.28f);
                tex.SetPixel(x, y, new Color(0f, 0f, 0f, alpha));
            }
        }

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
        if (_noiseFrames != null)
        {
            foreach (var frame in _noiseFrames)
            {
                if (frame != null)
                    Destroy(frame);
            }
        }

        if (_vignetteTexture != null)
            Destroy(_vignetteTexture);
    }
}
