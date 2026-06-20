using UnityEngine;

public static class PortalEntrySpawn
{
    private const float StepIntoMap = 2f;

    public static void ApplyIfPending(GameObject player)
    {
        if (player == null || !PortalTransitionState.PendingEntry)
            return;

        Vector3 forward = PortalTransitionState.EntryForward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.01f)
            forward = Vector3.forward;
        forward.Normalize();

        Vector3 entryPos = player.transform.position + forward * StepIntoMap;
        entryPos = MapFloor.PlaceOnFloor(entryPos);
        player.transform.position = entryPos;
        player.transform.rotation = Quaternion.LookRotation(forward);

        var rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.position = entryPos;
            rb.rotation = player.transform.rotation;
        }

        var fpCamera = player.GetComponentInChildren<FirstPersonCamera>();
        fpCamera?.SyncBodyRotation();

        PortalTransitionState.ClearAfterEntry();
    }
}
