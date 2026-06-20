using UnityEngine;

public class DimensionFallTrigger : MonoBehaviour
{
    private bool _triggered;

    private void OnTriggerEnter(Collider other)
    {
        if (_triggered) return;
        if (!IsPlayer(other)) return;

        _triggered = true;

        var controller = other.GetComponentInParent<PlayerController>();
        if (controller != null)
            controller.enabled = false;

        SceneTransitioner.Instance?.PrepareNoclipTransition();
        GameManager.Instance?.EnterRandomDimension();
    }

    private static bool IsPlayer(Collider other)
    {
        if (other.CompareTag("Player"))
            return true;

        return other.GetComponentInParent<PlayerController>() != null;
    }
}
