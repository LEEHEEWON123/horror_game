#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public static class PrototypeMapMapBuilder
{
    public const string PrefabPath = "Assets/AngeloMaN87/Prototype Map/Prefabs/PrototypeMap.prefab";

    public struct BuildResult
    {
        public GameObject Root;
        public Bounds Bounds;
        public float WalkSurfaceY;
    }

    public static BuildResult Build()
    {
        var root = new GameObject("Map_04_Prototype");
        var prefab = LoadPrefab();
        if (prefab == null)
        {
            Debug.LogError($"Prototype map prefab missing at {PrefabPath}");
            return EmptyResult(root);
        }

        var instance = Object.Instantiate(prefab, root.transform);
        instance.name = "PrototypeMap";
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;

        MaterialURPFixer.FixHierarchy(instance);
        DisableEmbeddedCameras(instance);
        EnsureMeshColliders(instance);
        MarkStaticRecursive(instance);
        Physics.SyncTransforms();

        var bounds = CalculateBounds(instance);
        float floorY = Map04Viewpoint.ResolveGroundY(bounds, instance);
        MapFloor.SetWalkY(floorY);
        Debug.Log($"[MapFloor] Walk surface Y = {floorY:F3}");

        MapBoundaryBuilder.BuildPerimeterWalls(root.transform, bounds, floorY);
        MarkStaticRecursive(root);
        Physics.SyncTransforms();
        PrototypeMapAtmosphere.Apply(bounds);

        return new BuildResult
        {
            Root = root,
            Bounds = bounds,
            WalkSurfaceY = MapFloor.WalkY
        };
    }

    private static BuildResult EmptyResult(GameObject root)
    {
        return new BuildResult
        {
            Root = root,
            Bounds = new Bounds(Vector3.zero, Vector3.one * 20f),
            WalkSurfaceY = MapFloor.DefaultWalkY
        };
    }

    private static void EnsureMeshColliders(GameObject mapRoot)
    {
        foreach (var meshFilter in mapRoot.GetComponentsInChildren<MeshFilter>(true))
        {
            var go = meshFilter.gameObject;
            if (meshFilter.sharedMesh == null || go.GetComponent<Collider>() != null)
                continue;

            var meshCollider = go.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = meshFilter.sharedMesh;
            meshCollider.convex = false;
        }
    }

    private static void DisableEmbeddedCameras(GameObject mapRoot)
    {
        foreach (var cam in mapRoot.GetComponentsInChildren<Camera>(true))
            Object.Destroy(cam);
    }

    private static Bounds CalculateBounds(GameObject mapRoot)
    {
        var renderers = mapRoot.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return new Bounds(mapRoot.transform.position, Vector3.one * 40f);

        var bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }

    private static void MarkStaticRecursive(GameObject root)
    {
        root.isStatic = true;
        foreach (Transform child in root.transform)
            MarkStaticRecursive(child.gameObject);
    }

    private static GameObject LoadPrefab()
    {
        return RuntimePrefabLoader.Load(PrefabPath);
    }
}
