using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class MonsterGroundFollow : MonoBehaviour
{
    private NavMeshAgent _agent;
    private float _fallbackY;
    private float _maxStepUp = 0.55f;
    private float _maxStepDown = 5f;

    public void Configure(float fallbackY, float entityScale)
    {
        _fallbackY = fallbackY;
        _maxStepUp = 0.55f * Mathf.Max(1f, entityScale);
        _maxStepDown = 3f * Mathf.Max(1f, entityScale);
    }

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
    }

    private void LateUpdate()
    {
        if (_agent == null || !_agent.enabled || !_agent.isOnNavMesh)
            return;

        Vector3 pos = transform.position;
        Vector3 moveDir = _agent.velocity;
        moveDir.y = 0f;

        if (!SpawnHelper.TryQueryFootingY(
                pos, moveDir, pos.y, _fallbackY, out float footingY, _maxStepUp, _maxStepDown))
        {
            return;
        }

        if (Mathf.Abs(footingY - pos.y) < 0.001f)
            return;

        pos.y = footingY;
        transform.position = pos;
        _agent.nextPosition = pos;
    }
}
