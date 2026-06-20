using System.Collections;
using UnityEngine;

public class PlayerFootAlignRunner : MonoBehaviour
{
    private void Start()
    {
        var visual = transform.Find("ProtectiveSuitVisual");
        if (visual != null)
            StartCoroutine(AlignAfterAnimatorUpdate(visual.gameObject));
    }

    private static IEnumerator AlignAfterAnimatorUpdate(GameObject visualRoot)
    {
        yield return null;
        PlayerFootAlign.AlignVisualToFloor(visualRoot);
    }
}
