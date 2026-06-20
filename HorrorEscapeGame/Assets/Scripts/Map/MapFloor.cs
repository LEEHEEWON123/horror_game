using UnityEngine;

public static class MapFloor
{
    public const float DefaultWalkY = 0f;

    public static float WalkY { get; private set; } = DefaultWalkY;

    public static void SetWalkY(float y)
    {
        WalkY = y;
    }

    public static void Calibrate(Vector3 sampleXZ)
    {
        float y = SpawnHelper.QueryFloorY(sampleXZ, DefaultWalkY);
        if (y > 1.5f)
        {
            Debug.LogWarning($"[MapFloor] Suspicious floor Y {y:F3} — using default {DefaultWalkY}");
            y = DefaultWalkY;
        }

        WalkY = y;
        Debug.Log($"[MapFloor] Walk surface Y = {WalkY:F3}");
    }

    public static Vector3 PlaceOnFloor(Vector3 worldPos, float footOffset = 0f)
    {
        return new Vector3(worldPos.x, WalkY + footOffset, worldPos.z);
    }
}
