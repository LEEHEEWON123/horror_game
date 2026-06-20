using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainMenuBootstrap : MonoBehaviour
{
    private void Awake()
    {
        GameUIBuilder.EnsureCoreSystems();
        GameManager.Instance?.ResetRun();

        var canvas = GameUIBuilder.CreateScreenCanvas("MainMenuCanvas");
        var bg = new GameObject("Background", typeof(RectTransform));
        bg.transform.SetParent(canvas.transform, false);
        var bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        bg.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.08f, 1f);

        CreateLabel(canvas.transform, "HORROR ESCAPE", 72, new Vector2(0, 300), Color.white);
        var menu = canvas.gameObject.AddComponent<MainMenuUI>();
        CreateButton(canvas.transform, "GAME START", new Vector2(0, 0), () => menu.OnStartButton());
        CreateButton(canvas.transform, "QUIT", new Vector2(0, -120), () => menu.OnQuitButton());
    }

    private static void CreateLabel(Transform parent, string text, float size, Vector2 pos, Color color)
    {
        var go = new GameObject("Title", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(800, 120);
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
        rt.sizeDelta = new Vector2(320, 80);
        go.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.25f, 1f);
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
        tmp.fontSize = 32;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
    }
}
