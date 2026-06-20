using System.Collections;
using UnityEngine;

public class RealityFallCoordinator : MonoBehaviour
{
    private IEnumerator Start()
    {
        for (int i = 0; i < 3; i++)
            yield return null;

        if (!RealityEscapeState.ConsumePending())
        {
            Destroy(gameObject);
            yield break;
        }

        var player = GameObject.FindGameObjectWithTag("Player");
        var endingPanel = GameObject.Find("EndingPanel");

        var sequenceHost = player != null ? player : gameObject;
        var sequence = sequenceHost.GetComponent<RealityFallSequence>();
        if (sequence == null)
            sequence = sequenceHost.AddComponent<RealityFallSequence>();

        yield return sequence.Run(player != null ? player.transform : null, endingPanel);
        Destroy(gameObject);
    }
}
