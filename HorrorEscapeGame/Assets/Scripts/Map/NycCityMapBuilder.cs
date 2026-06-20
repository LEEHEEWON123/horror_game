#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Rendering;

public static class NycCityMapBuilder
{
    public const string BuildingsFolder =
        "Assets/(HDRP) NYC-Like City Buildings Set (PBR)/Prefabs/Buildings";

    private const int MazeCellCount = 8;
    private const int MazeSeed = 505;
    private const int MazeExtraLoops = 6;
    private const float TileSize = 11f;
    private const float BuildingFootprintFill = 1f;

    public struct BuildResult
    {
        public GameObject Root;
        public Bounds Bounds;
        public float WalkSurfaceY;
        public bool[,] MazeWalls;
        public float Tile;
    }

    private static readonly string[] BuildingPrefabs =
    {
        "building_4_1 Variant.prefab",
        "building_3_1 Variant.prefab",
        "building_2_1 Variant.prefab",
        "building_1_1 Variant.prefab",
    };

    private const string StreetMaterialPath =
        "Assets/(HDRP) NYC-Like City Buildings Set (PBR)/Materials/concrate.mat";

    public static BuildResult Build()
    {
        int seed = MapGenerationContext.ResolveSeed(MazeSeed);
        var walls = MazeGenerator.Generate(MazeCellCount, seed);
        MazeGenerator.AddLoopPassages(walls, MazeExtraLoops, seed + 17);
        int widthTiles = walls.GetLength(0);
        int depthTiles = walls.GetLength(1);

        var root = new GameObject("Map_05_NYC_Maze");
        var geometry = new GameObject("Geometry");
        geometry.transform.SetParent(root.transform, false);

        PlaceSharedStreetFloor(geometry.transform, widthTiles, depthTiles);

        for (int x = 0; x < widthTiles; x++)
        {
            for (int z = 0; z < depthTiles; z++)
            {
                if (!walls[x, z]) continue;
                var worldPos = new Vector3(x * TileSize, 0f, z * TileSize);
                PlaceBuilding(geometry.transform, walls, worldPos, x, z);
            }
        }

        MaterialURPFixer.FixHierarchy(geometry);
        OptimizeBuildingVisuals(geometry);
        SanitizeImportedHierarchy(geometry);
        StripBuildingColliders(geometry.transform);
        EnsureBuildingColliders(geometry.transform, walls);
        MarkStaticRecursive(root);

        Physics.SyncTransforms();

        float centerX = widthTiles * TileSize * 0.5f - TileSize * 0.5f;
        float centerZ = depthTiles * TileSize * 0.5f - TileSize * 0.5f;
        var bounds = new Bounds(
            new Vector3(centerX, 24f, centerZ),
            new Vector3(widthTiles * TileSize, 48f, depthTiles * TileSize));

        const float walkY = 0f;
        MapFloor.SetWalkY(walkY);
        MapBoundaryBuilder.BuildPerimeterWalls(root.transform, bounds, walkY, inset: 1.5f);
        MarkStaticRecursive(root);
        Physics.SyncTransforms();
        ApplyPerformanceSettings(bounds);
        NycCityAtmosphere.Apply(bounds);

        Debug.Log($"[NycCityMapBuilder] Alley maze {widthTiles}x{depthTiles}, tile={TileSize:F1}m");

        return new BuildResult
        {
            Root = root,
            Bounds = bounds,
            WalkSurfaceY = walkY,
            MazeWalls = walls,
            Tile = TileSize
        };
    }

