using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    private IInteractable _currentInteractable;

    private static IInteractable FindInteractable(Collider other)
    {
        if (other.TryGetComponent(out IInteractable direct))
            return direct;
        return other.GetComponentInParent<IInteractable>();
    }

    private void OnTriggerEnter(Collider other)
    {
        var interactable = FindInteractable(other);
        if (interactable == null) return;

        _currentInteractable = interactable;
        HUDManager.Instance?.ShowInteractionButton(
            interactable.CanInteract(),
            interactable.GetInteractionLabel());
    }

    private void OnTriggerStay(Collider other)
    {
        if (_currentInteractable == null) return;
        var interactable = FindInteractable(other);
        if (interactable == null) return;

        HUDManager.Instance?.ShowInteractionButton(
            interactable.CanInteract(),
            interactable.GetInteractionLabel());
    }

    private void OnTriggerExit(Collider other)
    {
        if (FindInteractable(other) == null) return;

        _currentInteractable = null;
        HUDManager.Instance?.ShowInteractionButton(false, "");
    }

    public void TryInteract()
    {
        if (_currentInteractable == null) return;
        if (_currentInteractable.CanInteract())
            _currentInteractable.Interact();
    }
}
