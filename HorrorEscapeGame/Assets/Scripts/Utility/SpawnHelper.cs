using UnityEngine;

public static class SpawnHelper
{
    private const float IndoorProbeHeight = 2.2f;
    private const float IndoorProbeDistance = 3f;
    private const float CeilingCutoffY = 2.5f;

    public static float QueryFloorY(Vector3 worldXZ, float fallbackY = 0f)
    {
        var indoorOrigin = new Vector3(worldXZ.x, IndoorProbeHeight, worldXZ.z);
        if (Physics.Raycast(indoorOrigin, Vector3.down, out var indoorHit, IndoorProbeDistance, ~0,
                QueryTriggerInteraction.Ignore))
            return indoorHit.point.y;

        var hits = Physics.RaycastAll(new Vector3(worldXZ.x, 25f, worldXZ.z), Vector3.down, 50f, ~0,
            QueryTriggerInteraction.Ignore);
        float bestY = float.MinValue;
        foreach (var hit in hits)
        {
            if (hit.point.y >= CeilingCutoffY) continue;
            if (hit.point.y > bestY)
                bestY = hit.point.y;
        }

        return bestY > float.MinValue ? bestY : fallbackY;
    }

    /// <summary>Outdoor / elevated maps — lowest upward-facing walk surface in the column.</summary>
    public static float QueryOutdoorFloorY(Vector3 worldXZ, float fallbackY = 0f, float minY = float.NegativeInfinity)
    {
        const float probeHeight = 80f;
        const float probeDistance = 120f;
        const float minUpNormal = 0.4f;

        var hits = Physics.RaycastAll(
            new Vector3(worldXZ.x, probeHeight, worldXZ.z),
            Vector3.down,
            probeDistance,
            ~0,
            QueryTriggerInteraction.Ignore);

        float bestY = float.MaxValue;
        foreach (var hit in hits)
        {
            if (!IsWalkSurfaceHit(hit, minUpNormal, minY)) continue;
            if (hit.point.y < bestY)
                bestY = hit.point.y;
        }

        return bestY < float.MaxValue ? bestY : fallbackY;
    }

    /// <summary>Raycast near the bottom of the map bounds — avoids hitting rooftops far above.</summary>
    public static float QueryBoundedFloorY(Vector3 worldXZ, Bounds bounds, float fallbackY, float minY)
    {
        const float minUpNormal = 0.4f;
        float probeBottom = bounds.min.y - 0.25f;
        float startY = bounds.max.y - Mathf.Clamp(bounds.size.y * 0.08f, 0.75f, 2.5f);
        float distance = Mathf.Min(Mathf.Max(startY - probeBottom, 1f), 24f);

        var hits = Physics.RaycastAll(
            new Vector3(worldXZ.x, startY, worldXZ.z),
            Vector3.down,
            distance,
            ~0,
            QueryTriggerInteraction.Ignore);

        float bestY = float.MaxValue;
        foreach (var hit in hits)
        {
            if (!IsWalkSurfaceHit(hit, minUpNormal, minY)) continue;
            if (hit.point.y < bestY)
                bestY = hit.point.y;
        }

        return bestY < float.MaxValue ? bestY : fallbackY;
    }

