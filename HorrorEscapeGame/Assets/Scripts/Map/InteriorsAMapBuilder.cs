#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public static class InteriorsAMapBuilder
{
    private const string AssetRoot = "Assets/LoafbrrAssets/Interiors_A/prefabs";
    private const float Tile = 3f;
    private const float CorridorCeilingY = 3f;
    private const float MezzanineY = 3f;
    private const float MezzanineCeilingY = 6f;
    private const int MazeCellCount = 8;
    private const int MazeSeed = 606;
    private const int MazeExtraLoops = 2;

    public struct BuildResult
    {
        public GameObject Root;
        public Bounds Bounds;
        public float WalkSurfaceY;
        public float MezzanineSurfaceY;
        public float TileSize;
        public bool[,] MazeWalls;
    }

    public static BuildResult Build()
    {
        int seed = MapGenerationContext.ResolveSeed(MazeSeed);
        var walls = MazeGenerator.Generate(MazeCellCount, seed);
        MazeGenerator.AddLoopPassages(walls, MazeExtraLoops, seed + 23);
        MazeGenerator.AddSightPillars(walls, seed + 99);

        int width = walls.GetLength(0);
        int depth = walls.GetLength(1);

        var root = new GameObject("Map_06_Interiors");
        var geometry = new GameObject("Geometry");
        geometry.transform.SetParent(root.transform, false);

        var floorPrefab = LoadPrefab($"{AssetRoot}/Floor/Floor_3x3.prefab");
        var corridorCeilingPrefab = LoadPrefab($"{AssetRoot}/Floor/FloorCeiling_3x3.prefab");
        var pillarPrefab = LoadPrefab($"{AssetRoot}/wall/Wall_Pillar_A.prefab");
        var partitionPrefab = LoadPrefab($"{AssetRoot}/wall/Wall_3m_1Side.prefab");
        var outerWallPrefab = LoadPrefab($"{AssetRoot}/wall/Wall_3m.prefab");
        var bottomTrimPrefab = LoadPrefab($"{AssetRoot}/wall/Wall_Bottom_3m.prefab");
        var ceilingLightPrefab = LoadPrefab($"{AssetRoot}/Decal/Decal_Ceiling_B_Light.prefab");
        var railPrefab = LoadPrefab($"{AssetRoot}/StairParts/Stair_Rail_3m.prefab");
        var stairsPrefab = LoadPrefab($"{AssetRoot}/StairSet/Stairs_2m_B_Grp.prefab");

        if (floorPrefab == null)
        {
            Debug.LogError("[InteriorsAMapBuilder] Floor_3x3 prefab missing.");
            return EmptyResult(root, width, depth, walls);
        }

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                float px = x * Tile;
                float pz = z * Tile;
                bool passage = !walls[x, z];
                bool mezzanine = passage && IsMezzanineCell(x, z, width, depth);

                Place(floorPrefab, geometry.transform, px, 0f, pz, 0f);

                if (mezzanine)
                    Place(floorPrefab, geometry.transform, px, MezzanineY, pz, 0f);

                if (passage && !mezzanine && corridorCeilingPrefab != null)
                    Place(corridorCeilingPrefab, geometry.transform, px, CorridorCeilingY, pz, 0f);

                if (mezzanine && corridorCeilingPrefab != null)
                    Place(corridorCeilingPrefab, geometry.transform, px, MezzanineCeilingY, pz, 0f);

                if (walls[x, z])
                {
                    if (IsPerimeter(x, z, width, depth) && outerWallPrefab != null)
                        Place(outerWallPrefab, geometry.transform, px, 0f, pz, PickOuterYaw(x, z, width, depth));
                    else if (IsInteriorPartition(walls, x, z, width, depth) && partitionPrefab != null)
                        Place(partitionPrefab, geometry.transform, px, 0f, pz, PickPartitionYaw(walls, x, z));
                    else if (pillarPrefab != null)
                        Place(pillarPrefab, geometry.transform, px, 0f, pz, 0f);
                }

                if (bottomTrimPrefab != null && IsPerimeter(x, z, width, depth))
                    Place(bottomTrimPrefab, geometry.transform, px, 0f, pz, PickOuterYaw(x, z, width, depth));
            }
        }

        if (ceilingLightPrefab != null)
        {
            var lightsRoot = new GameObject("CeilingLights");
            lightsRoot.transform.SetParent(geometry.transform, false);
            for (int x = 2; x < width - 2; x += 3)
            {
                for (int z = 2; z < depth - 2; z += 3)
                {
                    if (walls[x, z]) continue;
                    float lightY = IsMezzanineCell(x, z, width, depth) ? MezzanineCeilingY - 0.08f : CorridorCeilingY - 0.08f;
                    Place(ceilingLightPrefab, lightsRoot.transform, x * Tile, lightY, z * Tile, 0f);
                }
            }
        }

        if (railPrefab != null)
            BuildMezzanineRails(railPrefab, geometry.transform, walls, width, depth);

        if (stairsPrefab != null)
            PlaceStairs(stairsPrefab, geometry.transform, walls, width, depth);

        OptimizeHierarchy(geometry);
        MaterialURPFixer.FixHierarchy(geometry);
        MarkStaticRecursive(geometry);
        Physics.SyncTransforms();

        float centerX = width * Tile * 0.5f - Tile * 0.5f;
        float centerZ = depth * Tile * 0.5f - Tile * 0.5f;
        float walkY = SpawnHelper.QueryFloorY(new Vector3(centerX, 0f, Tile), MapFloor.DefaultWalkY);
        float mezzY = walkY + MezzanineY;

        var bounds = new Bounds(
            new Vector3(centerX, MezzanineCeilingY * 0.5f, centerZ),
            new Vector3(width * Tile, MezzanineCeilingY, depth * Tile));

        Debug.Log($"[InteriorsAMapBuilder] tight interior maze {MazeCellCount}x{MazeCellCount}, grid {width}x{depth}");

        return new BuildResult
        {
            Root = root,
            Bounds = bounds,
            WalkSurfaceY = walkY,
            MezzanineSurfaceY = mezzY,
            TileSize = Tile,
            MazeWalls = walls
        };
    }

    private static BuildResult EmptyResult(GameObject root, int width, int depth, bool[,] walls) =>
        new()
        {
            Root = root,
            Bounds = new Bounds(Vector3.zero, new Vector3(width * Tile, MezzanineCeilingY, depth * Tile)),
            WalkSurfaceY = MapFloor.DefaultWalkY,
            MezzanineSurfaceY = MapFloor.DefaultWalkY + MezzanineY,
            TileSize = Tile,
            MazeWalls = walls
        };

    private static bool IsMezzanineCell(int x, int z, int width, int depth) =>
        x == 1 || x == width - 2 || z == 1 || z == depth - 2;

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

    private static float PickPartitionYaw(bool[,] walls, int x, int z)
    {
        bool openX = !walls[x + 1, z] || !walls[x - 1, z];
        return openX ? 90f : 0f;
    }

    private static float PickOuterYaw(int x, int z, int width, int depth)
    {
        if (z == 0) return 0f;
        if (x == width - 1) return 90f;
        if (z == depth - 1) return 180f;
        return 270f;
    }

    private static void BuildMezzanineRails(GameObject railPrefab, Transform parent, bool[,] walls, int width, int depth)
    {
        var railsRoot = new GameObject("MezzanineRails");
        railsRoot.transform.SetParent(parent, false);

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                if (walls[x, z] || !IsMezzanineCell(x, z, width, depth)) continue;

                float px = x * Tile;
                float pz = z * Tile;

                if (x + 1 < width && !walls[x + 1, z] && !IsMezzanineCell(x + 1, z, width, depth))
                    PlaceDecorative(railPrefab, railsRoot.transform, px + Tile * 0.5f, MezzanineY, pz, 90f);

                if (x - 1 >= 0 && !walls[x - 1, z] && !IsMezzanineCell(x - 1, z, width, depth))
                    PlaceDecorative(railPrefab, railsRoot.transform, px - Tile * 0.5f, MezzanineY, pz, 270f);

                if (z + 1 < depth && !walls[x, z + 1] && !IsMezzanineCell(x, z + 1, width, depth))
                    PlaceDecorative(railPrefab, railsRoot.transform, px, MezzanineY, pz + Tile * 0.5f, 0f);

                if (z - 1 >= 0 && !walls[x, z - 1] && !IsMezzanineCell(x, z - 1, width, depth))
                    PlaceDecorative(railPrefab, railsRoot.transform, px, MezzanineY, pz - Tile * 0.5f, 180f);
            }
        }
    }

    private static void PlaceStairs(GameObject stairsPrefab, Transform parent, bool[,] walls, int width, int depth)
    {
        var stairsRoot = new GameObject("Stairs");
        stairsRoot.transform.SetParent(parent, false);

        TryPlaceStairs(stairsPrefab, stairsRoot.transform, walls, width, depth, 1, 1, 0f);
        TryPlaceStairs(stairsPrefab, stairsRoot.transform, walls, width, depth, width - 2, depth - 2, 180f);
    }

    private static void TryPlaceStairs(
        GameObject stairsPrefab,
        Transform parent,
        bool[,] walls,
        int width,
        int depth,
        int x,
        int z,
        float yaw)
    {
        if (x < 0 || z < 0 || x >= width || z >= depth) return;
        if (walls[x, z]) return;
        if (!IsMezzanineCell(x, z, width, depth)) return;

        Place(stairsPrefab, parent, x * Tile, 0f, z * Tile, yaw);
    }

    private static void OptimizeHierarchy(GameObject geometry)
    {
        foreach (var renderer in geometry.GetComponentsInChildren<Renderer>(true))
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }

    private static GameObject Place(GameObject prefab, Transform parent, float x, float y, float z, float yaw)
    {
        var instance = Object.Instantiate(prefab, parent);
        instance.transform.SetParent(parent, false);
        instance.transform.localPosition = new Vector3(x, y, z);
        instance.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        return instance;
    }

    private static void PlaceDecorative(GameObject prefab, Transform parent, float x, float y, float z, float yaw)
    {
        StripColliders(Place(prefab, parent, x, y, z, yaw));
    }

    private static void StripColliders(GameObject root)
    {
        foreach (var collider in root.GetComponentsInChildren<Collider>(true))
        {
            collider.enabled = false;
            Object.Destroy(collider);
        }
    }

    private static GameObject LoadPrefab(string path) => RuntimePrefabLoader.Load(path);

    private static void MarkStaticRecursive(GameObject root)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            t.gameObject.isStatic = true;
    }
}
