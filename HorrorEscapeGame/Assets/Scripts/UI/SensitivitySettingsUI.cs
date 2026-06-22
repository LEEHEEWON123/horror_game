using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Top-right settings button → sensitivity dialog. Pauses game while open.
/// </summary>
public class SensitivitySettingsUI : MonoBehaviour
{
    public static bool IsOpen { get; private set; }

    private GameObject _dialogRoot;
    private RectTransform _settingsButtonRt;
    private Slider _slider;
    private TextMeshProUGUI _valueLabel;
    private FirstPersonCamera _fpCam;
    private PlayerController _playerController;
    private float _savedTimeScale = 1f;
    private bool _open;

    public static SensitivitySettingsUI Create(Transform canvasParent, FirstPersonCamera fpCam)
    {
        var go = new GameObject("SettingsUI", typeof(RectTransform));
        go.transform.SetParent(canvasParent, false);
        Stretch(go.GetComponent<RectTransform>());
        var ui = go.AddComponent<SensitivitySettingsUI>();
        ui._fpCam = fpCam;
        ui._playerController = fpCam != null
            ? fpCam.GetComponentInParent<PlayerController>()
            : null;
        ui.Build();
        ui.transform.SetAsLastSibling();
        return ui;
    }

    private void Build()
    {
        BuildSettingsButton();
        BuildDialog();
    }

    private void BuildSettingsButton()
    {
        var btnGo = new GameObject("SettingsButton", typeof(RectTransform));
        btnGo.transform.SetParent(transform, false);
        var rt = btnGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-20f, -20f);
        rt.sizeDelta = new Vector2(100f, 44f);
        _settingsButtonRt = rt;

        var img = btnGo.AddComponent<Image>();
        img.color = new Color(0.08f, 0.08f, 0.1f, 0.85f);

        var btn = btnGo.AddComponent<Button>();
        btn.onClick.AddListener(OpenSettings);

