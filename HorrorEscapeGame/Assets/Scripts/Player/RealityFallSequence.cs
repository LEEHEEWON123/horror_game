using System.Collections;
using UnityEngine;

public class RealityFallSequence : MonoBehaviour
{
    private const float FallDuration = 4.2f;
    private const float StartHeightOffset = 140f;
    private const float EndHeightOffset = 1.65f;
    private static readonly Color FallBackground = new Color(0.72f, 0.78f, 0.88f);

    public IEnumerator Run(Transform player, GameObject endingPanel)
    {
        if (player == null)
        {
            if (SceneTransitioner.Instance != null)
                yield return SceneTransitioner.Instance.FadeInRoutine(1.2f);
            yield break;
        }

        var controller = player.GetComponent<PlayerController>();
        var interaction = player.GetComponent<PlayerInteraction>();
        var fpCamera = player.GetComponentInChildren<FirstPersonCamera>();
        var playerCam = Camera.main;

        SetGameplayEnabled(controller, interaction, fpCamera, false);
        SetGameplayUiVisible(false);

        for (int i = 0; i < 12; i++)
            yield return null;

        Physics.SyncTransforms();

        Vector3 landing = player.position;
        float streetY = landing.y;
        Vector3 lookTarget = landing + Vector3.up * 0.2f;

        bool fogWasEnabled = RenderSettings.fog;
        float prevFogStart = RenderSettings.fogStartDistance;
        float prevFogEnd = RenderSettings.fogEndDistance;
        Color prevFogColor = RenderSettings.fogColor;

        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.62f, 0.68f, 0.74f);
        RenderSettings.fogStartDistance = 28f;
        RenderSettings.fogEndDistance = 48f;

        var fallCamGo = new GameObject("RealityFallCamera");
        fallCamGo.tag = "MainCamera";
        var fallCam = fallCamGo.AddComponent<Camera>();
        fallCam.clearFlags = RenderSettings.skybox != null
            ? CameraClearFlags.Skybox
            : CameraClearFlags.SolidColor;
        fallCam.backgroundColor = FallBackground;
        fallCam.fieldOfView = 72f;
        fallCam.nearClipPlane = 0.2f;
        fallCam.farClipPlane = 600f;

        float startHeight = streetY + StartHeightOffset;
        var startPos = new Vector3(landing.x, startHeight, landing.z);
        fallCamGo.transform.position = startPos;
        fallCamGo.transform.rotation = BuildFallRotation(startPos, lookTarget, shake: 0f);

        if (playerCam != null)
        {
            playerCam.enabled = false;
            playerCam.tag = "Untagged";
            var listener = playerCam.GetComponent<AudioListener>();
            if (listener != null)
                listener.enabled = false;
        }

        fallCamGo.AddComponent<AudioListener>();
        BindUiCamera(fallCam);

        var endingGroup = PrepareEndingPanel(endingPanel);

        VhsFallOverlay overlay = null;
        Transform overlayParent = SceneTransitioner.Instance != null
            ? SceneTransitioner.Instance.OverlayRoot
            : null;
        if (overlayParent != null)
        {
            overlay = VhsFallOverlay.Create(overlayParent);
            overlay.SetVisible(true);
            overlay.SetIntensity(0.55f);
        }

        if (SceneTransitioner.Instance != null)
            SceneTransitioner.Instance.RevealScene();

        float elapsed = 0f;
        while (elapsed < FallDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / FallDuration);
            float fallT = t * t * (3f - 2f * t);

            float height = Mathf.Lerp(startHeight, streetY + EndHeightOffset, fallT);
            float sway = Mathf.Lerp(0.08f, 0.45f, fallT);
            var pos = new Vector3(
                landing.x + Mathf.Sin(elapsed * 1.15f) * sway,
                height,
                landing.z + Mathf.Cos(elapsed * 0.95f) * sway);

            fallCamGo.transform.position = pos;
            fallCamGo.transform.rotation = BuildFallRotation(
                pos,
                lookTarget,
                shake: Mathf.Lerp(0.15f, 1.1f, fallT));

            fallCam.fieldOfView = Mathf.Lerp(68f, 76f, fallT);
            RenderSettings.fogStartDistance = Mathf.Lerp(28f, 16f, fallT);
            RenderSettings.fogEndDistance = Mathf.Lerp(48f, 88f, fallT);

            if (overlay != null)
                overlay.SetIntensity(Mathf.Lerp(0.5f, 0.04f, fallT));

            if (endingGroup != null)
            {
                float uiT = Mathf.InverseLerp(0.38f, 0.96f, fallT);
                endingGroup.alpha = Mathf.SmoothStep(0f, 1f, uiT);
            }

            yield return null;
        }

        if (SceneTransitioner.Instance != null)
            yield return SceneTransitioner.Instance.FlashWhiteRoutine(0.12f);

        if (overlay != null)
        {
            overlay.SetVisible(false);
            Destroy(overlay.gameObject);
        }

        Destroy(fallCamGo);

        if (playerCam != null)
        {
            playerCam.tag = "MainCamera";
            playerCam.enabled = true;
            BindUiCamera(playerCam);
            var listener = playerCam.GetComponent<AudioListener>();
            if (listener != null)
                listener.enabled = true;
        }

        RenderSettings.fog = fogWasEnabled;
        RenderSettings.fogStartDistance = prevFogStart;
        RenderSettings.fogEndDistance = prevFogEnd;
        RenderSettings.fogColor = prevFogColor;

        if (SceneTransitioner.Instance != null)
            yield return SceneTransitioner.Instance.FadeInRoutine(0.55f);

        if (endingGroup != null)
        {
            endingGroup.alpha = 1f;
            endingGroup.interactable = true;
            endingGroup.blocksRaycasts = true;
        }

        if (endingPanel != null)
            endingPanel.SetActive(true);

        SetGameplayUiVisible(true);
        SetGameplayEnabled(controller, interaction, fpCamera, false);
    }

    private static CanvasGroup PrepareEndingPanel(GameObject endingPanel)
    {
        if (endingPanel == null) return null;

        endingPanel.SetActive(true);
        var group = endingPanel.GetComponent<CanvasGroup>();
        if (group == null)
            group = endingPanel.AddComponent<CanvasGroup>();

        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
        return group;
    }

    private static void BindUiCamera(Camera cam)
    {
        var canvas = GameObject.Find("GameCanvas")?.GetComponent<Canvas>();
        if (canvas == null || cam == null) return;

        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.planeDistance = 1f;
    }

    private static void SetGameplayUiVisible(bool visible)
    {
        var canvas = GameObject.Find("GameCanvas");
        if (canvas == null) return;

        foreach (Transform child in canvas.transform)
        {
            if (child.name == "EndingPanel") continue;
            child.gameObject.SetActive(visible);
        }
    }

    private static Quaternion BuildFallRotation(Vector3 from, Vector3 target, float shake)
    {
        var dir = target - from;
        if (dir.sqrMagnitude < 0.01f)
            dir = Vector3.down;

        var look = Quaternion.LookRotation(dir.normalized, Vector3.up);
        return look * Quaternion.Euler(
            Random.Range(-shake, shake),
            Random.Range(-shake, shake),
            Random.Range(-shake * 0.6f, shake * 0.6f));
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
}
