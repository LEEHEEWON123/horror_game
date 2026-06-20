using UnityEngine;

public class MonsterAttackZone : MonoBehaviour
{
    private MonsterAI _owner;

    private void Awake()
    {
        _owner = GetComponentInParent<MonsterAI>();
    }

    private void OnTriggerEnter(Collider other) => TryAttack(other);

    private void OnTriggerStay(Collider other) => TryAttack(other);

    private void TryAttack(Collider other)
    {
        if (DimensionWakeState.IsActive) return;
        if (!other.CompareTag("Player")) return;
        if (_owner != null && !_owner.IsLockedOn) return;

        other.GetComponentInParent<PlayerHealth>()?.BeginCatch(_owner);
    }
}