    /// <summary>Sample map grid; pick the most common floor height (main walk plane).</summary>
    public static float QueryPrototypeGroundY(Bounds bounds, float minY)
    {
        const int steps = 9;
        float margin = Mathf.Min(bounds.size.x, bounds.size.z) * 0.06f;
        var bucketCounts = new System.Collections.Generic.Dictionary<int, int>();
        var bucketHeights = new System.Collections.Generic.Dictionary<int, float>();

        for (int ix = 0; ix < steps; ix++)
        {
            float tX = steps == 1 ? 0.5f : ix / (float)(steps - 1);
            for (int iz = 0; iz < steps; iz++)
            {
                float tZ = steps == 1 ? 0.5f : iz / (float)(steps - 1);
                float x = Mathf.Lerp(bounds.min.x + margin, bounds.max.x - margin, tX);
                float z = Mathf.Lerp(bounds.min.z + margin, bounds.max.z - margin, tZ);
                float y = QueryBoundedFloorY(new Vector3(x, 0f, z), bounds, float.PositiveInfinity, minY);
                if (y >= float.PositiveInfinity)
                    continue;

                int bucket = Mathf.RoundToInt(y * 4f);
                bucketCounts.TryGetValue(bucket, out int count);
                bucketCounts[bucket] = count + 1;
                bucketHeights[bucket] = bucket / 4f;
            }
        }

        if (bucketCounts.Count == 0)
            return bounds.min.y;

        int bestBucket = -1;
        int bestCount = 0;
        foreach (var pair in bucketCounts)
        {
            if (pair.Value > bestCount)
            {
                bestCount = pair.Value;
                bestBucket = pair.Key;
            }
        }

        return bucketHeights[bestBucket];
    }

    /// <summary>Local floor snap capped to ground baseline so roof columns do not lift the player.</summary>
    public static float SnapOutdoorFloorY(Vector3 worldXZ, Bounds bounds, float groundBaseline, float minY, float maxAboveGround = 0.35f)
    {
        float localY = QueryBoundedFloorY(worldXZ, bounds, groundBaseline, minY);
        float cap = groundBaseline + maxAboveGround;
        return localY > cap ? groundBaseline : localY;
    }

    private static bool IsWalkSurfaceHit(RaycastHit hit, float minUpNormal, float minY)
    {
        if (hit.normal.y < minUpNormal) return false;
        if (hit.point.y < minY) return false;
        if (hit.collider != null && hit.collider.gameObject.name == "NavMeshGround") return false;
        return true;
    }

    public static bool TryQueryFootingY(
        Vector3 worldPos,
        Vector3 moveDir,
        float currentY,
        float fallbackY,
        out float footingY,
        float maxStepUp = 0.55f,
        float maxStepDown = 3f)
    {
        const float minWalkNormal = 0.35f;
        float bestY = float.NegativeInfinity;

        if (TryProbeFooting(worldPos, currentY, minWalkNormal, maxStepUp, maxStepDown, out float centerY))
            bestY = centerY;

        if (moveDir.sqrMagnitude > 0.01f)
        {
            var ahead = worldPos + moveDir.normalized * 0.65f;
            if (TryProbeFooting(ahead, currentY, minWalkNormal, maxStepUp, maxStepDown, out float aheadY)
                && aheadY > bestY)
            {
                bestY = aheadY;
            }
        }

        if (bestY > float.NegativeInfinity)
        {
            footingY = bestY;
            return true;
        }

        footingY = fallbackY;
        return false;
    }

    private static bool TryProbeFooting(
        Vector3 worldPos,
        float currentY,
        float minWalkNormal,
        float maxStepUp,
        float maxStepDown,
        out float footingY)
    {
        const float probeUp = 1.25f;
        const float probeDown = 2.75f;

        var origin = new Vector3(worldPos.x, currentY + probeUp, worldPos.z);
        var hits = Physics.RaycastAll(origin, Vector3.down, probeUp + probeDown, ~0,
            QueryTriggerInteraction.Ignore);

        float bestY = float.NegativeInfinity;

        foreach (var hit in hits)
        {
            if (!IsWalkSurfaceHit(hit, minWalkNormal, float.NegativeInfinity))
                continue;

            float delta = hit.point.y - currentY;
            if (delta > maxStepUp || delta < -maxStepDown)
                continue;

            if (hit.point.y > bestY)
                bestY = hit.point.y;
        }

        if (bestY > float.NegativeInfinity)
        {
            footingY = bestY;
            return true;
        }

        footingY = currentY;
        return false;
    }

    public static Vector3 SnapToGround(Vector3 position, float footOffset = 0.02f)
    {
        float y = QueryFloorY(position, position.y);
        return new Vector3(position.x, y + footOffset, position.z);
    }
}
