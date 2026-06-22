using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance { get; private set; }

    [SerializeField] private GameObject interactionButton;
    [SerializeField] private TMP_Text interactionLabel;

    private void Awake()
    {
        Instance = this;
        ShowInteractionButton(false, "");
    }

    public void Configure(GameObject interactBtn, TMP_Text label)
    {
        interactionButton = interactBtn;
        interactionLabel = label;
        ShowInteractionButton(false, "");
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void ShowInteractionButton(bool show, string label)
    {
        if (interactionButton == null) return;
        interactionButton.SetActive(show);
        if (interactionLabel != null)
            interactionLabel.text = label;
    }
}
