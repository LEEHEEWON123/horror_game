using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ReturnMapNotice
{
    public static void TryShowForScene(string sceneName)
    {
        if (!DimensionMapPool.IsRandomMap(sceneName)) return;

        var gm = GameManager.Instance;
        if (gm == null || !gm.IsReturnMap(sceneName)) return;

        if (Object.FindAnyObjectByType<ReturnMapNoticeRunner>() != null)
            return;

        new GameObject("ReturnMapNoticeRunner").AddComponent<ReturnMapNoticeRunner>();
    }
}

public class ReturnMapNoticeRunner : MonoBehaviour
{
    private const float HoldDuration = 4.5f;
    private const float FadeDuration = 0.65f;

    private IEnumerator Start()
    {
        yield return null;

        while (DimensionWakeState.IsActive)
            yield return null;

        yield return new WaitForSecondsRealtime(0.6f);

        var label = CreateBanner();
        if (label == null)
        {
            Destroy(gameObject);
            yield break;
        }

        var canvasGroup = label.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = label.gameObject.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;
        yield return Fade(canvasGroup, 0f, 1f, FadeDuration);
        yield return new WaitForSecondsRealtime(HoldDuration);
        yield return Fade(canvasGroup, 1f, 0f, FadeDuration);

        if (label != null)
            Destroy(label.transform.parent.gameObject);

        Destroy(gameObject);
    }

    private static TextMeshProUGUI CreateBanner()
    {
        var canvasGo = new GameObject("ReturnMapNoticeCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 8500;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGo.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1080, 1920);

        var panelGo = new GameObject("Banner", typeof(RectTransform));
        panelGo.transform.SetParent(canvasGo.transform, false);
        var rt = panelGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -48f);
        rt.sizeDelta = new Vector2(920f, 120f);

        var bg = panelGo.AddComponent<Image>();
        bg.color = new Color(0.04f, 0.05f, 0.06f, 0.82f);
        bg.raycastTarget = false;

        var textGo = new GameObject("Text", typeof(RectTransform));
        textGo.transform.SetParent(panelGo.transform, false);
        var textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(24f, 12f);
        textRt.offsetMax = new Vector2(-24f, -12f);

        var label = textGo.AddComponent<TextMeshProUGUI>();
        TmpFontHelper.ApplyDefaultFont(label);
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 30;
        label.color = new Color(0.92f, 0.94f, 0.88f, 1f);
        label.text = "An exit has opened in this area.\nSearch every corner.";

        return label;
    }

    private static IEnumerator Fade(CanvasGroup group, float from, float to, float duration)
    {
        if (group == null) yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
            group.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }

        group.alpha = to;
    }
}
