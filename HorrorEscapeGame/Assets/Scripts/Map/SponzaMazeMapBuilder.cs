#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public static class SponzaMazeMapBuilder
{
    public const string SponzaFbxPath = "Assets/Sponza/Sponza.fbx";
    public const string FloorMatPath = "Assets/Sponza/Materials/floor_01.mat";
    public const string WallMatPath = "Assets/Sponza/Materials/brickwall_01.mat";
    public const string AltWallMatPath = "Assets/Sponza/Materials/brickwall_02.mat";
    public const string CeilingMatPath = "Assets/Sponza/Materials/ceiling_plaster_01.mat";
    public const string ColumnMatPath = "Assets/Sponza/Materials/column_1stfloor.mat";

    private const float Tile = 10f;
    private const float RoomHeight = 5.4f;
    private const float ModuleScale = 0.34f;
    private const float WallOverlap = 1.06f;
    private const float EdgeWallThickness = 0.55f;
    private const int MazeCellCount = 8;
    private const int MazeSeed = 303;
    private const int MazeExtraLoops = 3;

    public struct BuildResult
    {
        public GameObject Root;
        public Bounds Bounds;
        public float WalkSurfaceY;
        public float RoomHeight;
        public float TileSize;
        public bool[,] MazeWalls;
    }

    public static BuildResult Build(GameObject sponzaPrefab = null)
    {
        int seed = MapGenerationContext.ResolveSeed(MazeSeed);
        var walls = MazeGenerator.Generate(MazeCellCount, seed);
        MazeGenerator.AddLoopPassages(walls, MazeExtraLoops, seed + 17);
        MazeGenerator.AddSightPillars(walls, seed + 41, margin: 2);

        int width = walls.GetLength(0);
        int depth = walls.GetLength(1);

        var root = new GameObject("Map_03_SponzaMaze");
        var geometry = new GameObject("Geometry");
        geometry.transform.SetParent(root.transform, false);

        var floorMat = LoadMaterial(FloorMatPath);
        var wallMat = LoadMaterial(WallMatPath);
        var altWallMat = LoadMaterial(AltWallMatPath);
        var ceilingMat = LoadMaterial(CeilingMatPath);
        var columnMat = LoadMaterial(ColumnMatPath);
        var modulePrefab = sponzaPrefab != null ? sponzaPrefab : LoadDefaultPrefab();

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                float px = x * Tile;
                float pz = z * Tile;
                bool passage = !walls[x, z];

                CreateCeiling(geometry.transform, px, pz, ceilingMat);
                CreateFloorCollider(geometry.transform, px, pz, passage);

                if (passage)
                {
                    CreateFloorVisual(geometry.transform, px, pz, floorMat);
                    CreatePassageBoundaryWalls(geometry.transform, walls, x, z, width, depth);

                    if (ShouldPlaceModule(x, z) && modulePrefab != null)
                        PlaceSponzaModule(modulePrefab, geometry.transform, px, pz, x, z);
                }
                else
                {
                    var mat = IsPerimeter(x, z, width, depth)
                        ? wallMat
                        : IsInteriorPartition(walls, x, z, width, depth) ? altWallMat : wallMat;
                    CreateWallBlock(geometry.transform, px, pz, mat, RoomHeight, Tile * WallOverlap);

                    if (!IsPerimeter(x, z, width, depth) && !IsInteriorPartition(walls, x, z, width, depth))
                        CreateColumnVisual(geometry.transform, px, pz, columnMat != null ? columnMat : wallMat);
                }
            }
        }

        MaterialURPFixer.FixHierarchy(geometry);
        DarkenRenderers(geometry);
        MarkStaticRecursive(geometry);
        Physics.SyncTransforms();

        float centerX = width * Tile * 0.5f - Tile * 0.5f;
        float centerZ = depth * Tile * 0.5f - Tile * 0.5f;
        float walkY = SpawnHelper.QueryFloorY(new Vector3(centerX, 0f, Tile), MapFloor.DefaultWalkY);
        MapFloor.SetWalkY(walkY);

        var bounds = new Bounds(
            new Vector3(centerX, RoomHeight * 0.5f, centerZ),
            new Vector3(width * Tile, RoomHeight, depth * Tile));

        Debug.Log($"[SponzaMazeMapBuilder] maze {MazeCellCount}x{MazeCellCount}, grid {width}x{depth}, tile {Tile}m");

        return new BuildResult
        {
            Root = root,
            Bounds = bounds,
            WalkSurfaceY = walkY,
            RoomHeight = RoomHeight,
            TileSize = Tile,
            MazeWalls = walls
        };
    }

    public static GameObject LoadDefaultPrefab()
    {
#if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<GameObject>(SponzaFbxPath);
#else
        return null;
#endif
    }

    private static bool ShouldPlaceModule(int x, int z)
    {
        if (x % 2 == 0 || z % 2 == 0) return false;
        return ((x * 73856093) ^ (z * 19349663)) % 5 == 0;
    }

    private static void PlaceSponzaModule(GameObject prefab, Transform parent, float x, float z, int gridX, int gridZ)
    {
        var instance = Object.Instantiate(prefab, parent);
        instance.name = $"Sponza_{gridX}_{gridZ}";
        instance.transform.localPosition = new Vector3(x, 0f, z);
        instance.transform.localRotation = Quaternion.Euler(0f, ((gridX + gridZ) % 4) * 90f, 0f);
        instance.transform.localScale = Vector3.one * ModuleScale;

        TrimSponzaModuleForMaze(instance);
        EnsureModuleMeshColliders(instance);
    }

    private static void TrimSponzaModuleForMaze(GameObject instance)
    {
        foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
        {
            var name = renderer.gameObject.name.ToLowerInvariant();
            if (name.Contains("roof") || name.Contains("2nd") || name.Contains("second"))
            {
                renderer.enabled = false;
                continue;
            }

            if (renderer.bounds.max.y > RoomHeight + 0.35f)
                renderer.enabled = false;
        }
    }

    private static void EnsureModuleMeshColliders(GameObject instance)
    {
        foreach (var meshFilter in instance.GetComponentsInChildren<MeshFilter>(true))
        {
            var go = meshFilter.gameObject;
            if (meshFilter.sharedMesh == null || go.GetComponent<Collider>() != null)
                continue;

            var renderer = go.GetComponent<Renderer>();
            if (renderer != null && !renderer.enabled)
                continue;

            var meshCollider = go.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = meshFilter.sharedMesh;
            meshCollider.convex = false;
        }
    }

    private static void CreateFloorCollider(Transform parent, float x, float z, bool walkable)
    {
        var go = new GameObject(walkable ? "Floor" : "WallFloor");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(x, -0.02f, z);

        var box = go.AddComponent<BoxCollider>();
        box.size = new Vector3(Tile * 0.98f, 0.08f, Tile * 0.98f);
        box.center = new Vector3(0f, 0.04f, 0f);
        if (!walkable)
            go.layer = LayerMask.NameToLayer("Default");
    }

    private static void CreateFloorVisual(Transform parent, float x, float z, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = "FloorTile";
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(x, 0.01f, z);
        go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        go.transform.localScale = new Vector3(Tile * 0.96f, Tile * 0.96f, 1f);
        Object.Destroy(go.GetComponent<Collider>());
        ApplyMaterial(go, mat);
    }

    private static void CreateCeiling(Transform parent, float x, float z, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = "CeilingTile";
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(x, RoomHeight, z);
        go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        go.transform.localScale = new Vector3(Tile * 0.98f, Tile * 0.98f, 1f);
        Object.Destroy(go.GetComponent<Collider>());
        ApplyMaterial(go, mat);
    }

    private static void CreateWallBlock(Transform parent, float x, float z, Material mat, float height, float size)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Wall";
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(x, height * 0.5f, z);
        go.transform.localScale = new Vector3(size, height, size);
        ApplyMaterial(go, mat);
    }

    private static void CreateColumnVisual(Transform parent, float x, float z, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = "ColumnVisual";
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(x, RoomHeight * 0.5f, z);
        go.transform.localScale = new Vector3(1.35f, RoomHeight * 0.48f, 1.35f);
        Object.Destroy(go.GetComponent<Collider>());
        ApplyMaterial(go, mat);
    }

    private static void CreatePassageBoundaryWalls(
        Transform parent, bool[,] walls, int x, int z, int width, int depth)
    {
        float px = x * Tile;
        float pz = z * Tile;
        float half = Tile * 0.5f;
        float edgeLength = Tile * WallOverlap;
        float y = RoomHeight * 0.5f;

        if (x + 1 < width && walls[x + 1, z])
        {
            CreateCollisionWall(
                parent,
                new Vector3(px + half - EdgeWallThickness * 0.5f, y, pz),
                new Vector3(EdgeWallThickness, RoomHeight, edgeLength));
        }

        if (x - 1 >= 0 && walls[x - 1, z])
        {
            CreateCollisionWall(
                parent,
                new Vector3(px - half + EdgeWallThickness * 0.5f, y, pz),
                new Vector3(EdgeWallThickness, RoomHeight, edgeLength));
        }

        if (z + 1 < depth && walls[x, z + 1])
        {
            CreateCollisionWall(
                parent,
                new Vector3(px, y, pz + half - EdgeWallThickness * 0.5f),
                new Vector3(edgeLength, RoomHeight, EdgeWallThickness));
        }

        if (z - 1 >= 0 && walls[x, z - 1])
        {
            CreateCollisionWall(
                parent,
                new Vector3(px, y, pz - half + EdgeWallThickness * 0.5f),
                new Vector3(edgeLength, RoomHeight, EdgeWallThickness));
        }
    }

    private static void CreateCollisionWall(Transform parent, Vector3 localPosition, Vector3 size)
    {
        var go = new GameObject("EdgeWall");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;

        var box = go.AddComponent<BoxCollider>();
        box.size = size;
    }

    private static void ApplyMaterial(GameObject go, Material mat)
    {
        if (mat == null) return;

        var renderer = go.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = mat;
    }

    private static void DarkenRenderers(GameObject root)
    {
        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            var mats = renderer.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] == null) continue;
                var instance = Object.Instantiate(mats[i]);
                if (instance.HasProperty("_BaseColor"))
                {
                    var color = instance.GetColor("_BaseColor");
                    instance.SetColor("_BaseColor", color * 0.88f);
                }

                mats[i] = instance;
            }

            renderer.sharedMaterials = mats;
        }
    }

    private static bool IsPerimeter(int x, int z, int width, int depth) =>
        x == 0 || z == 0 || x == width - 1 || z == depth - 1;

    private static bool IsInteriorPartition(bool[,] walls, int x, int z, int width, int depth)
    {
        if (IsPerimeter(x, z, width, depth)) return false;

        int passages = 0;
        if (!walls[x + 1, z]) passages++;
        if (!walls[x - 1, z]) passages++;
        if (!walls[x, z + 1]) passages++;
        if (!walls[x, z - 1]) passages++;

        return passages == 2;
    }

    private static Material LoadMaterial(string path)
    {
#if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<Material>(path);
#else
        return null;
#endif
    }

    private static void MarkStaticRecursive(GameObject root)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            t.gameObject.isStatic = true;
    }
}
