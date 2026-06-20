#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public static class ParkingGarageIndoorBuilder
{
    private const float Tile = 3f;
    private const float RoomHeight = 3f;
    private const string MatRoot = "Assets/Parkgarage/Prefabs/parkgarage/Materials";
    private const string LampPrefabPath = "Assets/Parkgarage/Prefabs/lamps/Lamp_parkgarage_prefab.prefab";

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
        var walls = MazeGenerator.Generate(mazeSeed);
        int widthTiles = walls.GetLength(0);
        int depthTiles = walls.GetLength(1);

        var root = new GameObject("Map_02_ParkingGarage");
        var geometry = new GameObject("Geometry");
        geometry.transform.SetParent(root.transform);

        var floorMat = LoadMaterial($"{MatRoot}/Parkgarage_floor.mat");
        var pillarMat = LoadMaterial($"{MatRoot}/Big_poles.mat");
        var ceilingMat = LoadMaterial($"{MatRoot}/Floor_up_wall.mat");
        var lampPrefab = LoadPrefab(LampPrefabPath);

        BuildFloor(geometry.transform, widthTiles, depthTiles, floorMat);
        BuildCeiling(geometry.transform, widthTiles, depthTiles, ceilingMat);
        BuildMazePillars(geometry.transform, walls, pillarMat);
        BuildPassageLights(geometry.transform, walls, lampPrefab);

        MarkStaticRecursive(geometry);
        Physics.SyncTransforms();

        float centerX = widthTiles * Tile * 0.5f - Tile * 0.5f;
        float walkY = SpawnHelper.QueryFloorY(new Vector3(centerX, 0f, Tile * 0.5f), MapFloor.DefaultWalkY);

        var bounds = new Bounds(
            new Vector3(centerX, RoomHeight * 0.5f, depthTiles * Tile * 0.5f - Tile * 0.5f),
            new Vector3(widthTiles * Tile, RoomHeight, depthTiles * Tile));

        Debug.Log($"[ParkingGarageIndoorBuilder] DFS pillar maze {MazeGenerator.CellCount}x{MazeGenerator.CellCount}, grid {widthTiles}x{depthTiles}");

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

    private static void BuildFloor(Transform parent, int widthTiles, int depthTiles, Material mat)
    {
        var floorRoot = new GameObject("Floor");
        floorRoot.transform.SetParent(parent, false);

        for (int x = 0; x < widthTiles; x++)
        {
            for (int z = 0; z < depthTiles; z++)
                CreateTile(floorRoot.transform, x * Tile, -0.08f, z * Tile, new Vector3(Tile, 0.16f, Tile), mat);
        }
    }

    private static void BuildCeiling(Transform parent, int widthTiles, int depthTiles, Material mat)
    {
        var ceilingRoot = new GameObject("Ceiling");
        ceilingRoot.transform.SetParent(parent, false);

        for (int x = 0; x < widthTiles; x++)
        {
            for (int z = 0; z < depthTiles; z++)
                CreateTile(ceilingRoot.transform, x * Tile, RoomHeight, z * Tile, new Vector3(Tile, 0.18f, Tile), mat);
        }
    }

    private static void BuildMazePillars(Transform parent, bool[,] walls, Material mat)
    {
        int width = walls.GetLength(0);
        int depth = walls.GetLength(1);

        var pillarRoot = new GameObject("Pillars");
        pillarRoot.transform.SetParent(parent, false);

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                if (!walls[x, z]) continue;
                CreateTile(pillarRoot.transform, x * Tile, RoomHeight * 0.5f, z * Tile,
                    new Vector3(0.55f, RoomHeight, 0.55f), mat);
            }
        }
    }

    private static void BuildPassageLights(Transform parent, bool[,] walls, GameObject lampPrefab)
    {
        if (lampPrefab == null) return;

        int width = walls.GetLength(0);
        int depth = walls.GetLength(1);
        var lightRoot = new GameObject("CeilingLights");
        lightRoot.transform.SetParent(parent, false);

        for (int x = 2; x < width - 2; x += 4)
        {
            for (int z = 2; z < depth - 2; z += 4)
            {
                if (walls[x, z]) continue;

                var lamp = Object.Instantiate(lampPrefab, lightRoot.transform);
                lamp.transform.localPosition = new Vector3(x * Tile, RoomHeight - 0.15f, z * Tile);
                lamp.transform.localRotation = Quaternion.identity;
            }
        }
    }

    private static void CreateTile(Transform parent, float x, float y, float z, Vector3 scale, Material mat)
    {
        var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tile.transform.SetParent(parent, false);
        tile.transform.localPosition = new Vector3(x, y, z);
        tile.transform.localScale = scale;

        if (mat != null)
            tile.GetComponent<Renderer>().sharedMaterial = mat;
    }

    private static Material LoadMaterial(string path)
    {
#if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<Material>(path);
#else
        return null;
#endif
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
