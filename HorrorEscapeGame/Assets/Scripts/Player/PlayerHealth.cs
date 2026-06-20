using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    private bool _isDead;
    private bool _catchStarted;

    public bool IsCaught => _catchStarted || _isDead;

    public void BeginCatch(MonsterAI attacker)
    {
        if (IsCaught || DimensionWakeState.IsActive) return;
        _catchStarted = true;

        var catchSequence = GetComponent<PlayerCatchSequence>();
        if (catchSequence == null)
            catchSequence = gameObject.AddComponent<PlayerCatchSequence>();

        catchSequence.Play(attacker, this);
    }

    public void CompleteCatchDeath()
    {
        if (_isDead) return;
        _isDead = true;
        GameManager.Instance.PlayerDied();
    }

    public void TakeDamage()
    {
        if (IsCaught) return;
        _isDead = true;

        var controller = GetComponent<PlayerController>();
        if (controller != null) controller.enabled = false;

        var interaction = GetComponent<PlayerInteraction>();
        if (interaction != null) interaction.enabled = false;

        GameManager.Instance.PlayerDied();
    }
}
