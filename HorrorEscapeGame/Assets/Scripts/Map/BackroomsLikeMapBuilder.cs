#if UNITY_EDITOR
using UnityEditor;
#endif
using Unity.AI.Navigation;
using UnityEngine;

public static class BackroomsLikeMapBuilder
{
    public const string AssetRoot = "Assets/Asset/BackroomsLikeAsset/prefab";

    private const string FloorMatPath = "Assets/Asset/BackroomsLikeAsset/material/Floor_Carpet_Mat.mat";
    private const string CeilingMatPath = "Assets/Asset/BackroomsLikeAsset/material/Ceiling_Office_Mat.mat";

    private const float Tile = 3f;
    private const float RoomHeight = 3f;
    private const float SurfaceInset = 0.98f;
    private const int MazeCellCount = 9;
    private const int MazeSeed = 707;
    private const int MazeExtraLoops = 3;

    public struct BuildResult
    {
        public GameObject Root;
        public Bounds Bounds;
        public float WalkSurfaceY;
        public float TileSize;
        public bool[,] MazeWalls;
    }

    public static BuildResult Build()
    {
        int seed = MapGenerationContext.ResolveSeed(MazeSeed);
        var walls = MazeGenerator.Generate(MazeCellCount, seed);
        MazeGenerator.AddLoopPassages(walls, MazeExtraLoops, seed + 19);

        int width = walls.GetLength(0);
        int depth = walls.GetLength(1);

        var root = new GameObject("Map_07_BackroomsLike");
        var geometry = new GameObject("Geometry");
        geometry.transform.SetParent(root.transform, false);

        var floorMat = RuntimeMaterialLoader.Load(FloorMatPath);
        var ceilingMat = RuntimeMaterialLoader.Load(CeilingMatPath);
        var pillarPrefab = RuntimePrefabLoader.Load($"{AssetRoot}/Walls/Wall_Pillar_A.prefab");

        if (floorMat == null || ceilingMat == null)
        {
            Debug.LogError("[BackroomsLikeMapBuilder] Floor or ceiling material missing under Assets/Asset/BackroomsLikeAsset/material");
            return EmptyResult(root, walls, width, depth);
        }

        var floorRoot = new GameObject("Floors");
        var ceilingRoot = new GameObject("Ceilings");
        floorRoot.transform.SetParent(geometry.transform, false);
        ceilingRoot.transform.SetParent(geometry.transform, false);

        float surfaceSize = Tile * SurfaceInset;
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                float px = x * Tile;
                float pz = z * Tile;
                CreateSurfaceQuad(floorRoot.transform, px, 0.005f, pz, floorMat, faceUp: true, surfaceSize);
                CreateSurfaceQuad(ceilingRoot.transform, px, RoomHeight - 0.005f, pz, ceilingMat, faceUp: false, surfaceSize);
            }
        }

        if (pillarPrefab != null)
            BuildMazePillars(pillarPrefab, geometry.transform, walls);
        else
            Debug.LogWarning("[BackroomsLikeMapBuilder] Wall_Pillar_A missing — maze pillars not built.");

        var navFloors = new GameObject("NavMeshFloors");
        navFloors.transform.SetParent(root.transform, false);
        BuildNavMeshFloors(navFloors.transform, walls, Tile);

        var geometryModifier = geometry.AddComponent<NavMeshModifier>();
        geometryModifier.ignoreFromBuild = true;

        MaterialURPFixer.FixHierarchy(geometry);
        MarkStaticRecursive(geometry);
        Physics.SyncTransforms();

        float centerX = width * Tile * 0.5f - Tile * 0.5f;
        float centerZ = depth * Tile * 0.5f - Tile * 0.5f;
        float walkY = SpawnHelper.QueryFloorY(new Vector3(centerX, 0f, Tile * 0.5f), MapFloor.DefaultWalkY);
        MapFloor.SetWalkY(walkY);
        MapFloor.Calibrate(new Vector3(centerX, 0f, Tile * 0.5f));

        var bounds = new Bounds(
            new Vector3(centerX, RoomHeight * 0.5f, centerZ),
            new Vector3(width * Tile, RoomHeight, depth * Tile));

        Debug.Log($"[BackroomsLikeMapBuilder] pillar maze {MazeCellCount}x{MazeCellCount}, grid {width}x{depth}, tile {Tile}m");

        return new BuildResult
        {
            Root = root,
            Bounds = bounds,
            WalkSurfaceY = walkY,
            TileSize = Tile,
            MazeWalls = walls
        };
    }

    private static BuildResult EmptyResult(GameObject root, bool[,] walls, int width, int depth) =>
        new()
        {
            Root = root,
            Bounds = new Bounds(Vector3.zero, new Vector3(width * Tile, RoomHeight, depth * Tile)),
            WalkSurfaceY = MapFloor.DefaultWalkY,
            TileSize = Tile,
            MazeWalls = walls
        };

    private static void CreateSurfaceQuad(
        Transform parent,
        float x,
        float y,
        float z,
        Material mat,
        bool faceUp,
        float size)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = faceUp ? "FloorTile" : "CeilingTile";
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(x, y, z);
        go.transform.localRotation = Quaternion.Euler(faceUp ? 90f : -90f, 0f, 0f);
        go.transform.localScale = new Vector3(size, size, 1f);
        Object.Destroy(go.GetComponent<Collider>());

        var renderer = go.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = MaterialURPFixer.ConvertToUrP(mat);
    }

    private static void BuildMazePillars(GameObject pillarPrefab, Transform parent, bool[,] walls)
    {
        int width = walls.GetLength(0);
        int depth = walls.GetLength(1);

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                if (!walls[x, z]) continue;
                Place(pillarPrefab, parent, x * Tile, 0f, z * Tile, 0f);
            }
        }
    }

    private static void BuildNavMeshFloors(Transform parent, bool[,] walls, float tile)
    {
        int width = walls.GetLength(0);
        int depth = walls.GetLength(1);

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                if (walls[x, z])
                    continue;

                var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floor.name = $"NavFloor_{x}_{z}";
                floor.transform.SetParent(parent, false);
                floor.transform.localPosition = new Vector3(x * tile, -0.05f, z * tile);
                floor.transform.localScale = new Vector3(tile * SurfaceInset, 0.1f, tile * SurfaceInset);

                var renderer = floor.GetComponent<MeshRenderer>();
                if (renderer != null)
                    Object.Destroy(renderer);

                var collider = floor.GetComponent<Collider>();
                if (collider != null)
                    Object.Destroy(collider);

                floor.isStatic = true;
            }
        }
    }

    private static void Place(GameObject prefab, Transform parent, float x, float y, float z, float yaw)
    {
        var instance = Object.Instantiate(prefab, parent);
        instance.transform.SetParent(parent, false);
        instance.transform.localPosition = new Vector3(x, y, z);
        instance.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
    }

    private static void MarkStaticRecursive(GameObject root)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            t.gameObject.isStatic = true;
    }
}