    private static void ApplyPerformanceSettings(Bounds bounds)
    {
        QualitySettings.shadowDistance = Mathf.Min(QualitySettings.shadowDistance, 45f);
        QualitySettings.shadowResolution = ShadowResolution.Low;
        QualitySettings.pixelLightCount = 1;

        foreach (var light in Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude))
        {
            if (light.type != LightType.Directional) continue;
            light.shadows = LightShadows.None;
        }
    }

    private static void PlaceSharedStreetFloor(Transform parent, int widthTiles, int depthTiles)
    {
        float mapWidth = widthTiles * TileSize;
        float mapDepth = depthTiles * TileSize;
        var center = new Vector3(mapWidth * 0.5f - TileSize * 0.5f, -0.04f, mapDepth * 0.5f - TileSize * 0.5f);

        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "StreetFloor";
        floor.transform.SetParent(parent, false);
        floor.transform.position = center;
        floor.transform.localScale = new Vector3(mapWidth + TileSize, 0.08f, mapDepth + TileSize);

        var renderer = floor.GetComponent<Renderer>();
        if (renderer != null)
        {
            var mat = CreateStreetMaterial(mapWidth, mapDepth);
            if (mat != null)
                renderer.sharedMaterial = mat;

            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = true;
        }

        floor.isStatic = true;
    }

    private static Material CreateStreetMaterial(float mapWidth, float mapDepth)
    {
#if UNITY_EDITOR
        var source = AssetDatabase.LoadAssetAtPath<Material>(StreetMaterialPath);
        var mat = source != null ? MaterialURPFixer.ConvertToUrP(source) : null;
#else
        Material mat = null;
#endif
        if (mat == null)
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));

        mat.SetColor("_BaseColor", new Color(0.22f, 0.22f, 0.24f));
        mat.SetFloat("_Smoothness", 0.22f);
        mat.SetFloat("_Metallic", 0.02f);

        float tileRepeat = 3.5f;
        mat.SetTextureScale("_BaseMap", new Vector2(mapWidth / tileRepeat, mapDepth / tileRepeat));
        if (mat.HasProperty("_BumpMap"))
            mat.SetTextureScale("_BumpMap", new Vector2(mapWidth / tileRepeat, mapDepth / tileRepeat));

        return mat;
    }

    private static void PlaceBuilding(Transform parent, bool[,] walls, Vector3 worldPos, int gridX, int gridZ)
    {
        int prefabIndex = (gridX * 3 + gridZ) % BuildingPrefabs.Length;
        var prefab = LoadBuildingPrefab(BuildingPrefabs[prefabIndex]);
        if (prefab == null) return;

        var instance = Object.Instantiate(prefab, parent);
        instance.name = $"Building_{gridX}_{gridZ}";

        float rotationY = ResolveAlleyRotation(walls, gridX, gridZ);
        instance.transform.SetPositionAndRotation(worldPos, Quaternion.Euler(0f, rotationY, 0f));
        FitBuildingToTile(instance, TileSize, worldPos);
    }

    private static float ResolveAlleyRotation(bool[,] walls, int x, int z)
    {
        int width = walls.GetLength(0);
        int depth = walls.GetLength(1);

        bool openEast = x + 1 < width && !walls[x + 1, z];
        bool openWest = x - 1 >= 0 && !walls[x - 1, z];
        bool openNorth = z + 1 < depth && !walls[x, z + 1];
        bool openSouth = z - 1 >= 0 && !walls[x, z - 1];

        if (openEast || openWest)
            return 90f;
        if (openNorth || openSouth)
            return 0f;

        return ((x + z) & 1) == 0 ? 0f : 90f;
    }

    private static void FitBuildingToTile(GameObject instance, float tileSize, Vector3 cellCenter)
    {
        var renderers = instance.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return;

        Bounds bounds = EncapsulateRendererBounds(renderers);
        float footprint = Mathf.Max(bounds.size.x, bounds.size.z);
        if (footprint <= 0.01f) return;

        float scale = tileSize * BuildingFootprintFill / footprint;
        instance.transform.localScale = Vector3.one * scale;

        bounds = EncapsulateRendererBounds(renderers);
        Vector3 pos = instance.transform.position;
        pos.x += cellCenter.x - bounds.center.x;
        pos.z += cellCenter.z - bounds.center.z;
        instance.transform.position = pos;

        bounds = EncapsulateRendererBounds(renderers);
        pos = instance.transform.position;
        pos.y += -bounds.min.y;
        instance.transform.position = pos;
    }

    private static Bounds EncapsulateRendererBounds(Renderer[] renderers)
    {
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }

    private static Bounds EncapsulateFootprintBounds(Renderer[] renderers)
    {
        Bounds? footprint = null;
        foreach (var renderer in renderers)
        {
            string n = renderer.gameObject.name.ToLowerInvariant();
            if (n.Contains("top") || n.Contains("roof") || n.Contains("parapet"))
                continue;

            if (!footprint.HasValue)
                footprint = renderer.bounds;
            else
            {
                Bounds b = footprint.Value;
                b.Encapsulate(renderer.bounds);
                footprint = b;
            }
        }

        return footprint ?? EncapsulateRendererBounds(renderers);
    }

    private static void OptimizeBuildingVisuals(GameObject geometry)
    {
        foreach (var renderer in geometry.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer.gameObject.name == "StreetFloor") continue;

            string n = renderer.gameObject.name.ToLowerInvariant();
            if (n.Contains("decal") || n.Contains("leaf") || n.Contains("tree") || n.Contains("rope"))
            {
                Object.Destroy(renderer.gameObject);
                continue;
            }

            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }
    }

    private static void StripBuildingColliders(Transform geometryRoot)
    {
        foreach (Transform child in geometryRoot)
        {
            if (!child.name.StartsWith("Building_")) continue;

            foreach (var col in child.GetComponentsInChildren<Collider>(true))
                Object.Destroy(col);
        }
    }

    private static void EnsureBuildingColliders(Transform geometryRoot, bool[,] walls)
    {
        foreach (Transform child in geometryRoot)
        {
            if (!child.name.StartsWith("Building_")) continue;
            if (!TryParseBuildingGrid(child.name, out int gridX, out int gridZ)) continue;
            FitBuildingCollider(child.gameObject, walls, gridX, gridZ);
        }
    }

    private static bool TryParseBuildingGrid(string name, out int gridX, out int gridZ)
    {
        gridX = 0;
        gridZ = 0;
        var parts = name.Split('_');
        if (parts.Length < 3) return false;
        return int.TryParse(parts[1], out gridX) && int.TryParse(parts[2], out gridZ);
    }

    private static void FitBuildingCollider(GameObject building, bool[,] walls, int gridX, int gridZ)
    {
        var renderers = building.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return;

        Bounds footprint = EncapsulateFootprintBounds(renderers);
        Bounds fullHeight = EncapsulateRendererBounds(renderers);
        var world = new Bounds(
            new Vector3(footprint.center.x, fullHeight.center.y, footprint.center.z),
            new Vector3(footprint.size.x, fullHeight.size.y, footprint.size.z));

        int width = walls.GetLength(0);
        int depth = walls.GetLength(1);
        bool openEast = gridX + 1 < width && !walls[gridX + 1, gridZ];
        bool openWest = gridX - 1 >= 0 && !walls[gridX - 1, gridZ];
        bool openNorth = gridZ + 1 < depth && !walls[gridX, gridZ + 1];
        bool openSouth = gridZ - 1 >= 0 && !walls[gridX, gridZ - 1];

        // Seal interior corners only; leave passage-facing sides tight to the mesh.
        const float seal = 0.2f;
        if (!openEast) world.max = new Vector3(world.max.x + seal, world.max.y, world.max.z);
        if (!openWest) world.min = new Vector3(world.min.x - seal, world.min.y, world.min.z);
        if (!openNorth) world.max = new Vector3(world.max.x, world.max.y, world.max.z + seal);
        if (!openSouth) world.min = new Vector3(world.min.x, world.min.y, world.min.z - seal);

        ApplyWorldBoundsBoxCollider(building, world);
    }

    private static void ApplyWorldBoundsBoxCollider(GameObject building, Bounds worldBounds)
    {
        Vector3 center = worldBounds.center;
        Vector3 extents = worldBounds.extents;
        Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

        Vector3[] corners =
        {
            center + new Vector3(-extents.x, -extents.y, -extents.z),
            center + new Vector3(-extents.x, -extents.y, extents.z),
            center + new Vector3(-extents.x, extents.y, -extents.z),
            center + new Vector3(-extents.x, extents.y, extents.z),
            center + new Vector3(extents.x, -extents.y, -extents.z),
            center + new Vector3(extents.x, -extents.y, extents.z),
            center + new Vector3(extents.x, extents.y, -extents.z),
            center + new Vector3(extents.x, extents.y, extents.z),
        };

        foreach (Vector3 corner in corners)
        {
            Vector3 local = building.transform.InverseTransformPoint(corner);
            min = Vector3.Min(min, local);
            max = Vector3.Max(max, local);
        }

        var box = building.GetComponent<BoxCollider>();
        if (box == null)
            box = building.AddComponent<BoxCollider>();

        box.center = (min + max) * 0.5f;
        box.size = max - min;
        box.size = new Vector3(
            Mathf.Max(box.size.x, 0.5f),
            Mathf.Max(box.size.y, 6f),
            Mathf.Max(box.size.z, 0.5f));
    }

    private static void SanitizeImportedHierarchy(GameObject mapRoot)
    {
        foreach (var cam in mapRoot.GetComponentsInChildren<Camera>(true))
            Object.Destroy(cam);

        foreach (var probe in mapRoot.GetComponentsInChildren<ReflectionProbe>(true))
            Object.Destroy(probe);

        foreach (var volume in mapRoot.GetComponentsInChildren<Volume>(true))
            Object.Destroy(volume);

        foreach (var light in mapRoot.GetComponentsInChildren<Light>(true))
            Object.Destroy(light.gameObject);
    }

    private static void MarkStaticRecursive(GameObject root)
    {
        root.isStatic = true;
        foreach (Transform child in root.transform)
            MarkStaticRecursive(child.gameObject);
    }

    private static GameObject LoadBuildingPrefab(string fileName)
    {
#if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<GameObject>($"{BuildingsFolder}/{fileName}");
#else
        return null;
#endif
    }
}
