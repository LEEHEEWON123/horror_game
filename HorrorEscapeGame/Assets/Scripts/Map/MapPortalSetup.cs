using UnityEngine;
using UnityEngine.SceneManagement;

public static class MapPortalSetup
{
    private const float MinPortalSeparation = 6f;

    public static void SpawnForActiveMap(
        Vector3 dimensionPortalPos,
        Vector3 separationHint,
        bool[,] mazeWalls = null,
        float tileSize = 3f,
        bool snapToFloor = true,
        float footOffset = 0f)
    {
        DimensionPortalSpawner.Spawn(
            dimensionPortalPos,
            snapToFloor,
            footOffset,
            PortalTransitionKind.Dimension);

        if (!ShouldSpawnRealityPortal())
            return;

        Vector3 realityPos = ResolveRealityPortalPosition(
            dimensionPortalPos,
            separationHint,
            mazeWalls,
            tileSize);

        DimensionPortalSpawner.Spawn(
            realityPos,
            snapToFloor,
            footOffset,
            PortalTransitionKind.Return);
    }

    private static bool ShouldSpawnRealityPortal()
    {
        var gm = GameManager.Instance;
        if (gm == null) return false;

        return gm.IsReturnMap(SceneManager.GetActiveScene().name);
    }

    private static Vector3 ResolveRealityPortalPosition(
        Vector3 dimensionPortalPos,
        Vector3 separationHint,
        bool[,] mazeWalls,
        float tileSize)
    {
        float y = MapFloor.WalkY;

        if (mazeWalls != null)
        {
            return MazeGenerator.FindFarthestPassage(
                dimensionPortalPos,
                mazeWalls,
                tileSize,
                y,
                MinPortalSeparation);
        }

        Vector3 flatHint = Flat(separationHint);
        if (FlatDistance(flatHint, Flat(dimensionPortalPos)) >= MinPortalSeparation)
            return MapFloor.PlaceOnFloor(separationHint);

        Vector3 offset = Flat(dimensionPortalPos - separationHint);
        if (offset.sqrMagnitude < 0.01f)
            offset = Vector3.left;

        offset.Normalize();
        return MapFloor.PlaceOnFloor(dimensionPortalPos + offset * MinPortalSeparation);
    }

    private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

    private static float FlatDistance(Vector3 a, Vector3 b) => Vector3.Distance(a, b);
}
