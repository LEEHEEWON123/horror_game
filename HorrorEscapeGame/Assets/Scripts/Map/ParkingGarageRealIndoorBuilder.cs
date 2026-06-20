#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public static class ParkingGarageRealIndoorBuilder
{
    public const float Tile = 3f;
    public const float RoomHeight = 3.2f;
    public const int FloorCount = 2;
    public const int WidthTiles = 14;
    public const int DepthTiles = 10;

    private const string MatRoot = "Assets/Parkgarage/Prefabs/parkgarage/Materials";
    private const string LampPrefabPath = "Assets/Parkgarage/Prefabs/lamps/Lamp_parkgarage_prefab.prefab";

    public struct BuildResult
    {
        public GameObject Root;
        public Bounds Bounds;
        public float WalkSurfaceY;
        public bool[,] MazeWalls;
        public Vector3 StairsUpTriggerCenter;
        public Vector3 StairsDownTriggerCenter;
        public Vector3 StairsUpDestination;
        public Vector3 StairsDownDestination;
        public float UpperWalkY;
    }

    public static BuildResult Build()
    {
        var root = new GameObject("Map_03_ParkingGarage");
        var geometry = new GameObject("Geometry");
        geometry.transform.SetParent(root.transform);

        var floorMat = LoadMaterial($"{MatRoot}/Parkgarage_floor.mat");
        var pillarMat = LoadMaterial($"{MatRoot}/Big_poles.mat");
        var ceilingMat = LoadMaterial($"{MatRoot}/Floor_up_wall.mat");
        var wallMat = LoadMaterial($"{MatRoot}/Parkgarage_outside_A.mat");
        var lampPrefab = LoadPrefab(LampPrefabPath);

        for (int floor = 0; floor < FloorCount; floor++)
            BuildFloorLevel(geometry.transform, floor, floorMat, pillarMat, ceilingMat, wallMat, lampPrefab);

        BuildStairwell(root.transform);

        MarkStaticRecursive(geometry);
        Physics.SyncTransforms();

        float centerX = WidthTiles * Tile * 0.5f - Tile * 0.5f;
        float centerZ = DepthTiles * Tile * 0.5f - Tile * 0.5f;
        float walkY = SpawnHelper.QueryFloorY(new Vector3(centerX, 0f, Tile), MapFloor.DefaultWalkY);

        var bounds = new Bounds(
            new Vector3(centerX, RoomHeight * FloorCount * 0.5f, centerZ),
            new Vector3(WidthTiles * Tile, RoomHeight * FloorCount, DepthTiles * Tile));

        float upperWalkY = RoomHeight;
        var stairsUpDest = new Vector3(Tile * 2f, upperWalkY, Tile * 2f);
        var stairsDownDest = new Vector3(Tile * 2f, walkY, Tile * 2f);

        return new BuildResult
        {
            Root = root,
            Bounds = bounds,
            WalkSurfaceY = walkY,
            MazeWalls = CreateOpenLayout(),
            StairsUpTriggerCenter = new Vector3(Tile * 2f, walkY + 0.5f, Tile * 2f),
            StairsDownTriggerCenter = new Vector3(Tile * 2f, upperWalkY + 0.5f, Tile * 2f),
            StairsUpDestination = stairsUpDest,
            StairsDownDestination = stairsDownDest,
            UpperWalkY = upperWalkY
        };
    }

    private static void BuildFloorLevel(
        Transform parent,
        int floorIndex,
        Material floorMat,
        Material pillarMat,
        Material ceilingMat,
        Material wallMat,
        GameObject lampPrefab)
    {
        float baseY = floorIndex * RoomHeight;
        var floorRoot = new GameObject($"Floor_{floorIndex + 1}");
        floorRoot.transform.SetParent(parent, false);
        floorRoot.transform.localPosition = new Vector3(0f, baseY, 0f);

        BuildSlab(floorRoot.transform, "FloorSlab", -0.08f, new Vector3(WidthTiles * Tile, 0.16f, DepthTiles * Tile), floorMat);
        BuildSlab(floorRoot.transform, "Ceiling", RoomHeight - 0.02f, new Vector3(WidthTiles * Tile, 0.14f, DepthTiles * Tile), ceilingMat);
        BuildPerimeterWalls(floorRoot.transform, wallMat);
        BuildPillarGrid(floorRoot.transform, pillarMat);
        BuildParkingMarkings(floorRoot.transform);
        BuildHazardBarriers(floorRoot.transform);
        BuildCeilingLamps(floorRoot.transform, lampPrefab);
    }

    private static void BuildSlab(Transform parent, string name, float y, Vector3 size, Material mat)
    {
        var slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
        slab.name = name;
        slab.transform.SetParent(parent, false);
        slab.transform.localPosition = new Vector3(size.x * 0.5f - Tile * 0.5f, y, size.z * 0.5f - Tile * 0.5f);
        slab.transform.localScale = size;
        if (mat != null)
            slab.GetComponent<Renderer>().sharedMaterial = mat;
    }

    private static void BuildPerimeterWalls(Transform parent, Material mat)
    {
        float w = WidthTiles * Tile;
        float d = DepthTiles * Tile;
        float h = RoomHeight;
        float cx = w * 0.5f - Tile * 0.5f;
        float cz = d * 0.5f - Tile * 0.5f;
        const float thickness = 0.35f;

        CreateWall(parent, "Wall_N", new Vector3(cx, h * 0.5f, -thickness * 0.5f), new Vector3(w, h, thickness), mat);
        CreateWall(parent, "Wall_S", new Vector3(cx, h * 0.5f, d - thickness * 0.5f), new Vector3(w, h, thickness), mat);
        CreateWall(parent, "Wall_W", new Vector3(-thickness * 0.5f, h * 0.5f, cz), new Vector3(thickness, h, d), mat);
        CreateWall(parent, "Wall_E", new Vector3(w - thickness * 0.5f, h * 0.5f, cz), new Vector3(thickness, h, d), mat);
    }

    private static void CreateWall(Transform parent, string name, Vector3 localPos, Vector3 scale, Material mat)
    {
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.SetParent(parent, false);
        wall.transform.localPosition = localPos;
        wall.transform.localScale = scale;
        if (mat != null)
            wall.GetComponent<Renderer>().sharedMaterial = mat;
    }

    private static void BuildPillarGrid(Transform parent, Material mat)
    {
        var pillarRoot = new GameObject("Pillars");
        pillarRoot.transform.SetParent(parent, false);

        for (int x = 2; x < WidthTiles - 1; x += 2)
        {
            for (int z = 1; z < DepthTiles - 1; z += 2)
            {
                if (x == 2 && z <= 2) continue;

                CreateTile(pillarRoot.transform, x * Tile, RoomHeight * 0.5f, z * Tile,
                    new Vector3(0.5f, RoomHeight, 0.5f), mat);
            }
        }
    }

    private static void BuildParkingMarkings(Transform parent)
    {
        var marks = new GameObject("ParkingLines");
        marks.transform.SetParent(parent, false);
        var lineMat = CreateLineMaterial(new Color(0.75f, 0.75f, 0.72f, 0.85f));

        for (int z = 1; z < DepthTiles - 1; z++)
        {
            CreateTile(marks.transform, (WidthTiles * Tile) * 0.5f - Tile * 0.5f, 0.02f, z * Tile,
                new Vector3(WidthTiles * Tile - Tile * 2f, 0.02f, 0.06f), lineMat);
        }
    }

    private static void BuildHazardBarriers(Transform parent)
    {
        var barriers = new GameObject("HazardBarriers");
        barriers.transform.SetParent(parent, false);

        for (int z = 1; z < DepthTiles - 1; z++)
        {
            bool yellow = z % 2 == 0;
            var mat = CreateLineMaterial(yellow ? new Color(0.95f, 0.78f, 0.08f) : new Color(0.08f, 0.08f, 0.08f));
            CreateTile(barriers.transform, Tile * 0.65f, 0.35f, z * Tile,
                new Vector3(0.35f, 0.45f, 1.4f), mat);
        }
    }

    private static void BuildCeilingLamps(Transform parent, GameObject lampPrefab)
    {
        if (lampPrefab == null) return;

        var lampRoot = new GameObject("LampMeshes");
        lampRoot.transform.SetParent(parent, false);

        for (int x = 3; x < WidthTiles - 2; x += 4)
        {
            for (int z = 3; z < DepthTiles - 2; z += 4)
            {
                var lamp = Object.Instantiate(lampPrefab, lampRoot.transform);
                lamp.transform.localPosition = new Vector3(x * Tile, RoomHeight - 0.2f, z * Tile);
                lamp.transform.localRotation = Quaternion.identity;

                foreach (var light in lamp.GetComponentsInChildren<Light>(true))
                {
                    light.intensity = 0.12f;
                    light.range = 6f;
                }
            }
        }
    }

    private static void BuildStairwell(Transform root)
    {
        var stairsRoot = new GameObject("Stairwell");
        stairsRoot.transform.SetParent(root.transform, false);

        float rampWidth = Tile * 1.2f;
        float rampLength = RoomHeight * 1.4f;
        var ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ramp.name = "StairRampVisual";
        ramp.transform.SetParent(stairsRoot.transform, false);
        ramp.transform.position = new Vector3(Tile * 2f, RoomHeight * 0.5f, Tile * 1.2f);
        ramp.transform.localScale = new Vector3(rampWidth, 0.12f, rampLength);
        ramp.transform.rotation = Quaternion.Euler(-32f, 0f, 0f);

        CreateTransitionZone(
            stairsRoot.transform,
            "StairsUp",
            new Vector3(Tile * 2f, 0.4f, Tile * 1.5f),
            new Vector3(rampWidth * 0.8f, 1.2f, 1.4f),
            RoomHeight,
            new Vector3(Tile * 2f, RoomHeight, Tile * 2.5f));

        CreateTransitionZone(
            stairsRoot.transform,
            "StairsDown",
            new Vector3(Tile * 2f, RoomHeight + 0.4f, Tile * 2.5f),
            new Vector3(rampWidth * 0.8f, 1.2f, 1.4f),
            0f,
            new Vector3(Tile * 2f, 0f, Tile * 1.5f));
    }

    private static FloorTransitionZone CreateTransitionZone(
        Transform parent,
        string name,
        Vector3 center,
        Vector3 size,
        float targetWalkY,
        Vector3 destination)
    {
        var zoneGo = new GameObject(name);
        zoneGo.transform.SetParent(parent, false);
        zoneGo.transform.position = center;

        var col = zoneGo.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = size;

        var zone = zoneGo.AddComponent<FloorTransitionZone>();
        zone.Configure(targetWalkY, destination);
        return zone;
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

    private static Material CreateLineMaterial(Color color)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) return null;

        var mat = new Material(shader);
        mat.SetColor("_BaseColor", color);
        mat.SetFloat("_Smoothness", 0.15f);
        return mat;
    }

    private static bool[,] CreateOpenLayout()
    {
        var walls = new bool[WidthTiles, DepthTiles];
        for (int x = 2; x < WidthTiles - 2; x += 2)
        {
            for (int z = 2; z < DepthTiles - 2; z += 2)
            {
                if (x == 2 && z <= 2) continue;
                walls[x, z] = true;
            }
        }

        return walls;
    }

    private static Material LoadMaterial(string path)
    {
#if UNITY_EDITOR
        var source = AssetDatabase.LoadAssetAtPath<Material>(path);
        return source != null ? MaterialURPFixer.ConvertToUrP(source) : CreateFallbackMaterial(new Color(0.18f, 0.19f, 0.2f));
#else
        return null;
#endif
    }

    private static Material CreateFallbackMaterial(Color color)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) return null;

        var mat = new Material(shader);
        mat.SetColor("_BaseColor", color);
        mat.SetFloat("_Smoothness", 0.12f);
        return mat;
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
