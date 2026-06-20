using UnityEngine;
using UnityEngine.AI;

public class MonsterLocomotion : MonoBehaviour
{
    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    private Animator _animator;
    private NavMeshAgent _agent;

    public void Bind(NavMeshAgent agent, Animator animator)
    {
        _agent = agent;
        _animator = animator;

        if (_animator != null)
            _animator.applyRootMotion = false;
    }

    private void Update()
    {
        if (_animator == null || _agent == null) return;

        var velocity = _agent.velocity;
        velocity.y = 0f;
        float speed = velocity.magnitude;

        if (speed < 0.05f && _agent.hasPath && !_agent.isStopped)
        {
            var desired = _agent.desiredVelocity;
            desired.y = 0f;
            speed = desired.magnitude;
        }

        if (HasParameter(_animator, SpeedHash))
            _animator.SetFloat(SpeedHash, speed, 0.1f, Time.deltaTime);
    }

    private static bool HasParameter(Animator anim, int hash)
    {
        foreach (var param in anim.parameters)
        {
            if (param.nameHash == hash)
                return true;
        }

        return false;
    }
}
