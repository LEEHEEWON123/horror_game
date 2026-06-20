using UnityEngine;
using UnityEngine.InputSystem;

public class InteractionPrompt : MonoBehaviour
{
    private PlayerInteraction _interaction;

    private void Awake()
    {
        _interaction = GetComponent<PlayerInteraction>();
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.eKey.wasPressedThisFrame)
            _interaction?.TryInteract();
    }
}
