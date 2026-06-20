using Unity.AI.Navigation;
using UnityEngine;

public static class NavMeshWallCarver
{
    public static void Apply(Transform mapRoot)
    {
        if (mapRoot == null) return;

        foreach (var col in mapRoot.GetComponentsInChildren<Collider>(true))
        {
            if (col == null || col.isTrigger) continue;
            if (!ShouldCarve(col)) continue;
            EnsureCarver(col);
        }
    }

    private static bool ShouldCarve(Collider col)
    {
        if (col.gameObject.name.StartsWith("Boundary_"))
            return true;

        Bounds b = col.bounds;
        float horizontal = Mathf.Max(b.size.x, b.size.z);
        return b.size.y > horizontal * 0.65f && horizontal > 0.05f;
    }

    private static void EnsureCarver(Collider col)
    {
        if (col.GetComponent<UnityEngine.AI.NavMeshObstacle>() != null)
            return;

        var obstacle = col.gameObject.AddComponent<UnityEngine.AI.NavMeshObstacle>();
        obstacle.carving = true;
        obstacle.carveOnlyStationary = true;
        obstacle.shape = UnityEngine.AI.NavMeshObstacleShape.Box;

        if (col is BoxCollider box)
        {
            obstacle.center = box.center;
            obstacle.size = box.size;
            return;
        }

        Bounds world = col.bounds;
        Transform t = col.transform;
        Vector3 localCenter = t.InverseTransformPoint(world.center);
        Vector3 lossy = t.lossyScale;
        obstacle.center = localCenter;
        obstacle.size = new Vector3(
            world.size.x / Mathf.Max(Mathf.Abs(lossy.x), 0.001f),
            world.size.y / Mathf.Max(Mathf.Abs(lossy.y), 0.001f),
            world.size.z / Mathf.Max(Mathf.Abs(lossy.z), 0.001f));
    }
}
