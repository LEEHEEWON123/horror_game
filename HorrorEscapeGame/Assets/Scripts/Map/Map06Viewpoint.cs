using UnityEngine;

public static class Map06Viewpoint
{
    public const float EyeHeight = 1.65f;

    public static void SetupCamera(Transform player, Camera cam)
    {
        if (cam == null) return;

        var pivot = new GameObject("CameraPivot");
        pivot.transform.SetParent(player, false);
        pivot.transform.localPosition = new Vector3(0f, EyeHeight, 0f);

        cam.transform.SetParent(pivot.transform, false);
        cam.transform.localPosition = Vector3.zero;
        cam.transform.localRotation = Quaternion.identity;

        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.fieldOfView = 66f;
        cam.nearClipPlane = 0.08f;
        cam.backgroundColor = new Color(0.48f, 0.5f, 0.53f);

        if (cam.GetComponent<AudioListener>() == null)
            cam.gameObject.AddComponent<AudioListener>();

        foreach (var listener in Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude))
        {
            if (listener.gameObject != cam.gameObject)
                listener.enabled = false;
        }

        player.rotation = Quaternion.Euler(0f, player.eulerAngles.y, 0f);

        var fp = pivot.AddComponent<FirstPersonCamera>();
        fp.Configure(player, cam.transform, FirstPersonCamera.DefaultMouseSensitivity);
    }

    public static void ConfigureCatchView(PlayerCatchSequence sequence, float entityScale)
    {
        if (sequence == null) return;

        sequence.Configure(
            faceDistance: 0.55f * entityScale,
            targetFov: 58f,
            lookHeightOffset: 0.08f * entityScale,
            cameraHeightOffset: -0.03f * entityScale,
            entityScale: entityScale,
            nearClip: 0.02f);
    }
}
