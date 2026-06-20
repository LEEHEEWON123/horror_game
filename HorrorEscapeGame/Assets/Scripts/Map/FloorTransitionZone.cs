using UnityEngine;

public class FloorTransitionZone : MonoBehaviour
{
    [SerializeField] private float targetWalkY;
    [SerializeField] private Vector3 destination;

    public void Configure(float walkY, Vector3 dest)
    {
        targetWalkY = walkY;
        destination = dest;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        MapFloor.SetWalkY(targetWalkY);

        var rb = other.attachedRigidbody ?? other.GetComponentInParent<Rigidbody>();
        var t = rb != null ? rb.transform : other.transform;
        var pos = new Vector3(destination.x, targetWalkY, destination.z);
        t.position = pos;
        if (rb != null)
        {
            rb.position = pos;
            rb.linearVelocity = Vector3.zero;
        }
    }
}
