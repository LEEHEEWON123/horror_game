using UnityEngine;

public static class DemoCityRoadFloor
{
    private const float MinWalkNormal = 0.45f;
    private const float ProbeHeight = 45f;
    private const float ProbeDistance = 80f;
    private const float StreetMaxY = 9.5f;

    public static Collider BindRoadCollider(GameObject cityRoot)
    {
        if (cityRoot == null) return null;

        foreach (var collider in cityRoot.GetComponentsInChildren<Collider>(true))
        {
            if (collider.gameObject.name.Contains("collider", System.StringComparison.OrdinalIgnoreCase))
                return collider;
        }

        return cityRoot.GetComponentInChildren<Collider>();
    }

    public static float ResolveStreetBaseline(Vector3 worldXZ, Collider road, float fallbackY)
    {
        return QueryLowestRoadY(worldXZ, road, fallbackY, StreetMaxY);
    }

    public static float QueryFootingY(
        Vector3 worldPos,
        Vector3 moveDir,
        float currentY,
        Collider road,
        float streetBaseline,
        float maxStepUp = 0.45f,
        float maxStepDown = 2.5f)
    {
        float maxY = streetBaseline + 1.1f;
        float bestY = float.NegativeInfinity;

        if (TryProbe(worldPos, currentY, road, maxY, maxStepUp, maxStepDown, out float centerY))
            bestY = centerY;

        if (moveDir.sqrMagnitude > 0.01f)
        {
            var ahead = worldPos + moveDir.normalized * 0.45f;
            if (TryProbe(ahead, currentY, road, maxY, maxStepUp, maxStepDown, out float aheadY)
                && aheadY > bestY)
            {
                bestY = aheadY;
            }
        }

        if (bestY > float.NegativeInfinity)
            return bestY;

        return currentY;
    }

    private static bool TryProbe(
        Vector3 worldPos,
        float currentY,
        Collider road,
        float maxY,
        float maxStepUp,
        float maxStepDown,
        out float footingY)
    {
        footingY = currentY;
        if (road == null) return false;

        float y = QueryLowestRoadY(worldPos, road, currentY, maxY);
        if (y >= float.MaxValue) return false;

        float delta = y - currentY;
        if (delta > maxStepUp || delta < -maxStepDown) return false;

        footingY = y;
        return true;
    }

    private static float QueryLowestRoadY(Vector3 worldXZ, Collider road, float fallbackY, float maxY)
    {
        if (road == null) return fallbackY;

        var hits = Physics.RaycastAll(
            new Vector3(worldXZ.x, ProbeHeight, worldXZ.z),
            Vector3.down,
            ProbeDistance,
            ~0,
            QueryTriggerInteraction.Ignore);

        float bestY = float.MaxValue;
        foreach (var hit in hits)
        {
            if (hit.collider != road) continue;
            if (hit.normal.y < MinWalkNormal) continue;
            if (hit.point.y > maxY) continue;
            if (hit.point.y < bestY)
                bestY = hit.point.y;
        }

        return bestY < float.MaxValue ? bestY : fallbackY;
    }
}
