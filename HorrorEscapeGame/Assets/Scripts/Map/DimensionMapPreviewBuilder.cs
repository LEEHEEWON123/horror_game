#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public static class DimensionMapPreviewBuilder
{
    public struct PreviewBuildResult
    {
        public GameObject Root;
        public Bounds Bounds;
    }

    public static PreviewBuildResult Build(string sceneName, Transform parent)
    {
        PortalTransitionState.UseGenerationSeed();

        PreviewBuildResult result = sceneName switch
        {
            "Map_01" => BuildMap01(parent),
            "Map_02" => BuildMap02(parent),
            "Map_03" => BuildMap03(parent),
            "Map_04" => BuildMap04(parent),
            "Map_05" => BuildMap05(parent),
            "Map_06" => BuildMap06(parent),
            "Map_07" => BuildMap07(parent),
            _ => default
        };

        MapGenerationContext.Clear();
        return result;
    }

    private static PreviewBuildResult BuildMap01(Transform parent)
    {
#if UNITY_EDITOR
        var prefab = BackroomsMapBuilder.LoadDefaultPrefab();
#else
        GameObject prefab = null;
#endif
        var map = BackroomsMapBuilder.Build(prefab);
        return Attach(parent, map.Root, map.Bounds);
    }

    private static PreviewBuildResult BuildMap02(Transform parent)
    {
        var map = ParkingGarageMapBuilder.Build();
        return Attach(parent, map.Root, map.Bounds);
    }

    private static PreviewBuildResult BuildMap03(Transform parent)
    {
        var map = SponzaMazeMapBuilder.Build();
        return Attach(parent, map.Root, map.Bounds);
    }

    private static PreviewBuildResult BuildMap04(Transform parent)
    {
        var map = PrototypeMapMapBuilder.Build();
        return Attach(parent, map.Root, map.Bounds);
    }

    private static PreviewBuildResult BuildMap05(Transform parent)
    {
        var map = NycCityMapBuilder.Build();
        return Attach(parent, map.Root, map.Bounds);
    }

    private static PreviewBuildResult BuildMap06(Transform parent)
    {
        var map = InteriorsAMapBuilder.Build();
        return Attach(parent, map.Root, map.Bounds);
    }

    private static PreviewBuildResult BuildMap07(Transform parent)
    {
        var map = BackroomsLikeMapBuilder.Build();
        return Attach(parent, map.Root, map.Bounds);
    }

    private static PreviewBuildResult Attach(Transform parent, GameObject root, Bounds bounds)
    {
        if (root == null)
            return default;

        root.transform.SetParent(parent, false);
        root.transform.localPosition = AlignNearEdge(bounds);
        root.transform.localRotation = Quaternion.identity;
        return new PreviewBuildResult { Root = root, Bounds = bounds };
    }

    private static Vector3 AlignNearEdge(Bounds bounds)
    {
        return new Vector3(-bounds.center.x, 0f, -bounds.min.z + 2f);
    }
}
