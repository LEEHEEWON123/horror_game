using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] private RectTransform background;
    [SerializeField] private RectTransform handle;
    [SerializeField] private float maxHandleDistance = 60f;

    private Vector2 _input = Vector2.zero;
    private Canvas _canvas;

    public Vector2 Input => _input;

    public void Configure(RectTransform bg, RectTransform knob)
    {
        background = bg;
        handle = knob;
    }

    private void Awake()
    {
        _canvas = GetComponentInParent<Canvas>();
        if (background != null)
            background.gameObject.SetActive(false);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (background == null || handle == null) return;

        background.gameObject.SetActive(true);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            transform.parent as RectTransform,
            eventData.position,
            _canvas != null ? _canvas.worldCamera : null,
            out Vector2 localPoint);
        background.anchoredPosition = localPoint;
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (background == null || handle == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            background,
            eventData.position,
            _canvas != null ? _canvas.worldCamera : null,
            out Vector2 localPoint);

        _input = localPoint.magnitude > maxHandleDistance
            ? localPoint.normalized
            : localPoint / maxHandleDistance;

        handle.anchoredPosition = _input * maxHandleDistance;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _input = Vector2.zero;
        if (handle != null) handle.anchoredPosition = Vector2.zero;
        if (background != null) background.gameObject.SetActive(false);
    }
}
