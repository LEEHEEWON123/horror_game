using UnityEngine;

public class PlayerLocomotionAnimator : MonoBehaviour
{
    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    private Animator _animator;
    private Rigidbody _rb;

    public void Bind(Animator animator)
    {
        _animator = animator;
        _rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (_animator == null || _rb == null) return;

        var velocity = _rb.linearVelocity;
        velocity.y = 0f;
        _animator.SetFloat(SpeedHash, velocity.magnitude);
    }
}
