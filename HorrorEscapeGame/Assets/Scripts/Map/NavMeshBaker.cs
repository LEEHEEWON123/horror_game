using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

public static class NavMeshBaker
{
    public static void BakeForMap(Bounds bounds)
    {
        var surfaceGo = new GameObject("NavMeshSurface");
        var surface = surfaceGo.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.All;
        BakeSurface(surface);
    }

    public static void BakeForMapRoot(Transform mapRoot, bool carveWalls = true)
    {
        if (mapRoot == null)
        {
            Debug.LogWarning("NavMesh bake skipped — map root is null.");
            return;
        }

        if (carveWalls)
            NavMeshWallCarver.Apply(mapRoot);

        var surface = mapRoot.GetComponent<NavMeshSurface>();
        if (surface == null)
            surface = mapRoot.gameObject.AddComponent<NavMeshSurface>();

        surface.collectObjects = CollectObjects.Children;
        BakeSurface(surface, mapRoot);
    }

    private static void BakeSurface(NavMeshSurface surface, Transform scope = null)
    {
        var skipped = DisableUnreadableMeshColliders(scope);
        try
        {
            // Prefer render meshes; unreadable asset-store FBX needs Read/Write or dedicated nav geometry.
            surface.useGeometry = NavMeshCollectGeometry.RenderMeshes;
            surface.BuildNavMesh();

            if (!HasNavMeshData())
            {
                surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
                surface.BuildNavMesh();
            }
        }
        finally
        {
            RestoreColliders(skipped);
        }

        if (!HasNavMeshData())
            Debug.LogWarning("NavMesh bake produced no walkable area — check floor geometry.");
    }

    private static List<MeshCollider> DisableUnreadableMeshColliders(Transform scope)
    {
        var skipped = new List<MeshCollider>();
        MeshCollider[] colliders = scope != null
            ? scope.GetComponentsInChildren<MeshCollider>(true)
            : Object.FindObjectsByType<MeshCollider>(FindObjectsSortMode.None);

        foreach (var collider in colliders)
        {
            if (collider == null || !collider.enabled)
                continue;

            var mesh = collider.sharedMesh;
            if (mesh == null || mesh.isReadable)
                continue;

            collider.enabled = false;
            skipped.Add(collider);
        }

        return skipped;
    }

    private static void RestoreColliders(List<MeshCollider> colliders)
    {
        foreach (var collider in colliders)
        {
            if (collider != null)
                collider.enabled = true;
        }
    }

    private static bool HasNavMeshData()
    {
        var triangulation = NavMesh.CalculateTriangulation();
        return triangulation.indices != null && triangulation.indices.Length > 0;
    }
}
