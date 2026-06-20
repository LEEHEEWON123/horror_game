#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public static class DemoCityMapBuilder
{
    public const string CityPrefabPath =
        "Assets/Versatile Studio Assets/Demo City By Versatile Studio/Prefabs/demo_city_by_versatile_studio.prefab";

    private const float PlayMarginX = 26f;
    private const float PlayMarginZ = 14f;

    public struct BuildResult
    {
        public GameObject Root;
        public Collider RoadCollider;
        public Bounds RoadBounds;
        public Bounds PlayBounds;
        public float StreetBaselineY;
    }

    public static BuildResult Build(GameObject cityPrefab)
    {
        if (cityPrefab == null)
        {
            Debug.LogError("[DemoCityMapBuilder] City prefab is missing.");
            return default;
        }

        var root = Object.Instantiate(cityPrefab);
        root.name = "DemoCity";
        MaterialURPFixer.FixHierarchy(root);
        MarkWalkGeometryStatic(root);

        Physics.SyncTransforms();

        var roadCollider = DemoCityRoadFloor.BindRoadCollider(root);
        var roadBounds = roadCollider != null ? roadCollider.bounds : ResolveRendererBounds(root);
        float streetBaseline = DemoCityRoadFloor.ResolveStreetBaseline(
            DemoCityLayout.SpawnHint,
            roadCollider,
            roadBounds.min.y + 5.5f);
        MapFloor.SetWalkY(streetBaseline);

        var playBounds = ResolvePlayBounds(streetBaseline);
        CullOutsidePlayArea(root, playBounds);
        MapBoundaryBuilder.BuildPerimeterWalls(root.transform, playBounds, streetBaseline, inset: 1.25f);

        Debug.Log(
            $"[DemoCityMapBuilder] streetY={streetBaseline:F2} play={playBounds.size} spawn={DemoCityLayout.SpawnHint}");

        return new BuildResult
        {
            Root = root,
            RoadCollider = roadCollider,
            RoadBounds = roadBounds,
            PlayBounds = playBounds,
            StreetBaselineY = streetBaseline
        };
    }

    public static GameObject LoadDefaultPrefab()
    {
#if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<GameObject>(CityPrefabPath);
#else
        return null;
#endif
    }

    private static Bounds ResolvePlayBounds(float streetY)
    {
        float minX = Mathf.Min(DemoCityLayout.SpawnHint.x, DemoCityLayout.FallHint.x) - PlayMarginX;
        float maxX = Mathf.Max(DemoCityLayout.SpawnHint.x, DemoCityLayout.FallHint.x) + PlayMarginX;
        float minZ = Mathf.Min(DemoCityLayout.SpawnHint.z, DemoCityLayout.FallHint.z) - PlayMarginZ;
        float maxZ = Mathf.Max(DemoCityLayout.SpawnHint.z, DemoCityLayout.FallHint.z) + PlayMarginZ;

        var center = new Vector3((minX + maxX) * 0.5f, streetY, (minZ + maxZ) * 0.5f);
        var size = new Vector3(maxX - minX, 18f, maxZ - minZ);
        return new Bounds(center, size);
    }

    private static void CullOutsidePlayArea(GameObject root, Bounds playBounds)
    {
        var visibleBounds = playBounds;
        visibleBounds.Expand(6f);

        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null) continue;
            if (renderer.bounds.Intersects(visibleBounds)) continue;
            renderer.enabled = false;
        }

        foreach (var light in root.GetComponentsInChildren<Light>(true))
        {
            if (light.type == LightType.Directional) continue;
            if (visibleBounds.Contains(light.transform.position)) continue;
            light.enabled = false;
        }
    }

    private static Bounds ResolveRendererBounds(GameObject root)
    {
        var renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return ResolvePlayBounds(6f);

        var bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        return bounds;
    }

    private static void MarkWalkGeometryStatic(GameObject root)
    {
        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            renderer.gameObject.isStatic = true;

        foreach (var collider in root.GetComponentsInChildren<Collider>(true))
            collider.gameObject.isStatic = true;
    }
}
