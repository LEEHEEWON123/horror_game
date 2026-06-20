using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance { get; private set; }

    [SerializeField] private Image[] heartImages;
    [SerializeField] private GameObject interactionButton;
    [SerializeField] private TMP_Text interactionLabel;

    private void Awake()
    {
        Instance = this;
        int lives = GameManager.Instance != null ? GameManager.Instance.Lives : GameState.MaxLives;
        UpdateHearts(lives);
        ShowInteractionButton(false, "");
    }

    public void Configure(Image[] hearts, GameObject interactBtn, TMP_Text label)
    {
        heartImages = hearts;
        interactionButton = interactBtn;
        interactionLabel = label;

        int lives = GameManager.Instance != null ? GameManager.Instance.Lives : GameState.MaxLives;
        UpdateHearts(lives);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void UpdateHearts(int lives)
    {
        if (heartImages == null) return;
        for (int i = 0; i < heartImages.Length; i++)
        {
            if (heartImages[i] == null) continue;
            heartImages[i].color = i < lives ? Color.red : new Color(0.3f, 0.3f, 0.3f, 0.5f);
        }
    }

    public void ShowInteractionButton(bool show, string label)
    {
        if (interactionButton == null) return;
        interactionButton.SetActive(show);
        if (interactionLabel != null)
            interactionLabel.text = label;
    }
}
