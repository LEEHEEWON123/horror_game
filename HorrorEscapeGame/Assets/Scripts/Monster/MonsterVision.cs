using UnityEngine;

public class MonsterVision : MonoBehaviour
{
    [SerializeField] private float viewRadius = 14f;
    [SerializeField] private float viewAngle = 110f;
    [SerializeField] private float senseRadius = 8f;
    [SerializeField] private LayerMask targetMask;
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private float eyeHeight = 1.6f;

    private Transform _player;

    public float SenseRadius => senseRadius;

    public void Configure(
        LayerMask targets,
        LayerMask obstacles,
        float visionRadius = 14f,
        float fov = 110f,
        float proximity = 8f,
        float visionEyeHeight = 1.6f)
    {
        targetMask = targets;
        obstacleMask = obstacles;
        viewRadius = visionRadius;
        viewAngle = fov;
        senseRadius = proximity;
        eyeHeight = visionEyeHeight;
    }

    public bool CanSeePlayer(out Transform target)
    {
        target = null;
        if (!TryGetPlayerInternal(out Transform player)) return false;

        Vector3 origin = EyePosition();
        Vector3 dirToTarget = player.position - origin;
        float distance = dirToTarget.magnitude;
        if (distance > viewRadius) return false;

        dirToTarget /= distance;
        if (!IsInFieldOfView(transform.forward, dirToTarget, viewAngle))
            return false;

        int sightMask = targetMask.value | obstacleMask.value;
        if (Physics.Raycast(origin, dirToTarget, out RaycastHit hit, distance, sightMask, QueryTriggerInteraction.Ignore))
        {
            if (IsPlayerCollider(hit.collider))
            {
                target = player;
                return true;
            }

            return false;
        }

        target = player;
        return true;
    }

    public bool SensePlayerNearby(out Transform target)
    {
        target = null;
        if (!TryGetPlayerInternal(out Transform player)) return false;

        Vector3 flat = player.position - transform.position;
        flat.y = 0f;
        if (flat.sqrMagnitude > senseRadius * senseRadius) return false;

        target = player;
        return true;
    }

    public bool IsPlayerWithin(float radius, out Transform target)
    {
        target = null;
        if (!TryGetPlayerInternal(out Transform player)) return false;

        Vector3 flat = player.position - transform.position;
        flat.y = 0f;
        if (flat.sqrMagnitude > radius * radius) return false;

        target = player;
        return true;
    }

    public bool TryGetPlayer(out Transform player) => TryGetPlayerInternal(out player);

    private bool TryGetPlayerInternal(out Transform player)
    {
        if (_player == null)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            _player = go != null ? go.transform : null;
        }

        player = _player;
        return player != null;
    }

    private Vector3 EyePosition() => transform.position + Vector3.up * eyeHeight;

    private static bool IsPlayerCollider(Collider col)
    {
        if (col == null) return false;
        return col.CompareTag("Player") || col.GetComponentInParent<PlayerHealth>() != null;
    }

    public static bool IsInFieldOfView(Vector3 monsterForward, Vector3 dirToTarget, float fovAngle)
    {
        float angle = Vector3.Angle(monsterForward, dirToTarget.normalized);
        return angle < fovAngle / 2f;
    }
}