        CreateText(btnGo.transform, "Settings", 16f, Vector2.zero, new Vector2(100f, 44f),
            TextAlignmentOptions.Center);
    }

    private void BuildDialog()
    {
        _dialogRoot = new GameObject("SettingsDialog", typeof(RectTransform));
        _dialogRoot.transform.SetParent(transform, false);
        var rootRt = _dialogRoot.GetComponent<RectTransform>();
        Stretch(rootRt);

        var dim = _dialogRoot.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.65f);
        dim.raycastTarget = true;

        var panel = new GameObject("Panel", typeof(RectTransform));
        panel.transform.SetParent(_dialogRoot.transform, false);
        var panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(380f, 240f);

        var panelBg = panel.AddComponent<Image>();
        panelBg.color = new Color(0.1f, 0.1f, 0.12f, 0.96f);

        CreateText(panel.transform, "Settings", 22f, new Vector2(0f, 88f), new Vector2(340f, 36f),
            TextAlignmentOptions.Center);
        CreateText(panel.transform, "Mouse Sensitivity", 15f, new Vector2(0f, 52f), new Vector2(340f, 28f),
            TextAlignmentOptions.Center, new Color(0.75f, 0.75f, 0.75f, 1f));
        CreateText(panel.transform, "Adjust Sensitivity", 17f, new Vector2(0f, 18f), new Vector2(340f, 30f),
            TextAlignmentOptions.Center);

        var sliderGo = new GameObject("Slider", typeof(RectTransform));
        sliderGo.transform.SetParent(panel.transform, false);
        var sliderRt = sliderGo.GetComponent<RectTransform>();
        sliderRt.anchorMin = new Vector2(0.5f, 0.5f);
        sliderRt.anchorMax = new Vector2(0.5f, 0.5f);
        sliderRt.sizeDelta = new Vector2(280f, 24f);
        sliderRt.anchoredPosition = new Vector2(-24f, -24f);

        _slider = sliderGo.AddComponent<Slider>();
        _slider.minValue = MouseSensitivityPrefs.Min;
        _slider.maxValue = MouseSensitivityPrefs.Max;
        _slider.value = MouseSensitivityPrefs.Load();
        BuildSliderVisuals(sliderGo.transform, _slider);

        var valueGo = new GameObject("ValueLabel", typeof(RectTransform));
        valueGo.transform.SetParent(panel.transform, false);
        var valueRt = valueGo.GetComponent<RectTransform>();
        valueRt.anchorMin = new Vector2(0.5f, 0.5f);
        valueRt.anchorMax = new Vector2(0.5f, 0.5f);
        valueRt.anchoredPosition = new Vector2(148f, -24f);
        valueRt.sizeDelta = new Vector2(56f, 28f);
        _valueLabel = valueGo.AddComponent<TextMeshProUGUI>();
        TmpFontHelper.ApplyDefaultFont(_valueLabel);
        _valueLabel.fontSize = 16f;
        _valueLabel.alignment = TextAlignmentOptions.MidlineLeft;
        _valueLabel.color = Color.white;

        _slider.onValueChanged.AddListener(OnSliderChanged);
        UpdateValueLabel(_slider.value);

        var closeBtnGo = new GameObject("CloseButton", typeof(RectTransform));
        closeBtnGo.transform.SetParent(panel.transform, false);
        var closeRt = closeBtnGo.GetComponent<RectTransform>();
        closeRt.anchorMin = new Vector2(0.5f, 0.5f);
        closeRt.anchorMax = new Vector2(0.5f, 0.5f);
        closeRt.anchoredPosition = new Vector2(0f, -78f);
        closeRt.sizeDelta = new Vector2(140f, 44f);

        var closeImg = closeBtnGo.AddComponent<Image>();
        closeImg.color = new Color(0.22f, 0.22f, 0.26f, 1f);
        var closeBtn = closeBtnGo.AddComponent<Button>();
        closeBtn.onClick.AddListener(CloseSettings);
        CreateText(closeBtnGo.transform, "Close", 16f, Vector2.zero, new Vector2(140f, 44f),
            TextAlignmentOptions.Center);

        _dialogRoot.SetActive(false);
    }

    private static void BuildSliderVisuals(Transform sliderGo, Slider slider)
    {
        var bgBar = new GameObject("Background", typeof(RectTransform));
        bgBar.transform.SetParent(sliderGo, false);
        var bgBarRt = bgBar.GetComponent<RectTransform>();
        bgBarRt.anchorMin = Vector2.zero;
        bgBarRt.anchorMax = Vector2.one;
        bgBarRt.sizeDelta = Vector2.zero;
        var bgBarImg = bgBar.AddComponent<Image>();
        bgBarImg.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        slider.targetGraphic = bgBarImg;

        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderGo, false);
        var fillAreaRt = fillArea.GetComponent<RectTransform>();
        fillAreaRt.anchorMin = new Vector2(0f, 0.25f);
        fillAreaRt.anchorMax = new Vector2(1f, 0.75f);
        fillAreaRt.offsetMin = new Vector2(5f, 0f);
        fillAreaRt.offsetMax = new Vector2(-15f, 0f);

        var fill = new GameObject("Fill", typeof(RectTransform));
        fill.transform.SetParent(fillArea.transform, false);
        var fillRt = fill.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.sizeDelta = Vector2.zero;
        var fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(0.8f, 0.8f, 0.8f, 1f);
        slider.fillRect = fillRt;

        var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(sliderGo, false);
        var handleAreaRt = handleArea.GetComponent<RectTransform>();
        handleAreaRt.anchorMin = Vector2.zero;
        handleAreaRt.anchorMax = Vector2.one;
        handleAreaRt.offsetMin = new Vector2(10f, 0f);
        handleAreaRt.offsetMax = new Vector2(-10f, 0f);

        var handle = new GameObject("Handle", typeof(RectTransform));
        handle.transform.SetParent(handleArea.transform, false);
        var handleRt = handle.GetComponent<RectTransform>();
        handleRt.sizeDelta = new Vector2(20f, 20f);
        var handleImg = handle.AddComponent<Image>();
        handleImg.color = Color.white;
        slider.handleRect = handleRt;
    }

    private void Update()
    {
        TryOpenFromPointer();

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            if (_open)
                CloseSettings();
            else
                OpenSettings();
        }
    }

    private void TryOpenFromPointer()
    {
        if (_open || _settingsButtonRt == null) return;

        var mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;

        var cam = _settingsButtonRt.GetComponentInParent<Canvas>()?.worldCamera;
        if (!RectTransformUtility.RectangleContainsScreenPoint(_settingsButtonRt, mouse.position.ReadValue(), cam))
            return;

        OpenSettings();
    }

    private void OnDestroy()
    {
        if (_open)
            ResumeGame();
    }

    public void OpenSettings()
    {
        if (_open) return;

        _open = true;
        IsOpen = true;
        _savedTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        if (_playerController != null)
            _playerController.enabled = false;
        if (_fpCam != null)
            _fpCam.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        _dialogRoot.transform.SetAsLastSibling();
        _settingsButtonRt.transform.SetAsLastSibling();
        _dialogRoot.SetActive(true);
    }

    public void CloseSettings()
    {
        if (!_open) return;

        _dialogRoot.SetActive(false);
        _open = false;
        IsOpen = false;
        ResumeGame();
    }

    private void ResumeGame()
    {
        Time.timeScale = _savedTimeScale;

        if (_playerController != null)
            _playerController.enabled = true;
        if (_fpCam != null)
            _fpCam.enabled = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnSliderChanged(float value)
    {
        MouseSensitivityPrefs.Save(value);
        ApplySensitivity(value);
        UpdateValueLabel(value);
    }

    private void ApplySensitivity(float value)
    {
        if (_fpCam != null)
            _fpCam.SetSensitivity(value);

        foreach (var cam in FindObjectsByType<FirstPersonCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            cam.SetSensitivity(value);
    }

    private void UpdateValueLabel(float value)
    {
        if (_valueLabel != null)
            _valueLabel.text = value.ToString("F2");
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void CreateText(Transform parent, string text, float size, Vector2 pos, Vector2 sizeDelta,
        TextAlignmentOptions alignment, Color? color = null)
    {
        var go = new GameObject(text, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = sizeDelta;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        TmpFontHelper.ApplyDefaultFont(tmp);
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = alignment;
        tmp.color = color ?? Color.white;
        tmp.raycastTarget = false;
    }
}
