using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private VirtualJoystick joystick;
    [SerializeField] private float moveSpeed = 4.5f;

    private Rigidbody _rb;
    private bool _followGround;
    private float _groundFallbackY;
    private float _maxStepUp = 0.55f;
    private float _maxStepDown = 3f;
    private Collider _roadCollider;
    private float _streetBaselineY;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.useGravity = false;
    }

    public void SetJoystick(VirtualJoystick joy) => joystick = joy;

    public void EnableGroundFollow(float fallbackY, float maxStepUp = 0.55f, float maxStepDown = 3f)
    {
        _followGround = true;
        _groundFallbackY = fallbackY;
        _maxStepUp = maxStepUp;
        _maxStepDown = maxStepDown;
        _roadCollider = null;
        _rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    public void EnableRoadFollow(Collider road, float streetBaselineY)
    {
        _roadCollider = road;
        _streetBaselineY = streetBaselineY;
        EnableGroundFollow(streetBaselineY);
    }

    private void FixedUpdate()
    {
        Vector2 input = ReadInput();
        if (input.sqrMagnitude > 1f) input.Normalize();

        Vector3 moveDir = transform.forward * input.y + transform.right * input.x;
        moveDir.y = 0f; // Y성분 제거 → FreezePositionY와 충돌 방지
        if (moveDir.sqrMagnitude > 0.01f)
            moveDir.Normalize();

        Vector3 delta = moveDir * (moveSpeed * Time.fixedDeltaTime);
        Vector3 nextPos = _rb.position + delta;

        if (_followGround)
        {
            float footingY;
            if (_roadCollider != null)
            {
                footingY = DemoCityRoadFloor.QueryFootingY(
                    nextPos, moveDir, _rb.position.y, _roadCollider, _streetBaselineY);
            }
            else if (!SpawnHelper.TryQueryFootingY(
                         nextPos, moveDir, _rb.position.y, _groundFallbackY, out footingY,
                         _maxStepUp, _maxStepDown))
            {
                footingY = _rb.position.y;
            }

            nextPos.y = footingY;
        }
        else
        {
            nextPos.y = MapFloor.WalkY;
        }

        _rb.MovePosition(nextPos);
        _rb.linearVelocity = moveDir * moveSpeed;
    }

    private Vector2 ReadInput()
    {
        Vector2 input = joystick != null ? joystick.Input : Vector2.zero;
        if (input.sqrMagnitude > 0.01f) return input;

        var keyboard = Keyboard.current;
        if (keyboard == null) return input;

        float x = (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? -1f : 0f)
                + (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f);
        float y = (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? -1f : 0f)
                + (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1f : 0f);
        return new Vector2(x, y);
    }
}
