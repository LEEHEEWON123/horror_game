using System.Collections;
using UnityEngine;

public class DimensionWakeCoordinator : MonoBehaviour
{
    private IEnumerator Start()
    {
        if (!NoclipEntryState.ConsumePending())
        {
            Destroy(gameObject);
            yield break;
        }

        yield return WaitForMapReady();

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            if (SceneTransitioner.Instance != null)
                yield return SceneTransitioner.Instance.FadeInRoutine();
            NoclipEntryState.ConsumeAutoStand();
            Destroy(gameObject);
            yield break;
        }

        var sequence = player.AddComponent<DimensionWakeSequence>();
        yield return sequence.Run(autoStand: true);
        NoclipEntryState.ConsumeAutoStand();
        Destroy(gameObject);
    }

    private static IEnumerator WaitForMapReady()
    {
        const float timeout = 6f;
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            var cam = Camera.main;
            var fpCamera = player != null ? player.GetComponentInChildren<FirstPersonCamera>() : null;
            int meshCount = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Length;

            if (player != null && cam != null && fpCamera != null && meshCount >= 8)
            {
                Physics.SyncTransforms();
                yield return null;
                yield return new WaitForEndOfFrame();
                yield break;
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        Debug.LogWarning("[DimensionWakeCoordinator] Map was not ready before wake sequence; continuing anyway.");
    }
}
