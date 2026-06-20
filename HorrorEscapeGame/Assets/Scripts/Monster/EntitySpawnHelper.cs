using UnityEngine;

public static class EntitySpawnHelper
{
    public const float MinDistanceFromPlayer = 18f;

    public static Vector3 ResolveSpawnPosition(Vector3 preferred, Vector3 playerPosition, Vector3[] alternates)
    {
        preferred.y = playerPosition.y;

        if (HorizontalDistance(preferred, playerPosition) >= MinDistanceFromPlayer)
            return preferred;

        Vector3 best = preferred;
        float bestDistance = HorizontalDistance(preferred, playerPosition);

        if (alternates != null)
        {
            foreach (var alternate in alternates)
            {
                var candidate = alternate;
                candidate.y = preferred.y;
                float distance = HorizontalDistance(candidate, playerPosition);
                if (distance >= MinDistanceFromPlayer)
                    return candidate;

                if (distance > bestDistance)
                {
                    bestDistance = distance;
                    best = candidate;
                }
            }
        }

        if (bestDistance >= MinDistanceFromPlayer)
            return best;

        Vector3 away = best - playerPosition;
        away.y = 0f;
        if (away.sqrMagnitude < 0.01f)
            away = Vector3.forward;

        away.Normalize();
        var pushed = playerPosition + away * MinDistanceFromPlayer;
        pushed.y = preferred.y;
        return pushed;
    }

    private static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
