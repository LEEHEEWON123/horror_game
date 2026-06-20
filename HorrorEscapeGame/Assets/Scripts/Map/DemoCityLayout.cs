using UnityEngine;

internal struct DemoCityLayout
{
    // Main street / crosswalk cluster in the demo city (street level, not overpass).
    public static readonly Vector3 SpawnHint = new Vector3(227f, 0f, 6f);
    public static readonly Vector3 FallHint = new Vector3(245f, 0f, -58f);

    public Vector3 PlayerSpawn;
    public Vector3 FallTriggerCenter;
    public Vector3 FallTriggerSize;

    public static DemoCityLayout ForCity(Collider road, float streetBaseline)
    {
        return new DemoCityLayout
        {
            PlayerSpawn = ResolveWalkPoint(SpawnHint, road, streetBaseline),
            FallTriggerCenter = ResolveWalkPoint(FallHint, road, streetBaseline),
            FallTriggerSize = new Vector3(7f, 3.5f, 7f)
        };
    }

    private static Vector3 ResolveWalkPoint(Vector3 hint, Collider road, float streetBaseline)
    {
        float y = DemoCityRoadFloor.ResolveStreetBaseline(hint, road, streetBaseline);
        return new Vector3(hint.x, y, hint.z);
    }
}
