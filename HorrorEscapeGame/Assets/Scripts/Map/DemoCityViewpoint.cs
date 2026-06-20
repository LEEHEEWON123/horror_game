using System.Collections;
using UnityEngine;

public static class DemoCityViewpoint
{
    public const float EyeHeight = 1.68f;

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
        cam.fieldOfView = 68f;
        cam.nearClipPlane = 0.08f;
        cam.backgroundColor = new Color(0.72f, 0.78f, 0.88f);

        if (cam.GetComponent<AudioListener>() == null)
            cam.gameObject.AddComponent<AudioListener>();

        foreach (var listener in Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude))
        {
            if (listener.gameObject != cam.gameObject)
                listener.enabled = false;
        }

        var fp = pivot.AddComponent<FirstPersonCamera>();
        fp.Configure(player, cam.transform, 1.5f);
        fp.SetPitchLimits(-8f, 22f);
    }

    public static IEnumerator StabilizeAfterPhysics(
        MonoBehaviour host,
        Transform player,
        Collider road,
        float streetBaseline)
    {
        yield return null;
        Physics.SyncTransforms();

        var pos = player.position;
        float y = DemoCityRoadFloor.QueryFootingY(pos, Vector3.zero, pos.y, road, streetBaseline);
        MapFloor.SetWalkY(y);

        pos.y = y;
        player.position = pos;

        var rb = player.GetComponent<Rigidbody>();
        if (rb != null)
            rb.position = pos;

        Debug.Log($"[DemoCity] streetY={y:F2} cameraY={y + EyeHeight:F2}");
    }
}
