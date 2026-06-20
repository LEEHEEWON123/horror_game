using UnityEngine;

public class MinimapController : MonoBehaviour
{
    public static MinimapController Instance { get; private set; }

    [SerializeField] private RectTransform playerIcon;
    [SerializeField] private RectTransform keyIcon;
    [SerializeField] private RectTransform lockIcon;
    [SerializeField] private RectTransform exitIcon;

    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform keyTransform;
    [SerializeField] private Transform lockTransform;
    [SerializeField] private Transform exitTransform;

    [SerializeField] private float mapWorldWidth = 50f;
    [SerializeField] private float mapWorldHeight = 10f;
    [SerializeField] private Vector2 mapWorldCenter = Vector2.zero;
    [SerializeField] private float minimapSize = 80f;

    private void Awake() => Instance = this;

    public void BindWorldTargets(Transform playerT, Transform keyT, Transform lockT, Transform exitT)
    {
        playerTransform = playerT;
        keyTransform = keyT;
        lockTransform = lockT;
        exitTransform = exitT;
    }

    public void SetMapBounds(float width, float height, Vector2 center)
    {
        mapWorldWidth = width;
        mapWorldHeight = height;
        mapWorldCenter = center;
    }

    public void BindIcons(RectTransform player, RectTransform key, RectTransform lockIconRt, RectTransform exit)
    {
        playerIcon = player;
        keyIcon = key;
        lockIcon = lockIconRt;
        exitIcon = exit;
    }

    private void Update()
    {
        SetIconPos(playerIcon, playerTransform);
        if (keyIcon != null && keyIcon.gameObject.activeSelf) SetIconPos(keyIcon, keyTransform);
        if (lockIcon != null && lockIcon.gameObject.activeSelf) SetIconPos(lockIcon, lockTransform);
        SetIconPos(exitIcon, exitTransform);
    }

    private void SetIconPos(RectTransform icon, Transform worldTarget)
    {
        if (icon == null || worldTarget == null) return;

        float nx = (worldTarget.position.x - mapWorldCenter.x) / mapWorldWidth;
        float ny = (worldTarget.position.z - mapWorldCenter.y) / mapWorldHeight;
        icon.anchoredPosition = new Vector2(nx * minimapSize, ny * minimapSize);
    }

    public void HideKeyIcon()
    {
        if (keyIcon != null) keyIcon.gameObject.SetActive(false);
    }

    public void HideLockIcon()
    {
        if (lockIcon != null) lockIcon.gameObject.SetActive(false);
    }
}
