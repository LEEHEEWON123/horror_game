using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class PriestPlayableLocomotion : MonoBehaviour
{
    private NavMeshAgent _agent;
    private PlayableGraph _graph;
    private AnimationClipPlayable _clipPlayable;

    public void Bind(NavMeshAgent agent, Animator animator, AnimationClip clip)
    {
        _agent = agent;
        if (animator == null || clip == null) return;

        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        animator.enabled = true;
        animator.runtimeAnimatorController = null;
        animator.Rebind();

        _graph = PlayableGraph.Create("PriestLocomotion");
        _graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

        var output = AnimationPlayableOutput.Create(_graph, "PriestOutput", animator);
        _clipPlayable = AnimationClipPlayable.Create(_graph, clip);
        _clipPlayable.SetApplyFootIK(false);
        output.SetSourcePlayable(_clipPlayable);
        _clipPlayable.SetSpeed(0f);
        _clipPlayable.SetTime(0f);
        _graph.Play();
        animator.Update(0f);
    }

    private void Update()
    {
        if (!_clipPlayable.IsValid() || _agent == null) return;

        bool moving = _agent.velocity.sqrMagnitude > 0.04f
                      || (_agent.hasPath && _agent.remainingDistance > _agent.stoppingDistance + 0.05f);

        if (moving)
        {
            float speed = Mathf.Max(_agent.velocity.magnitude, 0.5f);
            _clipPlayable.SetSpeed(Mathf.Clamp(speed / 3f, 0.75f, 1.35f));
        }
        else
            _clipPlayable.SetSpeed(0f);
    }

    private void OnDestroy()
    {
        if (_graph.IsValid())
            _graph.Destroy();
    }
}
