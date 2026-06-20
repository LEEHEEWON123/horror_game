#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Map_03 — Sponza stone/brick materials on a repeating 3m tile grid in one open hall with sparse interior walls.
/// </summary>
public static class SponzaMazeMapBuilder
{
    public const string SponzaFbxPath = "Assets/Sponza/Sponza.fbx";
    public const string FloorMatPath = "Assets/Sponza/Materials/floor_01.mat";
    public const string WallMatPath = "Assets/Sponza/Materials/brickwall_01.mat";
    public const string AltWallMatPath = "Assets/Sponza/Materials/brickwall_02.mat";
    public const string CeilingMatPath = "Assets/Sponza/Materials/ceiling_plaster_01.mat";

    private const float Tile = 3f;
    private const float RoomHeight = 3f;
    private const float SurfaceInset = 0.98f;
    private const float WallThickness = 0.32f;
    private const int HallTiles = 25;
    private const int ObstacleSpacing = 6;
    private const int SpawnClearTiles = 4;
    private const int ObstacleSeed = 303;
    private const int ExtraWallCount = 6;

    private readonly struct InteriorWallSegment
    {
        public readonly int StartX;
        public readonly int StartZ;
        public readonly int Length;
        public readonly bool Horizontal;

        public InteriorWallSegment(int startX, int startZ, int length, bool horizontal)
        {
            StartX = startX;
            StartZ = startZ;
            Length = length;
            Horizontal = horizontal;
        }
    }

    public struct BuildResult
    {
        public GameObject Root;
        public Bounds Bounds;
        public float WalkSurfaceY;
        public float RoomHeight;
        public float TileSize;
        public bool[,] MazeWalls;
        public Transform NavWalkRoot;
    }

    public static BuildResult Build(GameObject _ = null)
    {
        int seed = MapGenerationContext.ResolveSeed(ObstacleSeed);
        var interiorWalls = new List<InteriorWallSegment>();
        var walls = CreateOpenHallLayout(HallTiles, HallTiles, seed, interiorWalls);

        var floorMat = PrepareTileMaterial(LoadMaterial(FloorMatPath));
        var wallMat = PrepareTileMaterial(LoadMaterial(WallMatPath));
        var altWallMat = PrepareTileMaterial(LoadMaterial(AltWallMatPath));
        var ceilingMat = PrepareTileMaterial(LoadMaterial(CeilingMatPath));

        var root = new GameObject("Map_03_SponzaHall");
        var geometry = new GameObject("Geometry");
        geometry.transform.SetParent(root.transform, false);
        var navWalkways = new GameObject("NavWalkways");
        navWalkways.transform.SetParent(root.transform, false);

        var floorRoot = new GameObject("Floors");
        var ceilingRoot = new GameObject("Ceilings");
        var wallRoot = new GameObject("Walls");
        floorRoot.transform.SetParent(geometry.transform, false);
        ceilingRoot.transform.SetParent(geometry.transform, false);
        wallRoot.transform.SetParent(geometry.transform, false);

        float surfaceSize = Tile * SurfaceInset;

        for (int x = 0; x < HallTiles; x++)
        {
            for (int z = 0; z < HallTiles; z++)
            {
                float px = x * Tile;
                float pz = z * Tile;

                CreateSurfaceQuad(floorRoot.transform, px, 0.005f, pz, floorMat, faceUp: true, surfaceSize);
                CreateSurfaceQuad(ceilingRoot.transform, px, RoomHeight - 0.005f, pz, ceilingMat, faceUp: false, surfaceSize);

                if (!walls[x, z])
                {
                    CreateNavFloor(navWalkways.transform, px, pz);
                    continue;
                }

                if (IsPerimeter(x, z, HallTiles, HallTiles))
                    CreatePerimeterWall(wallRoot.transform, px, pz, wallMat);
            }
        }

        foreach (var segment in interiorWalls)
            CreateInteriorWall(wallRoot.transform, segment, altWallMat);

        var geometryModifier = geometry.AddComponent<NavMeshModifier>();
        geometryModifier.ignoreFromBuild = true;
        geometryModifier.applyToChildren = true;

        MaterialURPFixer.FixHierarchy(geometry);
        MarkStaticRecursive(geometry);
        MarkStaticRecursive(navWalkways);
        Physics.SyncTransforms();

        float centerX = HallTiles * Tile * 0.5f - Tile * 0.5f;
        float centerZ = HallTiles * Tile * 0.5f - Tile * 0.5f;
        float walkY = SpawnHelper.QueryFloorY(new Vector3(centerX, 0f, centerZ), MapFloor.DefaultWalkY);
        MapFloor.SetWalkY(walkY);
        MapFloor.Calibrate(new Vector3(centerX, 0f, centerZ));

        var bounds = new Bounds(
            new Vector3(centerX, RoomHeight * 0.5f, centerZ),
            new Vector3(HallTiles * Tile, RoomHeight, HallTiles * Tile));

        Debug.Log($"[SponzaMazeMapBuilder] open sponza hall {HallTiles}x{HallTiles}, interior walls {interiorWalls.Count}");

        return new BuildResult
        {
            Root = root,
            Bounds = bounds,
            WalkSurfaceY = walkY,
            RoomHeight = RoomHeight,
            TileSize = Tile,
            MazeWalls = walls,
            NavWalkRoot = navWalkways.transform,
        };
    }

