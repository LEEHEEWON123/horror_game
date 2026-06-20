#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public static class BackroomsIndoorBuilder
{
    private const float Tile = 3f;
    private const float RoomHeight = 3f;
    private const string AssetRoot = "Assets/LoafbrrAssets/BackroomsLikeAssetRe/prefab";

    public struct BuildResult
    {
        public GameObject Root;
        public Bounds Bounds;
        public float WalkSurfaceY;
        public int WidthTiles;
        public int DepthTiles;
        public bool[,] MazeWalls;
    }

    public static BuildResult Build(int? mazeSeed = null)
    {
        int seed = mazeSeed ?? MapGenerationContext.ResolveSeed(101);
        var walls = MazeGenerator.Generate(MazeGenerator.CellCount, seed);
        int widthTiles = walls.GetLength(0);
        int depthTiles = walls.GetLength(1);

        var root = new GameObject("Map_01_Backrooms");

        var floorPrefab = LoadPrefab($"{AssetRoot}/Floor/BR_Floor_3x3.prefab");
        var ceilingPrefab = LoadPrefab($"{AssetRoot}/Wall/BR_Wall_B_3x3.prefab");
        var pillarPrefab = LoadPrefab($"{AssetRoot}/Wall/BR_Wall_B_Post_3m.prefab");

        if (floorPrefab == null)
        {
            Debug.LogError("Backrooms floor prefab missing.");
            return EmptyResult(root, widthTiles, depthTiles, walls);
        }

        var geometry = new GameObject("Geometry");
        geometry.transform.SetParent(root.transform);

        for (int x = 0; x < widthTiles; x++)
        {
            for (int z = 0; z < depthTiles; z++)
                Place(floorPrefab, geometry.transform, x * Tile, 0f, z * Tile, 0f);
        }

        if (pillarPrefab != null)
            BuildMazePillars(pillarPrefab, geometry.transform, walls);
        else
            Debug.LogWarning("Backrooms pillar prefab missing — maze walls not built.");

        if (ceilingPrefab != null)
            BuildCeiling(ceilingPrefab, geometry.transform, widthTiles, depthTiles);

        MarkStaticRecursive(geometry);
        Physics.SyncTransforms();

        float centerX = widthTiles * Tile * 0.5f - Tile * 0.5f;
        float walkY = SpawnHelper.QueryFloorY(new Vector3(centerX, 0f, Tile * 0.5f), MapFloor.DefaultWalkY);

        var bounds = new Bounds(
            new Vector3(centerX, RoomHeight * 0.5f, depthTiles * Tile * 0.5f - Tile * 0.5f),
            new Vector3(widthTiles * Tile, RoomHeight, depthTiles * Tile));

        Debug.Log($"[BackroomsIndoorBuilder] DFS pillar maze {MazeGenerator.CellCount}x{MazeGenerator.CellCount} cells, grid {widthTiles}x{depthTiles}");

        return new BuildResult
        {
            Root = root,
            Bounds = bounds,
            WalkSurfaceY = walkY,
            WidthTiles = widthTiles,
            DepthTiles = depthTiles,
            MazeWalls = walls
        };
    }

    private static BuildResult EmptyResult(GameObject root, int w, int d, bool[,] walls) =>
        new()
        {
            Root = root,
            Bounds = new Bounds(Vector3.zero, new Vector3(w * Tile, 3f, d * Tile)),
            WalkSurfaceY = MapFloor.DefaultWalkY,
            WidthTiles = w,
            DepthTiles = d,
            MazeWalls = walls
        };

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

    private static void BuildCeiling(GameObject ceilingPrefab, Transform parent, int widthTiles, int depthTiles)
    {
        var ceilingRoot = new GameObject("Ceiling");
        ceilingRoot.transform.SetParent(parent, false);

        for (int x = 0; x < widthTiles; x++)
        {
            for (int z = 0; z < depthTiles; z++)
            {
                var instance = Object.Instantiate(ceilingPrefab, ceilingRoot.transform);
                instance.transform.localPosition = new Vector3(x * Tile, RoomHeight, z * Tile);
                instance.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                instance.transform.localScale = new Vector3(1.05f, 1f, 1.05f);
                EnableDoubleSided(instance);
            }
        }
    }

    private static void EnableDoubleSided(GameObject root)
    {
        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            var materials = renderer.materials;
            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] == null) continue;
                materials[i] = new Material(materials[i]);
                materials[i].SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
            }

            renderer.materials = materials;
        }
    }

    private static void Place(GameObject prefab, Transform parent, float x, float y, float z, float yaw)
    {
        var instance = Object.Instantiate(prefab, parent);
        instance.transform.SetParent(parent, false);
        instance.transform.localPosition = new Vector3(x, y, z);
        instance.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
    }

    private static GameObject LoadPrefab(string path)
    {
#if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
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
