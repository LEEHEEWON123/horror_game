using UnityEngine;

public static class ParkingGarageMapBuilder
{
    public const int MapSeed = 8402;

    public struct MapBuildResult
    {
        public GameObject Root;
        public Bounds Bounds;
        public bool[,] MazeWalls;
    }

    public static MapBuildResult Build(int? mazeSeed = null)
    {
        int seed = mazeSeed ?? MapGenerationContext.ResolveSeed(MapSeed);
        var indoor = ParkingGarageIndoorBuilder.Build(seed);
        MapFloor.Calibrate(new Vector3(indoor.Bounds.center.x, 0f, indoor.Bounds.min.z + 3f));
        BackroomsAtmosphere.Apply(indoor.Bounds);
        MaterialURPFixer.FixHierarchy(indoor.Root);

        return new MapBuildResult
        {
            Root = indoor.Root,
            Bounds = indoor.Bounds,
            MazeWalls = indoor.MazeWalls
        };
    }
}