    public static GameObject LoadDefaultPrefab()
    {
#if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<GameObject>(SponzaFbxPath);
#else
        var registry = HorrorAssetRegistry.Instance;
        return registry != null ? registry.sponzaPrefab : null;
#endif
    }

    private static bool[,] CreateOpenHallLayout(
        int width,
        int depth,
        int seed,
        List<InteriorWallSegment> interiorWalls)
    {
        var walls = new bool[width, depth];
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
                walls[x, z] = IsPerimeter(x, z, width, depth);
        }

        AddSparseWalls(walls, width, depth, seed, interiorWalls);
        return walls;
    }

    private static void AddSparseWalls(
        bool[,] walls,
        int width,
        int depth,
        int seed,
        List<InteriorWallSegment> interiorWalls)
    {
        var rng = new System.Random(seed);
        int spawnX = width / 2;
        int spawnZ = 3;
        int capsuleX = width - 4;
        int capsuleZ = depth - 4;

        for (int x = 3; x < width - 3; x += ObstacleSpacing)
        {
            for (int z = 3; z < depth - 3; z += ObstacleSpacing)
            {
                if (IsNearSpawnZone(x, z, spawnX, spawnZ, capsuleX, capsuleZ))
                    continue;

                if (rng.NextDouble() > 0.4)
                    continue;

                bool horizontal = rng.Next(2) == 0;
                int length = rng.Next(3, 6);
                TryPlaceWallSegment(
                    walls, interiorWalls, width, depth, x, z, horizontal, length,
                    spawnX, spawnZ, capsuleX, capsuleZ);
            }
        }

        for (int i = 0; i < ExtraWallCount; i++)
        {
            bool horizontal = rng.Next(2) == 0;
            int length = rng.Next(3, 6);
            int anchorX = rng.Next(3, width - 3 - (horizontal ? length : 1));
            int anchorZ = rng.Next(3, depth - 3 - (horizontal ? 1 : length));

            TryPlaceWallSegment(
                walls, interiorWalls, width, depth, anchorX, anchorZ, horizontal, length,
                spawnX, spawnZ, capsuleX, capsuleZ);
        }
    }

    private static void TryPlaceWallSegment(
        bool[,] walls,
        List<InteriorWallSegment> interiorWalls,
        int width,
        int depth,
        int anchorX,
        int anchorZ,
        bool horizontal,
        int length,
        int spawnX,
        int spawnZ,
        int capsuleX,
        int capsuleZ)
    {
        var cells = new (int x, int z)[length];
        for (int i = 0; i < length; i++)
        {
            int x = horizontal ? anchorX + i : anchorX;
            int z = horizontal ? anchorZ : anchorZ + i;

            if (IsPerimeter(x, z, width, depth) || walls[x, z])
                return;

            if (IsNearSpawnZone(x, z, spawnX, spawnZ, capsuleX, capsuleZ))
                return;

            cells[i] = (x, z);
        }

        foreach (var (x, z) in cells)
            walls[x, z] = true;

        interiorWalls.Add(new InteriorWallSegment(anchorX, anchorZ, length, horizontal));
    }

    private static bool IsNearSpawnZone(
        int x,
        int z,
        int spawnX,
        int spawnZ,
        int capsuleX,
        int capsuleZ)
    {
        if (Mathf.Abs(x - spawnX) <= SpawnClearTiles && z <= spawnZ + SpawnClearTiles)
            return true;

        if (Mathf.Abs(x - capsuleX) <= SpawnClearTiles && Mathf.Abs(z - capsuleZ) <= SpawnClearTiles)
            return true;

        return false;
    }

    private static Material PrepareTileMaterial(Material source)
    {
        if (source == null) return null;

        var mat = MaterialURPFixer.ConvertToUrP(source);
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", mat.GetColor("_BaseColor") * 0.9f);

        if (mat.HasProperty("_BaseMap"))
            mat.SetTextureScale("_BaseMap", Vector2.one);

        return mat;
    }

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
        if (renderer != null && mat != null)
        {
            renderer.sharedMaterial = mat;
            ApplyStaticRendererSettings(renderer);
        }
    }

    private static void CreatePerimeterWall(Transform parent, float x, float z, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "PerimeterWall";
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(x, RoomHeight * 0.5f, z);
        go.transform.localScale = new Vector3(Tile * SurfaceInset, RoomHeight, Tile * SurfaceInset);

        ApplyWallMaterial(go, mat);
    }

    private static void CreateInteriorWall(Transform parent, InteriorWallSegment segment, Material mat)
    {
        float lengthWorld = segment.Length * Tile * SurfaceInset;
        float centerX;
        float centerZ;
        float sizeX;
        float sizeZ;

        if (segment.Horizontal)
        {
            centerX = (segment.StartX + (segment.Length - 1) * 0.5f) * Tile;
            centerZ = segment.StartZ * Tile;
            sizeX = lengthWorld;
            sizeZ = WallThickness;
        }
        else
        {
            centerX = segment.StartX * Tile;
            centerZ = (segment.StartZ + (segment.Length - 1) * 0.5f) * Tile;
            sizeX = WallThickness;
            sizeZ = lengthWorld;
        }

        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = segment.Horizontal ? "InteriorWall_H" : "InteriorWall_V";
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(centerX, RoomHeight * 0.5f, centerZ);
        go.transform.localScale = new Vector3(sizeX, RoomHeight, sizeZ);

        ApplyWallMaterial(go, mat);
    }

    private static void ApplyWallMaterial(GameObject go, Material mat)
    {
        var renderer = go.GetComponent<Renderer>();
        if (renderer != null && mat != null)
        {
            renderer.sharedMaterial = mat;
            ApplyStaticRendererSettings(renderer);
        }
    }

    private static void ApplyStaticRendererSettings(Renderer renderer)
    {
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        renderer.lightProbeUsage = LightProbeUsage.Off;
    }

    private static void CreateNavFloor(Transform parent, float x, float z)
    {
        var go = new GameObject($"NavFloor_{x}_{z}");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(x, -0.02f, z);

        var box = go.AddComponent<BoxCollider>();
        box.size = new Vector3(Tile * SurfaceInset, 0.1f, Tile * SurfaceInset);
        box.center = new Vector3(0f, 0.05f, 0f);
        go.isStatic = true;
    }

    private static bool IsPerimeter(int x, int z, int width, int depth) =>
        x == 0 || z == 0 || x == width - 1 || z == depth - 1;

    private static Material LoadMaterial(string path)
    {
#if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<Material>(path);
#else
        var registry = HorrorAssetRegistry.Instance;
        if (registry == null) return null;

        return path switch
        {
            FloorMatPath => registry.sponzaFloorMaterial,
            WallMatPath => registry.sponzaWallMaterial,
            AltWallMatPath => registry.sponzaAltWallMaterial,
            CeilingMatPath => registry.sponzaCeilingMaterial,
            _ => null,
        };
#endif
    }

    private static void MarkStaticRecursive(GameObject root)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            t.gameObject.isStatic = true;
    }
}
