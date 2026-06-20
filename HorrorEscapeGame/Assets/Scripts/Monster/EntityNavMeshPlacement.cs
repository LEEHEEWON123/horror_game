using UnityEngine;
using UnityEngine.AI;

public static class EntityNavMeshPlacement
{
    public static bool TryWarp(NavMeshAgent agent, Vector3 near, float maxDistance = 6f)
    {
        if (agent == null) return false;

        float originalHeight = agent.height;
        float originalRadius = agent.radius;

        float[] heightScales = { 1f, 0.85f, 0.72f };
        float[] radiusScales = { 1f, 0.85f, 0.72f };

        foreach (float heightScale in heightScales)
        {
            foreach (float radiusScale in radiusScales)
            {
                agent.height = originalHeight * heightScale;
                agent.radius = originalRadius * radiusScale;

                if (!NavMesh.SamplePosition(near, out NavMeshHit hit, maxDistance, NavMesh.AllAreas))
                    continue;

                agent.Warp(hit.position);
                if (!agent.isOnNavMesh)
                    continue;

                agent.height = originalHeight;
                agent.radius = originalRadius;

                if (agent.isOnNavMesh)
                    return true;

                agent.height = originalHeight * heightScale;
                agent.radius = originalRadius * radiusScale;
                return true;
            }
        }

        agent.height = originalHeight;
        agent.radius = originalRadius;
        return false;
    }

    public static bool TryWarpCandidates(NavMeshAgent agent, float maxDistance, params Vector3[] candidates)
    {
        if (agent == null || candidates == null) return false;

        foreach (var candidate in candidates)
        {
            if (TryWarp(agent, candidate, maxDistance))
                return true;
        }

        Debug.LogWarning($"[Entity] NavMesh not found near candidates starting at {candidates[0]}");
        return false;
    }
}
