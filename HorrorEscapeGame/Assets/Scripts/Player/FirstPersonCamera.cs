using UnityEngine;
using UnityEngine.InputSystem;

public class FirstPersonCamera : MonoBehaviour
{
    public const float DefaultMouseSensitivity = 3f;

    [SerializeField] private Transform playerBody;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float mouseSensitivity = DefaultMouseSensitivity;
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;
    [SerializeField] private float bobAmount = 0.035f;
    [SerializeField] private float bobSpeed = 11f;

    private float _yaw;
    private float _pitch;
    private float _bobPhase;
    private Vector3 _cameraRestLocalPos;
    private Rigidbody _rb;
    private float _wakeSavedMinPitch;
    private float _wakeSavedMaxPitch;
    private bool _wakeMode;

    public void SetSensitivity(float s) => mouseSensitivity = s;

    public void Configure(Transform body, Transform cam, float sensitivity = DefaultMouseSensitivity)
    {
        playerBody = body;
        cameraTransform = cam;
        mouseSensitivity = sensitivity;
        _rb = body != null ? body.GetComponent<Rigidbody>() : null;
        _yaw = body != null ? body.eulerAngles.y : 0f;
        _pitch = 0f;
        _cameraRestLocalPos = cam.localPosition;
        cam.localRotation = Quaternion.identity;
    }

    public void SyncBodyRotation()
    {
        if (playerBody == null) return;

        _yaw = playerBody.eulerAngles.y;
    }

    public void SetPitchLimits(float min, float max)
    {
        minPitch = min;
        maxPitch = max;
        _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);
    }

    public float EyeHeight => transform.localPosition.y;

    public void SetView(float pitch, float eyeHeight)
    {
        _pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        transform.localPosition = new Vector3(transform.localPosition.x, eyeHeight, transform.localPosition.z);
        _cameraRestLocalPos = new Vector3(_cameraRestLocalPos.x, 0f, _cameraRestLocalPos.z);
        if (cameraTransform != null)
        {
            cameraTransform.localPosition = _cameraRestLocalPos;
            cameraTransform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }
    }

    public void EnterWakeMode()
    {
        if (_wakeMode) return;

        _wakeSavedMinPitch = minPitch;
        _wakeSavedMaxPitch = maxPitch;
        _wakeMode = true;
        SetPitchLimits(-85f, 85f);
    }

    public void ExitWakeMode()
    {
        if (!_wakeMode) return;

        _wakeMode = false;
        SetPitchLimits(_wakeSavedMinPitch, _wakeSavedMaxPitch);
    }

    private void OnEnable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        mouseSensitivity = MouseSensitivityPrefs.Load();
        MouseSensitivityPrefs.SensitivityChanged += OnSensitivityChanged;
    }

    private void OnDisable()
    {
        MouseSensitivityPrefs.SensitivityChanged -= OnSensitivityChanged;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnSensitivityChanged(float value) => mouseSensitivity = value;

    private void Update()
    {
        if (playerBody == null || cameraTransform == null) return;
        if (SensitivitySettingsUI.IsOpen) return;

        var mouse = Mouse.current;
        if (mouse == null) return;

        Vector2 delta = mouse.delta.ReadValue() * mouseSensitivity;
        if (delta.sqrMagnitude < 0.0001f) return;

        _yaw += delta.x;
        _pitch = Mathf.Clamp(_pitch - delta.y, minPitch, maxPitch);
        ApplyBodyYaw();
    }

    private void ApplyBodyYaw()
    {
        var rotation = Quaternion.Euler(0f, _yaw, 0f);
        playerBody.rotation = rotation;
        if (_rb != null)
            _rb.rotation = rotation;
    }

    private void LateUpdate()
    {
        if (playerBody == null || cameraTransform == null) return;

        cameraTransform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        ApplyHeadBob();
    }

    private void ApplyHeadBob()
    {
        if (_rb == null)
        {
            cameraTransform.localPosition = _cameraRestLocalPos;
            return;
        }

        Vector3 velocity = _rb.linearVelocity;
        velocity.y = 0f;
        float speed = velocity.magnitude;

        if (speed < 0.15f)
        {
            _bobPhase = 0f;
            cameraTransform.localPosition = _cameraRestLocalPos;
            return;
        }

        _bobPhase += Time.deltaTime * bobSpeed * Mathf.Clamp01(speed / 5f);
        float bobY = Mathf.Sin(_bobPhase) * bobAmount;
        float bobX = Mathf.Cos(_bobPhase * 0.5f) * bobAmount * 0.5f;
        cameraTransform.localPosition = _cameraRestLocalPos + new Vector3(bobX, bobY, 0f);
    }
}
