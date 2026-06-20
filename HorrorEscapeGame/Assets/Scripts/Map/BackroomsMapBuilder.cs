#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public static class BackroomsMapBuilder
{
    public const string TstLevelAssetPath =
        "Assets/LoafbrrAssets/BackroomsLikeAssetRe/prefab/Level/TstLevel.prefab";

    public struct MapBuildResult
    {
        public GameObject Root;
        public Bounds Bounds;
        public bool[,] MazeWalls;
    }

    public static MapBuildResult Build(GameObject levelPrefab)
    {
        var indoor = BackroomsIndoorBuilder.Build();
        MapFloor.Calibrate(new Vector3(indoor.Bounds.center.x, 0f, indoor.Bounds.min.z + Tile * 0.5f));
        BackroomsAtmosphere.Apply(indoor.Bounds);

        return new MapBuildResult
        {
            Root = indoor.Root,
            Bounds = indoor.Bounds,
            MazeWalls = indoor.MazeWalls
        };
    }

    private const float Tile = 3f;

    public static GameObject LoadDefaultPrefab()
    {
#if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<GameObject>(TstLevelAssetPath);
#else
        return null;
#endif
    }
}
