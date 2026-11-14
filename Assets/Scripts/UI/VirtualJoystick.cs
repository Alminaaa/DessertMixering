using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// On-screen joystick for touch input. Also works with the mouse in the editor.
///
/// Hierarchy:
///   JoystickArea      (this script + Image with alpha 0, Raycast Target ON)
///   |- Background     (Image, pivot center, scale 1)  -> outer ring
///      |- Handle      (Image, pivot center, anchors center) -> inner circle
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public enum JoystickMode
    {
        Fixed,    // Stays where you placed it in the editor
        Floating, // Appears where the finger touches and stays there while dragging
        Dynamic   // Appears where the finger touches and follows the finger if it goes too far
    }

    [Header("References")]
    [SerializeField] private RectTransform background;
    [SerializeField] private RectTransform handle;

    [Header("Behaviour")]
    [SerializeField] private JoystickMode mode = JoystickMode.Floating;

    [Tooltip("How far the handle can move from the center, in canvas units.")]
    [SerializeField] private float handleRange = 100f;

    [Tooltip("Input smaller than this (0-1) is ignored, so the player does not drift.")]
    [SerializeField, Range(0f, 0.9f)] private float deadZone = 0.1f;

    [Tooltip("Hide the joystick while it is not touched (Floating and Dynamic only).")]
    [SerializeField] private bool hideWhenIdle = true;

    /// <summary>Current input direction. Length is between 0 and 1.</summary>
    public Vector2 Direction { get; private set; }

    public bool IsPressed => activePointerId != NoPointer;

    private const int NoPointer = int.MinValue;

    private RectTransform area;
    private Camera uiCamera;
    private CanvasGroup backgroundGroup;
    private Vector3 restLocalPosition;
    private int activePointerId = NoPointer;

    private void Awake()
    {
        area = (RectTransform)transform;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvas = canvas.rootCanvas;
            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                uiCamera = canvas.worldCamera;
        }

        // CanvasGroup lets us hide the joystick safely at any time.
        backgroundGroup = background.GetComponent<CanvasGroup>();
        if (backgroundGroup == null)
            backgroundGroup = background.gameObject.AddComponent<CanvasGroup>();

        // The visuals must not steal touches from the touch area.
        backgroundGroup.blocksRaycasts = false;
        backgroundGroup.interactable = false;

        restLocalPosition = background.localPosition;
        ResetJoystick();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Ignore a second finger while the first one is controlling the joystick.
        if (activePointerId != NoPointer)
            return;

        activePointerId = eventData.pointerId;

        if (mode != JoystickMode.Fixed &&
            RectTransformUtility.ScreenPointToLocalPointInRectangle(area, eventData.position, uiCamera, out Vector2 localPoint))
        {
            background.localPosition = localPoint;
        }

        SetVisible(true);
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.pointerId != activePointerId)
            return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(background, eventData.position, uiCamera, out Vector2 offset))
            return;

        Vector2 input = offset / handleRange;
        float magnitude = input.magnitude;

        if (magnitude > 1f)
        {
            if (mode == JoystickMode.Dynamic)
            {
                // Pull the base along so the finger never leaves the joystick.
                Vector2 excess = input / magnitude * (magnitude - 1f) * handleRange;
                background.localPosition += (Vector3)excess;
            }

            input /= magnitude;
            magnitude = 1f;
        }

        handle.anchoredPosition = input * handleRange;

        if (magnitude < deadZone)
        {
            Direction = Vector2.zero;
        }
        else
        {
            // Remap so the output starts at 0 right after the dead zone.
            float remapped = (magnitude - deadZone) / (1f - deadZone);
            Direction = input / magnitude * remapped;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId != activePointerId)
            return;

        ResetJoystick();
    }

    private void OnDisable()
    {
        // Prevents a "stuck" joystick if the UI is turned off while touched.
        ResetJoystick();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        // Pointer Up is not sent when the app loses focus (phone call, notification, etc).
        if (!hasFocus)
            ResetJoystick();
    }

    private void ResetJoystick()
    {
        activePointerId = NoPointer;
        Direction = Vector2.zero;

        if (handle != null)
            handle.anchoredPosition = Vector2.zero;

        if (background != null && mode != JoystickMode.Fixed)
            background.localPosition = restLocalPosition;

        SetVisible(!hideWhenIdle || mode == JoystickMode.Fixed);
    }

    private void SetVisible(bool visible)
    {
        if (backgroundGroup != null)
            backgroundGroup.alpha = visible ? 1f : 0f;
    }
}