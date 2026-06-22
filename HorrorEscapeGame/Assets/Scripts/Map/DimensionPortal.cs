using System.Collections;
using UnityEngine;

public class DimensionPortal : MonoBehaviour
{
    private const float PullDuration = 1.15f;
    private const float HoldDuration = 0.12f;

    [SerializeField] private PortalTransitionKind _kind = PortalTransitionKind.Dimension;

    private Transform _portalRoot;
    private bool _triggered;

    public void Configure(PortalTransitionKind kind, Transform portalRoot = null)
    {
        _kind = kind;
        _portalRoot = portalRoot != null ? portalRoot : transform.parent != null ? transform.parent : transform;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_triggered || !IsPlayer(other)) return;

        _triggered = true;
        StartCoroutine(RunEntrySequence(other.transform));
    }

    private IEnumerator RunEntrySequence(Transform player)
    {
        if (player == null)
        {
            if (_kind == PortalTransitionKind.Dimension)
                GameManager.Instance?.CompleteMapThroughPortal(_portalRoot);
            else
                GameManager.Instance?.CompleteMap();
            yield break;
        }

        var controller = player.GetComponent<PlayerController>();
        var interaction = player.GetComponent<PlayerInteraction>();
        var fpCamera = player.GetComponentInChildren<FirstPersonCamera>();
        var cam = Camera.main;
        var rb = player.GetComponent<Rigidbody>();

        SetGameplayEnabled(controller, interaction, fpCamera, false);
        SetJoystickVisible(false);
        MonsterAI.PauseAllForWake();
        PlayEnterClip(_portalRoot != null ? _portalRoot.position : transform.position);

        float startFov = cam != null ? cam.fieldOfView : 76f;
        const float targetFov = 84f;

        Transform portal = _portalRoot != null ? _portalRoot : transform;
        Vector3 pullTarget = portal.position + portal.forward * 0.35f + Vector3.up * 1.45f;
        Transform camTransform = cam != null ? cam.transform : null;
        Vector3 bodyStart = player.position;
        Vector3 bodyEnd = MapFloor.PlaceOnFloor(portal.position - portal.forward * 0.25f);

        float elapsed = 0f;
        while (elapsed < PullDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / PullDuration);
            float moveT = t * 0.92f;

            Vector3 bodyPos = Vector3.Lerp(bodyStart, bodyEnd, moveT);
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.MovePosition(bodyPos);
            }
            else
            {
                player.position = bodyPos;
            }

            if (camTransform != null)
            {
                Vector3 lookDir = pullTarget - camTransform.position;
                if (lookDir.sqrMagnitude > 0.01f)
                {
                    var lookRot = Quaternion.LookRotation(lookDir.normalized);
                    camTransform.rotation = Quaternion.Slerp(camTransform.rotation, lookRot, t * 0.35f);
                }
            }

            if (cam != null)
                cam.fieldOfView = Mathf.Lerp(startFov, targetFov, t);

            yield return null;
        }

        if (HoldDuration > 0f)
            yield return new WaitForSecondsRealtime(HoldDuration);

        if (_kind == PortalTransitionKind.Dimension)
            GameManager.Instance?.CompleteMapThroughPortal(_portalRoot);
        else
            GameManager.Instance?.CompleteMap();
    }

    private static void PlayEnterClip(Vector3 position)
    {
        var clip = PortalAudioSetup.LoadEnterClip();
        if (clip == null) return;

        AudioSource.PlayClipAtPoint(clip, position, 0.75f);
    }

    private static void SetGameplayEnabled(
        PlayerController controller,
        PlayerInteraction interaction,
        FirstPersonCamera fpCamera,
        bool enabled)
    {
        if (controller != null) controller.enabled = enabled;
        if (interaction != null) interaction.enabled = enabled;
        if (fpCamera != null) fpCamera.enabled = enabled;
    }

    private static void SetJoystickVisible(bool visible)
    {
        var joystick = FindAnyObjectByType<VirtualJoystick>();
        if (joystick == null) return;

        var area = joystick.transform.parent;
        if (area != null)
            area.gameObject.SetActive(visible);
    }

    private static bool IsPlayer(Collider other)
    {
        if (other.CompareTag("Player"))
            return true;

        return other.GetComponentInParent<PlayerController>() != null;
    }
}
