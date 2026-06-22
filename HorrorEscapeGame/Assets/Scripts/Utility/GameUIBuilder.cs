using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public static class GameUIBuilder
{
    public static Canvas CreateScreenCanvas(string name, Camera worldCamera = null)
    {
        var canvasGo = new GameObject(name);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = worldCamera != null ? RenderMode.ScreenSpaceCamera : RenderMode.ScreenSpaceOverlay;
        canvas.worldCamera = worldCamera;
        canvas.planeDistance = 1f;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGo.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1080, 1920);
        canvasGo.AddComponent<GraphicRaycaster>();
        if (EventSystem.current == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }
        return canvas;
    }

    public static Image CreateFadeOverlay(Transform parent)
    {
        var go = CreateUIObject("FadePanel", parent);
        var img = go.AddComponent<Image>();
        Stretch(go.GetComponent<RectTransform>());
        img.color = new Color(0, 0, 0, 0);
        img.raycastTarget = false;
        return img;
    }

    public static (VirtualJoystick joystick, RectTransform bg, RectTransform handle) CreateJoystick(Transform parent)
    {
        var area = CreateUIObject("JoystickArea", parent);
        var areaRt = area.GetComponent<RectTransform>();
        areaRt.anchorMin = Vector2.zero;
        areaRt.anchorMax = new Vector2(0.5f, 0.4f);
        areaRt.offsetMin = Vector2.zero;
        areaRt.offsetMax = Vector2.zero;
        var areaImg = area.AddComponent<Image>();
        areaImg.color = new Color(0, 0, 0, 0);
        areaImg.raycastTarget = true;

        var bg = CreateUIObject("JoystickBackground", area.transform);
        var bgRt = bg.GetComponent<RectTransform>();
        bgRt.sizeDelta = new Vector2(120, 120);
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(1, 1, 1, 0.2f);

        var handle = CreateUIObject("JoystickHandle", bg.transform);
        var handleRt = handle.GetComponent<RectTransform>();
        handleRt.sizeDelta = new Vector2(60, 60);
        var handleImg = handle.AddComponent<Image>();
        handleImg.color = new Color(1, 1, 1, 0.5f);

        var joystick = area.AddComponent<VirtualJoystick>();
        joystick.Configure(bgRt, handleRt);
        bg.SetActive(false);
        return (joystick, bgRt, handleRt);
    }

    public struct HudBuildResult
    {
        public HUDManager Hud;
        public MinimapController Minimap;
        public RectTransform PlayerIcon;
        public RectTransform KeyIcon;
        public RectTransform LockIcon;
        public RectTransform ExitIcon;
    }

    public static HudBuildResult CreateHUD(Transform parent, PlayerInteraction playerInteraction, bool showMinimap = false)
    {
        var hudRoot = CreateUIObject("HUD", parent);
        var hudRt = hudRoot.GetComponent<RectTransform>();
        Stretch(hudRt);

        var interactBtn = CreateUIObject("InteractionButton", hudRoot.transform);
        var interactRt = interactBtn.GetComponent<RectTransform>();
        interactRt.anchorMin = new Vector2(0.5f, 0);
        interactRt.anchorMax = new Vector2(0.5f, 0);
        interactRt.pivot = new Vector2(0.5f, 0);
        interactRt.anchoredPosition = new Vector2(0, 120);
        interactRt.sizeDelta = new Vector2(260, 70);
        var interactImg = interactBtn.AddComponent<Image>();
        interactImg.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);
        var btn = interactBtn.AddComponent<Button>();
        btn.onClick.AddListener(playerInteraction.TryInteract);

        var labelGo = CreateUIObject("Label", interactBtn.transform);
        Stretch(labelGo.GetComponent<RectTransform>());
        var label = labelGo.AddComponent<TextMeshProUGUI>();
        TmpFontHelper.ApplyDefaultFont(label);
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 28;
        label.color = Color.white;
        label.text = "상호작용";
        interactBtn.SetActive(false);

        var hud = hudRoot.AddComponent<HUDManager>();
        hud.Configure(interactBtn, label);

        if (!showMinimap)
        {
            return new HudBuildResult { Hud = hud };
        }

        var minimapRoot = CreateUIObject("Minimap", parent);
        var minimapRt = minimapRoot.GetComponent<RectTransform>();
        minimapRt.anchorMin = new Vector2(1, 1);
        minimapRt.anchorMax = new Vector2(1, 1);
        minimapRt.pivot = new Vector2(1, 1);
        minimapRt.anchoredPosition = new Vector2(-20, -20);
        minimapRt.sizeDelta = new Vector2(200, 200);
        var minimapBg = minimapRoot.AddComponent<Image>();
        minimapBg.color = new Color(0, 0, 0, 0.5f);

        RectTransform CreateIcon(string iconName, Color color)
        {
            var icon = CreateUIObject(iconName, minimapRoot.transform);
            var rt = icon.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(10, 10);
            icon.AddComponent<Image>().color = color;
            return rt;
        }

        var minimap = minimapRoot.AddComponent<MinimapController>();
        return new HudBuildResult
        {
            Hud = hud,
            Minimap = minimap,
            PlayerIcon = CreateIcon("PlayerIcon", Color.white),
            KeyIcon = CreateIcon("KeyIcon", Color.yellow),
            LockIcon = CreateIcon("LockIcon", Color.red),
            ExitIcon = CreateIcon("ExitIcon", Color.green)
        };
    }

    public static void EnsureCoreSystems()
    {
        if (GameManager.Instance == null)
        {
            var gm = new GameObject("GameManager");
            gm.AddComponent<GameManager>();
        }

        if (SceneTransitioner.Instance == null)
        {
            var stGo = new GameObject("SceneTransitioner");
            stGo.AddComponent<SceneTransitioner>();
        }
    }

    public static GameObject CreatePrimitive(string name, Vector3 pos, Vector3 scale, Color color, bool navigationStatic = true)
    {
        var go = GameObject.CreatePrimitive(scale.y <= 0.3f ? PrimitiveType.Cube : PrimitiveType.Cube);
        go.name = name;
        go.transform.position = pos;
        go.transform.localScale = scale;
        var renderer = go.GetComponent<Renderer>();
        if (renderer != null)
            renderer.material.color = color;
        if (navigationStatic)
            go.isStatic = true;
        return go;
    }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
