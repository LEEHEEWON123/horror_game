using UnityEngine;

/// <summary>
/// Ensures mouse-sensitivity settings UI exists on any map with a first-person player.
/// Called from HorrorSceneBootstrap after map bootstraps finish spawning the player.
/// </summary>
public static class PlayerSettingsAttach
{
    public static void TryAttach()
    {
        if (Object.FindAnyObjectByType<SensitivitySettingsUI>() != null)
            return;

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        var fp = player.GetComponentInChildren<FirstPersonCamera>();
        if (fp == null) return;

        var canvas = FindGameCanvas();
        if (canvas == null)
        {
            Debug.LogWarning("[PlayerSettingsAttach] GameCanvas not found — settings UI skipped.");
            return;
        }

        fp.SetSensitivity(MouseSensitivityPrefs.Load());
        SensitivitySettingsUI.Create(canvas.transform, fp);
    }

    private static Canvas FindGameCanvas()
    {
        Canvas fallback = null;

        foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (canvas == null) continue;

            if (canvas.name == "GameCanvas")
                return canvas;

            if (fallback == null
                && canvas.renderMode != RenderMode.WorldSpace
                && canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>() != null
                && canvas.sortingOrder < 8000)
            {
                fallback = canvas;
            }
        }

        return fallback;
    }
}
