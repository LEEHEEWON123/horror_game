using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MapRealityBootstrap : MonoBehaviour
{
    [SerializeField] private GameObject cityPrefab;

    private void Awake()
    {
        ResolveAssets();
        GameUIBuilder.EnsureCoreSystems();

        var map = DemoCityMapBuilder.Build(cityPrefab);
        if (map.Root == null) return;

        DemoCityAtmosphere.Apply(map.PlayBounds, DemoCityAtmosphere.Mood.RealityReturn);

        var layout = DemoCityLayout.ForCity(map.RoadCollider, map.StreetBaselineY);
        MapFloor.SetWalkY(map.StreetBaselineY);

        var player = RealityCityPlayerSetup.Spawn(
            layout.PlayerSpawn,
            map.PlayBounds,
            map.RoadCollider,
            map.StreetBaselineY);

        var interaction = player.GetComponent<PlayerInteraction>();

        if (RealityEscapeState.HasPending())
        {
            var controller = player.GetComponent<PlayerController>();
            if (controller != null) controller.enabled = false;
            if (interaction != null) interaction.enabled = false;
            var fp = player.GetComponentInChildren<FirstPersonCamera>();
            if (fp != null) fp.enabled = false;
        }

        Vector3 lookDir = layout.FallTriggerCenter - layout.PlayerSpawn;
        lookDir.y = 0f;
        if (lookDir.sqrMagnitude > 0.01f)
            player.transform.rotation = Quaternion.LookRotation(lookDir.normalized);

        RealityCityPlayerSetup.SetupCityCamera(player.transform, Camera.main);

        var canvas = GameUIBuilder.CreateScreenCanvas("GameCanvas", Camera.main);
        RealityCityPlayerSetup.CreateWalkUi(canvas.transform, interaction, hintText: null);
        VillageAnalogOverlay.Attach();

        CreateEndingOverlay(canvas.transform, deferVisible: RealityEscapeState.HasPending());
        StartCoroutine(DemoCityViewpoint.StabilizeAfterPhysics(
            this, player.transform, map.RoadCollider, map.StreetBaselineY));
    }

    private void ResolveAssets()
    {
#if UNITY_EDITOR
        if (cityPrefab == null)
            cityPrefab = DemoCityMapBuilder.LoadDefaultPrefab();
#endif
    }

    private static void CreateEndingOverlay(Transform canvasRoot, bool deferVisible = false)
    {
        var panel = new GameObject("EndingPanel", typeof(RectTransform));
        panel.transform.SetParent(canvasRoot, false);
        var panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 1f);
        panelRt.anchorMax = new Vector2(0.5f, 1f);
        panelRt.pivot = new Vector2(0.5f, 1f);
        panelRt.anchoredPosition = new Vector2(0f, -48f);
        panelRt.sizeDelta = new Vector2(920f, 220f);

        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.45f);

        CreateLabel(panel.transform, "Back in the real world", 52, new Vector2(0f, 36f), Color.white);
        CreateLabel(panel.transform, "You escaped the dimensions and returned to the city.", 26, new Vector2(0f, -24f),
            new Color(0.88f, 0.9f, 0.94f));

        var ui = panel.AddComponent<RealityEndingUI>();
        CreateButton(panel.transform, "Main Menu", new Vector2(0f, -92f), () => ui.OnMainMenuButton());

        panel.AddComponent<CanvasGroup>();

        if (deferVisible)
            panel.SetActive(false);
    }

    private static void CreateLabel(Transform parent, string text, float size, Vector2 pos, Color color)
    {
        var go = new GameObject("Label", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(880f, 80f);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        TmpFontHelper.ApplyDefaultFont(tmp);
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
    }

    private static void CreateButton(Transform parent, string label, Vector2 pos, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(label, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(320f, 72f);

        go.AddComponent<Image>().color = new Color(0.18f, 0.2f, 0.24f, 0.95f);
        var btn = go.AddComponent<Button>();
        btn.onClick.AddListener(onClick);

        var textGo = new GameObject("Text", typeof(RectTransform));
        textGo.transform.SetParent(go.transform, false);
        var textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        TmpFontHelper.ApplyDefaultFont(tmp);
        tmp.text = label;
        tmp.fontSize = 28;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
    }
}
