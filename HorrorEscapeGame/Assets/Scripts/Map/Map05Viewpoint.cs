using UnityEngine;

public static class Map05Viewpoint
{
    public const float EyeHeight = 1.65f;

    public static void SetupCamera(Transform player, Camera cam)
    {
        if (cam == null) return;

        var pivot = new GameObject("CameraPivot");
        pivot.transform.SetParent(player, false);
        pivot.transform.localPosition = new Vector3(0f, EyeHeight, 0f);
        pivot.transform.localRotation = Quaternion.identity;

        cam.transform.SetParent(pivot.transform, false);
        cam.transform.localPosition = Vector3.zero;
        cam.transform.localRotation = Quaternion.identity;

        player.rotation = Quaternion.Euler(0f, player.eulerAngles.y, 0f);

        cam.clearFlags = CameraClearFlags.Skybox;
        cam.fieldOfView = 78f;
        cam.nearClipPlane = 0.08f;
        cam.backgroundColor = new Color(0.1f, 0.12f, 0.16f);

        if (cam.GetComponent<AudioListener>() == null)
            cam.gameObject.AddComponent<AudioListener>();

        foreach (var listener in Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude))
        {
            if (listener.gameObject != cam.gameObject)
                listener.enabled = false;
        }

        var fp = pivot.AddComponent<FirstPersonCamera>();
        fp.Configure(player, cam.transform, FirstPersonCamera.DefaultMouseSensitivity);
    }

    public static void ConfigureCatchView(PlayerCatchSequence sequence)
    {
        if (sequence == null) return;

        float scale = PriestVisualSetup.VisualScale;
        sequence.Configure(
            faceDistance: 0.45f * scale,
            targetFov: 56f,
            lookHeightOffset: 0.06f * scale,
            cameraHeightOffset: -0.04f * scale,
            entityScale: scale,
            nearClip: 0.02f);
    }
}
