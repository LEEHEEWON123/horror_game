using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class DimensionWakeSequence : MonoBehaviour
{
    private const float LyingEyeHeight = 0.28f;
    private const float LyingPitch = 12f;
    private const float RevealDuration = 1.35f;
    private const float StandDuration = 1.05f;

    private CameraClearFlags _savedClearFlags;
    private Color _savedBackgroundColor;

    public IEnumerator Run(bool autoStand = false)
    {
        var controller = GetComponent<PlayerController>();
        var interaction = GetComponent<PlayerInteraction>();
        var fpCamera = GetComponentInChildren<FirstPersonCamera>();
        var rb = GetComponent<Rigidbody>();
        var cam = Camera.main;

        if (fpCamera == null)
        {
            if (SceneTransitioner.Instance != null)
                yield return SceneTransitioner.Instance.FadeInRoutine();
            yield break;
        }

        float standingEyeHeight = fpCamera.EyeHeight;
        DimensionWakeState.SetActive(true);
        MonsterAI.PauseAllForWake();
        SetGameplayEnabled(controller, interaction, fpCamera, false);
        if (rb != null)
            rb.linearVelocity = Vector3.zero;

        SetJoystickVisible(false);
        CaptureCamera(cam);
        ApplyWakeCamera(cam);
        fpCamera.EnterWakeMode();
        fpCamera.SetView(LyingPitch, LyingEyeHeight);

        TextMeshProUGUI hint = autoStand ? null : CreateHintLabel();
        if (hint != null)
            hint.gameObject.SetActive(false);

        if (SceneTransitioner.Instance != null)
        {
            SceneTransitioner.Instance.ShowStaticOverlay(1f);
            yield return SceneTransitioner.Instance.FadeInRoutine(RevealDuration);
            yield return SceneTransitioner.Instance.FadeOutStaticRoutine(0.75f, 0.42f);
        }

        yield return new WaitForSecondsRealtime(autoStand ? 0.45f : 0.55f);

        if (autoStand)
        {
            yield return new WaitForSecondsRealtime(0.95f);
        }
        else
        {
            if (hint != null)
            {
                hint.gameObject.SetActive(true);
                hint.text = "Tap to stand up...";
            }

            yield return WaitForStandInput();
        }

        if (hint != null)
            hint.text = string.Empty;

        if (SceneTransitioner.Instance != null)
            yield return SceneTransitioner.Instance.FadeOutStaticRoutine(0.35f, 0f);

        float elapsed = 0f;
        while (elapsed < StandDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / StandDuration);
            fpCamera.SetView(Mathf.Lerp(LyingPitch, 0f, t), Mathf.Lerp(LyingEyeHeight, standingEyeHeight, t));
            yield return null;
        }

        fpCamera.SetView(0f, standingEyeHeight);
        fpCamera.ExitWakeMode();
        RestoreCamera(cam);
        DimensionWakeState.SetActive(false);
        MonsterAI.ResumeAllAfterWake();
        SetGameplayEnabled(controller, interaction, fpCamera, true);
        SetJoystickVisible(true);

        if (hint != null)
            Destroy(hint.transform.parent.gameObject);
    }

    private void CaptureCamera(Camera cam)
    {
        if (cam == null) return;
        _savedClearFlags = cam.clearFlags;
        _savedBackgroundColor = cam.backgroundColor;
    }

    private static void ApplyWakeCamera(Camera cam)
    {
        if (cam == null) return;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.02f, 0.022f, 0.028f);
    }

    private void RestoreCamera(Camera cam)
    {
        if (cam == null) return;
        cam.clearFlags = _savedClearFlags;
        cam.backgroundColor = _savedBackgroundColor;
    }

    private static void SetGameplayEnabled(
        PlayerController controller,
        PlayerInteraction interaction,
        FirstPersonCamera fpCamera,
        bool enabled)
    {
        if (controller != null) controller.enabled = enabled;
        if (interaction != null) interaction.enabled = enabled;
        if (fpCamera != null) fpCamera.enabled = enabled;
    }

    private static IEnumerator WaitForStandInput()
    {
        while (true)
        {
            if (WasStandInputPressed())
                yield break;

            yield return null;
        }
    }

    private static bool WasStandInputPressed()
    {
        if (Touchscreen.current != null)
        {
            foreach (var touch in Touchscreen.current.touches)
            {
                if (touch.press.wasPressedThisFrame)
                    return true;
            }
        }

        var mouse = Mouse.current;
        if (mouse != null && (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame))
            return true;

        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.anyKey.wasPressedThisFrame)
            return true;

        return false;
    }

    private static void SetJoystickVisible(bool visible)
    {
        var joystick = Object.FindAnyObjectByType<VirtualJoystick>();
        if (joystick == null) return;

        var area = joystick.transform.parent;
        if (area != null)
            area.gameObject.SetActive(visible);
    }

    private static TextMeshProUGUI CreateHintLabel()
    {
        var canvasGo = new GameObject("WakeHintCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9000;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        canvasGo.AddComponent<GraphicRaycaster>();

        var labelGo = new GameObject("WakeHint", typeof(RectTransform));
        labelGo.transform.SetParent(canvasGo.transform, false);

        var rt = labelGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 120f);
        rt.sizeDelta = new Vector2(900f, 80f);

        var bg = labelGo.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.35f);
        bg.raycastTarget = false;

        var textGo = new GameObject("Text", typeof(RectTransform));
        textGo.transform.SetParent(labelGo.transform, false);
        var textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        TmpFontHelper.ApplyDefaultFont(tmp);
        tmp.fontSize = 30;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.92f, 0.94f, 0.96f);
        tmp.raycastTarget = false;
        return tmp;
    }
}
