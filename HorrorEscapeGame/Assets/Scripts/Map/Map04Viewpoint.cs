using System.Collections;
using UnityEngine;

/// <summary>Map_04 only — resolves walk height and keeps the first-person camera at eye level.</summary>
public static class Map04Viewpoint
{
    public const float EyeHeight = 2.1f;

    public static float ResolveGroundY(Bounds bounds, GameObject mapRoot)
    {
        float minY = bounds.min.y + 0.05f;
        float groundY = SpawnHelper.QueryPrototypeGroundY(bounds, minY);

        if (groundY <= minY)
            groundY = EstimateFromRenderers(mapRoot, bounds, minY);

        return groundY;
    }

    public static float ResolveSpawnY(Vector3 spawnXZ, Bounds bounds, float groundY)
    {
        float minY = bounds.min.y + 0.05f;
        return SpawnHelper.QueryBoundedFloorY(spawnXZ, bounds, groundY, minY);
    }

    public static void ApplyFloor(Transform player, float floorY)
    {
        MapFloor.SetWalkY(floorY);

        var pos = player.position;
        pos.y = floorY;
        player.position = pos;

        var rb = player.GetComponent<Rigidbody>();
        if (rb != null)
            rb.position = pos;
    }

    public static void ConfigureCatchView(PlayerCatchSequence sequence)
    {
        if (sequence == null) return;

        float scale = PumpkinVisualSetup.VisualScale;
        sequence.Configure(
            faceDistance: 0.42f * scale,
            targetFov: 58f,
            lookHeightOffset: 0.06f * scale,
            cameraHeightOffset: -0.05f * scale,
            entityScale: scale,
            nearClip: 0.02f);
    }

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
    }

    public static IEnumerator StabilizeAfterPhysics(MonoBehaviour host, Transform player, Bounds bounds)
    {
        yield return null;
        Physics.SyncTransforms();

        var pos = player.position;
        if (SpawnHelper.TryQueryFootingY(pos, Vector3.zero, pos.y, MapFloor.WalkY, out float floorY))
            ApplyFloor(player, floorY);

        float cameraY = player.position.y + EyeHeight;
        Debug.Log(
            $"[Map04] floor={player.position.y:F2} cameraY={cameraY:F2} boundsY={bounds.min.y:F2}..{bounds.max.y:F2}");
    }

    private static float EstimateFromRenderers(GameObject mapRoot, Bounds bounds, float minY)
    {
        float bestY = float.MaxValue;
        foreach (var renderer in mapRoot.GetComponentsInChildren<Renderer>())
        {
            var b = renderer.bounds;
            if (b.size.y > Mathf.Max(b.size.x, b.size.z) * 0.45f)
                continue;

            float top = b.max.y;
            if (top < minY || top >= bestY)
                continue;

            bestY = top;
        }

        return bestY < float.MaxValue ? bestY : bounds.min.y;
    }
}
