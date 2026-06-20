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

    public static void BakeForMapRoot(
        Transform mapRoot,
        bool carveWalls = true,
        bool preferPhysicsColliders = false)
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
        BakeSurface(surface, mapRoot, preferPhysicsColliders);
    }

    private static void BakeSurface(
        NavMeshSurface surface,
        Transform scope = null,
        bool preferPhysicsColliders = false)
    {
        var skippedColliders = DisableUnreadableMeshColliders(scope);
        var skippedRenderers = DisableUnreadableMeshRenderers(scope);
        try
        {
            // Physics colliders work in WebGL/player; imported FBX render meshes often are not readable.
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.BuildNavMesh();

            if (!HasNavMeshData() && !preferPhysicsColliders)
            {
                surface.useGeometry = NavMeshCollectGeometry.RenderMeshes;
                surface.BuildNavMesh();
            }
        }
        finally
        {
            RestoreColliders(skippedColliders);
            RestoreRenderers(skippedRenderers);
        }

        if (!HasNavMeshData())
            Debug.LogWarning("NavMesh bake produced no walkable area — check floor geometry.");
    }

    private static List<MeshRenderer> DisableUnreadableMeshRenderers(Transform scope)
    {
        var skipped = new List<MeshRenderer>();
        MeshRenderer[] renderers = scope != null
            ? scope.GetComponentsInChildren<MeshRenderer>(true)
            : Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);

        foreach (var renderer in renderers)
        {
            if (renderer == null || !renderer.enabled)
                continue;

            var mesh = GetSharedMesh(renderer);
            if (mesh == null || mesh.isReadable)
                continue;

            renderer.enabled = false;
            skipped.Add(renderer);
        }

        return skipped;
    }

    private static Mesh GetSharedMesh(Renderer renderer)
    {
        if (renderer is SkinnedMeshRenderer skinned)
            return skinned.sharedMesh;

        var filter = renderer.GetComponent<MeshFilter>();
        return filter != null ? filter.sharedMesh : null;
    }

    private static void RestoreRenderers(List<MeshRenderer> renderers)
    {
        foreach (var renderer in renderers)
        {
            if (renderer != null)
                renderer.enabled = true;
        }
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
