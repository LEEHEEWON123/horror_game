using UnityEngine;

[DefaultExecutionOrder(100)]
[RequireComponent(typeof(Rigidbody))]
public class PlayerMapBounds : MonoBehaviour
{
    private Bounds _bounds;
    private Rigidbody _rb;

    public void Configure(Bounds bounds, float inset = 1.25f)
    {
        _bounds = MapBoundaryBuilder.ClampBounds(bounds, inset);
        _rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (_bounds.size.x <= 0f || _bounds.size.z <= 0f)
            return;

        var pos = _rb.position;
        float clampedX = Mathf.Clamp(pos.x, _bounds.min.x, _bounds.max.x);
        float clampedZ = Mathf.Clamp(pos.z, _bounds.min.z, _bounds.max.z);

        if (Mathf.Approximately(clampedX, pos.x) && Mathf.Approximately(clampedZ, pos.z))
            return;

        pos.x = clampedX;
        pos.z = clampedZ;
        _rb.position = pos;

        var velocity = _rb.linearVelocity;
        velocity.x = 0f;
        velocity.z = 0f;
        _rb.linearVelocity = velocity;
    }
}
