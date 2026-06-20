using UnityEngine;

public static class PortalPreviewLayer
{
    public const string LayerName = "PortalPreview";
    private const int FallbackLayer = 31;

    public static int Index
    {
        get
        {
            int layer = LayerMask.NameToLayer(LayerName);
            return layer >= 0 ? layer : FallbackLayer;
        }
    }

    public static int Mask => 1 << Index;

    public static void ApplyRecursively(GameObject root)
    {
        if (root == null) return;

        int layer = Index;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            t.gameObject.layer = layer;
    }

    public static void DisablePhysicsRecursively(GameObject root)
    {
        if (root == null) return;

        foreach (var collider in root.GetComponentsInChildren<Collider>(true))
            collider.enabled = false;

        foreach (var rb in root.GetComponentsInChildren<Rigidbody>(true))
            rb.isKinematic = true;

        foreach (var agent in root.GetComponentsInChildren<UnityEngine.AI.NavMeshAgent>(true))
            agent.enabled = false;
    }

    public static void HideFromCamera(Camera camera)
    {
        if (camera == null) return;
        camera.cullingMask &= ~Mask;
    }

    public static void RestoreCamera(Camera camera, int previousMask)
    {
        if (camera == null) return;
        camera.cullingMask = previousMask;
    }
}
