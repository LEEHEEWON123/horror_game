using UnityEngine;

public class SmilerBillboard : MonoBehaviour
{
    private Transform _cam;

    private void LateUpdate()
    {
        if (_cam == null)
        {
            var cam = Camera.main;
            if (cam == null) return;
            _cam = cam.transform;
        }

        Vector3 forward = _cam.position - transform.position;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f) return;

        transform.rotation = Quaternion.LookRotation(-forward.normalized, Vector3.up);
    }
}
