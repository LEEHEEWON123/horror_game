using UnityEngine;

public static class Map03Viewpoint
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

        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.fieldOfView = 76f;
        cam.nearClipPlane = 0.08f;
        cam.backgroundColor = new Color(0.01f, 0.012f, 0.016f);

        if (cam.GetComponent<AudioListener>() == null)
            cam.gameObject.AddComponent<AudioListener>();

        foreach (var listener in Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
        {
            if (listener.gameObject != cam.gameObject)
                listener.enabled = false;
        }

        var fp = pivot.AddComponent<FirstPersonCamera>();
        fp.Configure(player, cam.transform, 1.5f);
        fp.SetPitchLimits(-18f, 22f);
    }
}
