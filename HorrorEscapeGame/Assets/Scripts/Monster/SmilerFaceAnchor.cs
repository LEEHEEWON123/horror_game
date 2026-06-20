using UnityEngine;

public class SmilerFaceAnchor : MonoBehaviour
{
    private Transform _visual;

    public void Configure(Transform visual) => _visual = visual;

    public Vector3 FacePoint
    {
        get
        {
            if (_visual != null)
                return _visual.position + _visual.up * (SmilerVisualSetup.VisualHeight * 0.08f);

            return transform.position + Vector3.up * 1.2f;
        }
    }
}
