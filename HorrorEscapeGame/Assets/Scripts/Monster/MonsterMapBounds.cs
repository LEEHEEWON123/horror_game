using UnityEngine;
using UnityEngine.AI;

[DefaultExecutionOrder(110)]
[RequireComponent(typeof(NavMeshAgent))]
public class MonsterMapBounds : MonoBehaviour
{
    private Bounds _bounds;
    private NavMeshAgent _agent;

    public void Configure(Bounds bounds, float inset = 1.25f)
    {
        _bounds = MapBoundaryBuilder.ClampBounds(bounds, inset);
        _agent = GetComponent<NavMeshAgent>();
    }

    private void LateUpdate()
    {
        if (_bounds.size.x <= 0f || _bounds.size.z <= 0f)
            return;

        var pos = transform.position;
        float clampedX = Mathf.Clamp(pos.x, _bounds.min.x, _bounds.max.x);
        float clampedZ = Mathf.Clamp(pos.z, _bounds.min.z, _bounds.max.z);

        if (Mathf.Approximately(clampedX, pos.x) && Mathf.Approximately(clampedZ, pos.z))
            return;

        pos.x = clampedX;
        pos.z = clampedZ;
        transform.position = pos;

        if (_agent != null && _agent.enabled)
            _agent.Warp(pos);
    }
}
